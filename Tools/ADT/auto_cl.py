import asyncio
import logging
import sys
from pathlib import Path
from datetime import datetime, timezone
import aiohttp
import yaml

FILE_PATH = Path(__file__).resolve()
CHANGELOG_PATH = FILE_PATH.parents[2] / "Resources" / "Changelog" / "1ChangelogADT.yml"
OLD_CHANGELOG_PATH = FILE_PATH.parent / "cl_old.yml"

rate_limit_info = {
    "limit": 5000,
    "remaining": 5000,
    "reset_time": datetime.now(timezone.utc),
}

class NoDatesSafeLoader(yaml.SafeLoader):
    @classmethod
    def remove_implicit_resolver(cls, tag_to_remove):
        if "yaml_implicit_resolvers" not in cls.__dict__:
            cls.yaml_implicit_resolvers = cls.yaml_implicit_resolvers.copy()

        for first_letter, mappings in cls.yaml_implicit_resolvers.items():
            cls.yaml_implicit_resolvers[first_letter] = [
                (tag, regexp) for tag, regexp in mappings if tag != tag_to_remove
            ]

NoDatesSafeLoader.remove_implicit_resolver("tag:yaml.org,2002:timestamp")

class MyDumper(yaml.SafeDumper):
    def increase_indent(self, flow=False, indentless=False):
        return super(MyDumper, self).increase_indent(flow, False)

def represent_dict_compact(dumper, data):
    if len(data) == 2 and "message" in data and "type" in data:
        return dumper.represent_mapping("tag:yaml.org,2002:map", data.items(), flow_style=True)
    
    return dumper.represent_mapping("tag:yaml.org,2002:map", data.items())

def represent_multiline_string(dumper, data):
    return dumper.represent_scalar("tag:yaml.org,2002:str", data, style="|" if "\n" in data else None)

MyDumper.add_representer(dict, represent_dict_compact)
MyDumper.add_representer(str, represent_multiline_string)

async def handle_rate_limit(response):
    global rate_limit_info

    rate_limit_info["limit"] = int(response.headers.get("x-ratelimit-limit", rate_limit_info["limit"]))
    rate_limit_info["remaining"] = int(response.headers.get("x-ratelimit-remaining", rate_limit_info["remaining"]))
    reset_timestamp = int(response.headers.get("x-ratelimit-reset", rate_limit_info["reset_time"].timestamp()))
    rate_limit_info["reset_time"] = datetime.fromtimestamp(reset_timestamp, tz=timezone.utc)

    logging.info(f"Rate limit updated: {rate_limit_info['remaining']} remaining out of {rate_limit_info['limit']}.")

async def check_rate_limit():
    global rate_limit_info
    if rate_limit_info["remaining"] == 0:
        reset_in = (rate_limit_info["reset_time"] - datetime.now(timezone.utc)).total_seconds()
        logging.warning(f"Rate limit exceeded. Waiting {reset_in:.2f} seconds.")
        await asyncio.sleep(max(reset_in, 1))

async def fetch_with_retry(session, url, retries=3):
    for attempt in range(retries):
        await check_rate_limit() 

        async with session.get(url) as response:
            await handle_rate_limit(response)

            if response.status == 404:
                logging.info(f"{url} not found (404). Skipping.")
                return None
            
            elif response.status == 200:
                logging.info(f"Successfully fetched data from {url}.")
                return await response.json()
            
            logging.error(f"Error fetching {url}: {response.status}")
            response.raise_for_status()

async def get_latest_pr_number(session: aiohttp.ClientSession, repo):
    url = f"https://api.github.com/repos/{repo}/pulls?state=all&sort=created&direction=desc&per_page=1"
    pr_list = await fetch_with_retry(session, url)
    return pr_list[0]["number"] if pr_list else None

async def get_pr_info(session, repo, pr_number):
    url = f"https://api.github.com/repos/{repo}/pulls/{pr_number}"
    return await fetch_with_retry(session, url)

def load_yaml(file_path):
    if file_path.exists():
        logging.info(f"Loading YAML data from {file_path}.")
        with file_path.open("r", encoding="utf-8") as file:
            return yaml.load(file, Loader=NoDatesSafeLoader)
    
    logging.info(f"{file_path} does not exist. Returning empty entries.")
    return {"Entries": []}

def save_yaml(data, file_path):
    file_path.parent.mkdir(parents=True, exist_ok=True)
    with file_path.open("w", encoding="utf-8") as file:
        yaml.dump(data, file, default_flow_style=False, allow_unicode=True, Dumper=MyDumper)
    logging.info(f"YAML data saved to {file_path}.")

def strip_newlines(data):
    if isinstance(data, dict):
        return {k: strip_newlines(v) for k, v in data.items()}
    
    elif isinstance(data, list):
        return [strip_newlines(v) for v in data]
    
    elif isinstance(data, str):
        return data.replace("\n", " ").replace("\r", " ")
    
    return data

async def fetch_pr_data(token, repo, pr_number):
    pr_data = []

    async def process_pr_data(session, number):
        pr_info = await get_pr_info(session, repo, number)
        if not pr_info or not pr_info.get("merged_at"):
            logging.warning(f"PR #{number} is not merged or does not exist.")
            return None

        body, author, changes = pr_info.get("body", ""), pr_info["user"]["login"], []
        lines = [line.strip() for line in body.splitlines()]
        
        for line in lines:
            if line.lower().startswith("no cl"):
                logging.info(f"PR #{number} has 'no cl' instruction. Skipping.")
                return None
            
            if line.startswith(":cl:"):
                potential_author = line[4:].strip()
                if potential_author:
                    author = potential_author
                
                continue

            change_types = {"- add:": "Add", "- remove:": "Remove", "- tweak:": "Tweak", "- fix:": "Fix"}
            for prefix, change_type in change_types.items():
                if line.startswith(prefix):
                    changes.append({"message": line[len(prefix):].strip(), "type": change_type})
                    break

        return {"author": author, "changes": changes, "time": pr_info["merged_at"]} if changes else None

    async with aiohttp.ClientSession(headers={"Authorization": f"token {token}", "Accept": "application/vnd.github.v3+json"}) as session:
        tasks = [process_pr_data(session, number) for number in range(1, pr_number + 1)]
        for result in await asyncio.gather(*tasks):
            if result:
                pr_data.append(result)

    pr_data.sort(key=lambda x: x["time"])
    return pr_data

def update_changelog(file_path, new_entries):
    if file_path.exists():
        logging.info(f"Removing old changelog file {file_path}.")
        file_path.unlink()

    old_entries = load_yaml(OLD_CHANGELOG_PATH).get("Entries", [])
    next_id = max((entry.get("id", 0) for entry in old_entries), default=0) + 1

    for entry in new_entries:
        entry["id"] = next_id
        next_id += 1

    save_yaml({"Entries": strip_newlines(old_entries + new_entries)}, file_path)
    logging.info("Updated PR data saved to 1ChangelogADT.yml")

async def main():
    if len(sys.argv) < 3:
        logging.error("Usage: auto_cl.py <GITHUB_TOKEN> <REPO>")
        sys.exit(1)

    github_token, repo = sys.argv[1], sys.argv[2]
    
    async with aiohttp.ClientSession(headers={"Authorization": f"token {github_token}", "Accept": "application/vnd.github.v3+json"}) as session:
        logging.info(f"Fetching the latest PR number for repo: {repo}.")
        pr_number = await get_latest_pr_number(session, repo)
        if pr_number is None:
            logging.error("Failed to get the latest PR number")
            sys.exit(1)

        logging.info(f"Latest PR number is: {pr_number}. Fetching PR data.")
        pr_data = await fetch_pr_data(github_token, repo, pr_number)
        update_changelog(CHANGELOG_PATH, pr_data)

if __name__ == "__main__":
    logging.basicConfig(level=logging.INFO)
    asyncio.run(main())
