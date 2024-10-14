import asyncio
import logging
import sys
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path

import aiohttp
import yaml

FILE_PATH = Path(__file__).resolve()
CHANGELOG_PATH = FILE_PATH.parents[2] / "Resources" / "Changelog" / "1ChangelogADT.yml"
OLD_CHANGELOG_PATH = FILE_PATH.parent / "cl_old.yml"


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


def represent_dict_compact(dumper, data):
    if len(data) == 2 and "message" in data and "type" in data:
        return dumper.represent_mapping(
            "tag:yaml.org,2002:map", data.items(), flow_style=True
        )

    return dumper.represent_mapping("tag:yaml.org,2002:map", data.items())


def represent_multiline_string(dumper, data):
    if "\n" in data:
        return dumper.represent_scalar("tag:yaml.org,2002:str", data, style="|")

    return dumper.represent_scalar("tag:yaml.org,2002:str", data)


class MyDumper(yaml.SafeDumper):
    def increase_indent(self, flow=False, indentless=False):
        return super(MyDumper, self).increase_indent(flow, False)


MyDumper.add_representer(dict, represent_dict_compact)
MyDumper.add_representer(str, represent_multiline_string)


async def get_pr_info(session, repo, pr_number):
    url = f"https://api.github.com/repos/{repo}/pulls/{pr_number}"
    async with session.get(url) as response:
        if response.status == 404:
            logging.warning(f"PR #{pr_number} not found (404). Skipping.")
            return None

        elif response.status == 403:
            logging.error("Access forbidden (403). Exiting.")
            # При выбросе 403 будет краш от asyncio ибо криво закрывается поток.
            sys.exit(1)

        if response.status != 200 or response.status != 404:
            response.raise_for_status()

        return await response.json()


async def get_latest_pr_number(session, repo):
    url = f"https://api.github.com/repos/{repo}/pulls?state=all&sort=created&direction=desc&per_page=1"
    async with session.get(url) as response:
        if response.status == 403:
            logging.error("Access forbidden (403). Exiting.")
            sys.exit(1)
        response.raise_for_status()
        pr_list = await response.json()
        return pr_list[0]["number"] if pr_list else None


def load_yaml(file_path):
    if file_path.exists():
        with file_path.open("r", encoding="utf-8") as file:
            return yaml.load(file, Loader=NoDatesSafeLoader)

    return {"Entries": []}


def save_yaml(data, file_path):
    file_path.parent.mkdir(parents=True, exist_ok=True)
    with file_path.open("w", encoding="utf-8") as file:
        yaml.dump(
            data, file, default_flow_style=False, allow_unicode=True, Dumper=MyDumper
        )


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

    async def fetch_single_pr(session, number):
        try:
            pr_info = await get_pr_info(session, repo, number)
            if not pr_info:
                return None

            if not pr_info.get("merged_at"):
                return None

            body = pr_info.get("body", "")
            author = pr_info["user"]["login"]
            changes = []

            lines = [line.strip() for line in body.splitlines()]
            for line in lines:
                if line.lower().startswith("no cl"):
                    return None

                if line.startswith(":cl:"):
                    potential_author = line[4:].strip()
                    if potential_author:
                        author = potential_author
                    continue

                change_types = {
                    "- add:": "Add",
                    "- remove:": "Remove",
                    "- tweak:": "Tweak",
                    "- fix:": "Fix",
                }

                for prefix, change_type in change_types.items():
                    if line.startswith(prefix):
                        changes.append(
                            {
                                "message": line[len(prefix) :].strip(),
                                "type": change_type,
                            }
                        )
                        break

            if changes:
                return {
                    "author": author,
                    "changes": changes,
                    "time": pr_info["merged_at"],
                }

        except aiohttp.ClientError as e:
            logging.error(f"Failed to fetch PR #{number}: {e}")

        return None

    async with aiohttp.ClientSession(
        headers={
            "Authorization": f"token {token}",
            "Accept": "application/vnd.github.v3+json",
        }
    ) as session:
        tasks = []
        for number in range(1, pr_number + 1):
            tasks.append(fetch_single_pr(session, number))

        with ThreadPoolExecutor(max_workers=4):
            for result in await asyncio.gather(*tasks):
                if result:
                    pr_data.append(result)

    pr_data.sort(key=lambda x: x["time"])
    return pr_data


def update_cl_file(file_path, new_data):
    if file_path.exists():
        file_path.unlink()

    old_entries = load_yaml(OLD_CHANGELOG_PATH).get("Entries", [])

    next_id = max((entry["id"] for entry in old_entries), default=0) + 1

    for entry in new_data:
        entry["id"] = next_id
        next_id += 1

    combined_data = old_entries + new_data

    combined_data = strip_newlines(combined_data)

    save_yaml({"Entries": combined_data}, file_path)

    logging.info("Updated PR data saved to 1ChangelogADT.yml")


async def main():
    if len(sys.argv) < 3:
        logging.error("Usage: auto_cl.py <GITHUB_TOKEN> <REPO>")
        sys.exit(1)

    github_token = sys.argv[1]
    repo = sys.argv[2]

    async with aiohttp.ClientSession(
        headers={
            "Authorization": f"token {github_token}",
            "Accept": "application/vnd.github.v3+json",
        }
    ) as session:
        pr_number = await get_latest_pr_number(session, repo)
        if pr_number is None:
            logging.error("Failed to get the latest PR number")
            sys.exit(1)

        pr_data = await fetch_pr_data(github_token, repo, pr_number)

    update_cl_file(CHANGELOG_PATH, pr_data)


if __name__ == "__main__":
    logging.basicConfig(level=logging.INFO)
    asyncio.run(main())
