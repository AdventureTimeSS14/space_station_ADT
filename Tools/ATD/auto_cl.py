import re
import yaml
import logging
import os
import sys
import requests
from datetime import datetime

MAX_ENTRIES = 500
CHANGELOG_PATH = os.path.join(os.path.dirname(__file__), '../../Resources/Changelog/ChangelogADT.yml')

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
    for number in range(1, pr_number + 1):
        try:
            pr_info = get_pr_info(token, repo, number)

            # Проверяем, что PR был замержен
            if not pr_info.get('merged_at'):
                continue

            body = pr_info.get('body', '')
            cl_match = re.search(r':cl:\s*(.*)', body)
            if not cl_match:
                continue
            
            author = cl_match.group(1).strip() or pr_info['user']['login']

            changes = []
            for line in body.splitlines():
                line = line.strip()
                if line.startswith('- add:'):
                    changes.append({"message": line[len('- add:'):].strip(), "type": "Add"})
                elif line.startswith('- remove:'):
                    changes.append({"message": line[len('- remove:'):].strip(), "type": "Remove"})
                elif line.startswith('- tweak:'):
                    changes.append({"message": line[len('- tweak:'):].strip(), "type": "Tweak"})
                elif line.startswith('- fix:'):
                    changes.append({"message": line[len('- fix:'):].strip(), "type": "Fix"})

            if changes:
                pr_data.append({
                    "author": author,
                    "changes": changes,
                    "time": pr_info['merged_at']
                })
                logging.info(f"Fetched PR #{number}")

        except requests.exceptions.RequestException as e:
            logging.error(f"Failed to fetch PR #{number}: {e}")
    
    return pr_data

def update_cl_file(file_path, new_data):
    cl_old_data = load_yaml(file_path)
    existing_entries = cl_old_data.get("Entries", [])
    
    # Calculate the next ID based on existing entries
    if existing_entries:
        next_id = max(entry['id'] for entry in existing_entries) + 1
    else:
        next_id = 1

    # Add IDs to the new data
    for entry in new_data:
        entry['id'] = next_id
        next_id += 1

    # Combine and reorder entries
    combined_data = existing_entries + new_data

    # Ensure we do not exceed MAX_ENTRIES
    if len(combined_data) > MAX_ENTRIES:
        combined_data = combined_data[-MAX_ENTRIES:]

    # Strip newlines from all data
    combined_data = strip_newlines(combined_data)

    # Save the updated data back to ChangelogADT.yml
    cl_old_data["Entries"] = combined_data
    save_yaml(cl_old_data, file_path)

    logging.info("Updated PR data saved to ChangelogADT.yml")

def main():
    if len(sys.argv) != 4:
        logging.error("Usage: auto_cl.py <GITHUB_TOKEN> <REPO> <PR_NUMBER>")
        sys.exit(1)

    github_token = sys.argv[1]
    repo = sys.argv[2]
    pr_number = int(sys.argv[3])

    pr_data = fetch_pr_data(github_token, repo, pr_number)

    update_cl_file(CHANGELOG_PATH, pr_data)

if __name__ == "__main__":
    logging.basicConfig(level=logging.INFO)
    main()
