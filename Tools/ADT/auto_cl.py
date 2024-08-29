import logging
import os
import re
import sys
from concurrent.futures import ThreadPoolExecutor, as_completed
from datetime import datetime

import requests
import yaml

MAX_ENTRIES = 500
CHANGELOG_PATH = os.path.join(os.path.dirname(__file__), '../../Resources/Changelog/ChangelogADT.yml')
OLD_CHANGELOG_PATH = os.path.join(os.path.dirname(__file__), 'cl_old.yml')

class NoDatesSafeLoader(yaml.SafeLoader):
    @classmethod
    def remove_implicit_resolver(cls, tag_to_remove):
        if 'yaml_implicit_resolvers' not in cls.__dict__:
            cls.yaml_implicit_resolvers = cls.yaml_implicit_resolvers.copy()
        for first_letter, mappings in cls.yaml_implicit_resolvers.items():
            cls.yaml_implicit_resolvers[first_letter] = [(tag, regexp) for tag, regexp in mappings if tag != tag_to_remove]

NoDatesSafeLoader.remove_implicit_resolver('tag:yaml.org,2002:timestamp')

def represent_dict_compact(dumper, data):
    if len(data) == 2 and "message" in data and "type" in data:
        return dumper.represent_mapping('tag:yaml.org,2002:map', data.items(), flow_style=True)
    return dumper.represent_mapping('tag:yaml.org,2002:map', data.items())

def represent_multiline_string(dumper, data):
    if '\n' in data:
        return dumper.represent_scalar('tag:yaml.org,2002:str', data, style='|')
    return dumper.represent_scalar('tag:yaml.org,2002:str', data)

class MyDumper(yaml.SafeDumper):
    def increase_indent(self, flow=False, indentless=False):
        return super(MyDumper, self).increase_indent(flow, False)

MyDumper.add_representer(dict, represent_dict_compact)
MyDumper.add_representer(str, represent_multiline_string)

def get_pr_info(token, repo, pr_number):
    url = f"https://api.github.com/repos/{repo}/pulls/{pr_number}"
    headers = {
        "Authorization": f"token {token}",
        "Accept": "application/vnd.github.v3+json"
    }
    response = requests.get(url, headers=headers)
    if response.status_code == 200:
        return response.json()
    else:
        response.raise_for_status()

def get_latest_pr_number(token, repo):
    url = f"https://api.github.com/repos/{repo}/pulls?state=all&sort=created&direction=desc&per_page=1"
    headers = {
        "Authorization": f"token {token}",
        "Accept": "application/vnd.github.v3+json"
    }
    response = requests.get(url, headers=headers)
    if response.status_code == 200:
        pr_list = response.json()
        if pr_list:
            return pr_list[0]['number']
    response.raise_for_status()

def load_yaml(file_path):
    if os.path.exists(file_path):
        with open(file_path, 'r', encoding='utf-8') as file:
            return yaml.load(file, Loader=NoDatesSafeLoader)
    return {"Entries": []}

def save_yaml(data, file_path):
    os.makedirs(os.path.dirname(file_path), exist_ok=True)
    with open(file_path, 'w', encoding='utf-8') as file:
        yaml.dump(data, file, default_flow_style=False, allow_unicode=True, Dumper=MyDumper)

def strip_newlines(data):
    if isinstance(data, dict):
        return {k: strip_newlines(v) for k, v in data.items()}
    elif isinstance(data, list):
        return [strip_newlines(v) for v in data]
    elif isinstance(data, str):
        return data.replace('\n', ' ').replace('\r', ' ')
    else:
        return data

def fetch_pr_data(token, repo, pr_number):
    pr_data = []

    def fetch_single_pr(number):
        try:
            pr_info = get_pr_info(token, repo, number)

            # Проверяем, что PR был замержен
            if not pr_info.get('merged_at'):
                return None

            body = pr_info.get('body', '')
            author = pr_info['user']['login']
            changes = []

            lines = [line.strip() for line in body.splitlines()]
            for line in lines:
                if line.lower().startswith('no cl'):
                    return None

                if line.startswith(':cl:'):
                    potential_author = line[4:].strip()
                    if potential_author:
                        author = potential_author
                    continue

                change_types = {
                    '- add:': "Add",
                    '- remove:': "Remove",
                    '- tweak:': "Tweak",
                    '- fix:': "Fix"
                }

                for prefix, change_type in change_types.items():
                    if line.startswith(prefix):
                        changes.append({"message": line[len(prefix):].strip(), "type": change_type})
                        break

            if changes:
                return {
                    "author": author,
                    "changes": changes,
                    "time": pr_info['merged_at']
                }
        except requests.exceptions.RequestException as e:
            logging.error(f"Failed to fetch PR #{number}: {e}")

        return None

    with ThreadPoolExecutor(max_workers=10) as executor:
        futures = [executor.submit(fetch_single_pr, number) for number in range(1, pr_number + 1)]
        for future in as_completed(futures):
            pr_info = future.result()
            if pr_info:
                pr_data.append(pr_info)

    # Сортируем по времени мерджа
    pr_data.sort(key=lambda x: x['time'])

    return pr_data

def update_cl_file(file_path, new_data):
    if os.path.exists(file_path):
        os.remove(file_path)

    old_entries = load_yaml(OLD_CHANGELOG_PATH).get("Entries", [])

    # Calculate the next ID based on existing entries
    if old_entries:
        next_id = max(entry['id'] for entry in old_entries) + 1
    else:
        next_id = 1

    # Add IDs to the new data
    for entry in new_data:
        entry['id'] = next_id
        next_id += 1

    # Combine old entries and new data
    combined_data = old_entries + new_data

    # Ensure we do not exceed MAX_ENTRIES
    if len(combined_data) > MAX_ENTRIES:
        combined_data = combined_data[-MAX_ENTRIES:]

    # Strip newlines from all data
    combined_data = strip_newlines(combined_data)

    # Save the updated data back to ChangelogADT.yml
    save_yaml({"Entries": combined_data}, file_path)

    logging.info("Updated PR data saved to ChangelogADT.yml")

def main():
    if len(sys.argv) < 3:
        logging.error("Usage: auto_cl.py <GITHUB_TOKEN> <REPO>")
        sys.exit(1)

    github_token = sys.argv[1]
    repo = sys.argv[2]

    pr_number = get_latest_pr_number(github_token, repo)
    if pr_number is None:
        logging.error("Failed to get the latest PR number")
        sys.exit(1)

    pr_data = fetch_pr_data(github_token, repo, pr_number)

    update_cl_file(CHANGELOG_PATH, pr_data)

if __name__ == "__main__":
    logging.basicConfig(level=logging.INFO)
    main()
