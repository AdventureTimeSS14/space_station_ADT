#!/usr/bin/env python3
import os
import json
import re
import requests
from datetime import datetime

EMOJI_MAP = {
    "add": "<:new1:1376955864698585248>",
    "remove": "<:remove1:1376955857438376036>",
    "delete": "<:remove1:1376955857438376036>",
    "tweak": "<:tweak:1376955868028993658>",
    "fix": "<:fix:1376955860030459985>"
}

EMOJI_ORDER = ["add", "remove", "delete", "tweak", "fix"]
DEFAULT_COLOR = 0xE91E63  # Красный цвет эмбеда

def extract_changelog(text):
    match = re.search(r":cl:\s*(.*?)\s*(?:<!--|\Z)", text, re.DOTALL)
    if not match:
        return None

    content = match.group(1).strip()
    groups = {key: [] for key in EMOJI_MAP.keys()}

    for line in content.splitlines():
        line = line.strip()
        if not line.startswith("-"):
            continue
        line_content = line[1:].strip()
        for key in EMOJI_MAP:
            if line_content.lower().startswith(f"{key}:"):
                desc = line_content[len(key)+1:].strip().capitalize()
                groups[key].append(f"{EMOJI_MAP[key]} {desc}")
                break

    if all(len(v) == 0 for v in groups.values()):
        return None

    grouped_output = []
    for key in EMOJI_ORDER:
        if key in groups and groups[key]:
            grouped_output.extend(groups[key])
            grouped_output.append("")

    if grouped_output and grouped_output[-1] == "":
        grouped_output.pop()
    return "\n".join(grouped_output)

def split_changelog_by_items(changelog, chunk_size=4096):
    parts = []
    current = ""

    items = changelog.split("\n\n")
    for item in items:
        item = item.strip()
        if not item:
            continue
        item_text = item + "\n\n"

        if len(item_text) > chunk_size:
            start = 0
            while start < len(item_text):
                end = start + chunk_size
                chunk = item_text[start:end]
                if len(current) + len(chunk) > chunk_size:
                    if current:
                        parts.append(current)
                    current = ""
                current += chunk
                start = end
        else:
            if len(current) + len(item_text) > chunk_size:
                if current:
                    parts.append(current)
                current = ""
            current += item_text

    if current:
        parts.append(current)

    return parts

def create_embeds(changelog, author_name, author_avatar, branch):
    chunks = split_changelog_by_items(changelog)
    embeds = []

    for i, part in enumerate(chunks):
        embed = {
            "description": f"\u200b\n{part}",
            "color": DEFAULT_COLOR,
            "footer": {
                "text": f"🆑 {author_name}, {datetime.utcnow().strftime('%d.%m.%Y %H:%M UTC')}",
                "icon_url": author_avatar
            }
        }
        if i == 0:
            embed["title"] = "⭐ Обновление"
        embeds.append(embed)

    return embeds

def main():
    event_path = os.environ.get("GITHUB_EVENT_PATH")
    webhook_url = os.environ.get("DISCORD_WEBHOOK")

    if not event_path or not webhook_url:
        print("Missing required environment variables.")
        return

    with open(event_path, 'r', encoding='utf-8') as f:
        event = json.load(f)

    pr = event.get("pull_request")
    if not pr or not pr.get("merged"):
        print("PR not merged or no pull request data.")
        return

    body = pr.get("body", "")
    author = pr.get("user", {}).get("login", "Unknown")
    avatar_url = pr.get("user", {}).get("avatar_url", "")
    branch = pr.get("base", {}).get("ref", "master")

    changelog = extract_changelog(body)
    if not changelog:
        print("No valid changelog found. Skipping PR.")
        return

    embeds = create_embeds(changelog, author, avatar_url, branch)

    headers = {"Content-Type": "application/json"}

    for embed in embeds:
        payload = {"embeds": [embed]}
        response = requests.post(webhook_url, headers=headers, data=json.dumps(payload))
        if response.status_code >= 400:
            print(f"❌ Failed to send webhook: {response.status_code} - {response.text}")
            return
    print("✅ Webhook sent successfully.")

if __name__ == "__main__":
    main()