#!/usr/bin/env python3
import os
import json
import re
import requests
from datetime import datetime

EMOJI_MAP = {
    "add": "✨",
    "remove": "❌",
    "delete": "🗑️",
    "tweak": "🔧",
    "fix": "🐛"
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

def create_embed(changelog, author_name, author_avatar, branch, pr_url, pr_title, merged_at, commits_count, changed_files):
    # Подсчитываем количество изменений
    change_count = len([line for line in changelog.split('\n') if line.strip() and not line.startswith('**')])

    # Определяем цвет в зависимости от типа изменений
    if "✨" in changelog and "❌" not in changelog:
        color = 0x4CAF50  # Зеленый для добавлений
    elif "❌" in changelog and "✨" not in changelog:
        color = 0xF44336  # Красный для удалений
    elif "🔧" in changelog:
        color = 0xFF9800  # Оранжевый для исправлений
    else:
        color = DEFAULT_COLOR  # Розовый по умолчанию

    # Форматируем время слияния
    merged_time = datetime.fromisoformat(merged_at.replace('Z', '+00:00')).strftime('%d.%m.%Y %H:%M UTC')

    embed = {
        "title": f"🚀 Обновление: {pr_title}",
        "url": pr_url,
        "description": f"**👤 Автор:** {author_name}\n**🌿 Ветка:** {branch}\n**📊 Изменений:** {change_count}\n**🕒 Слит:** {merged_time}\n**📝 Коммитов:** {commits_count}\n**📁 Файлов:** {changed_files}\n\n{changelog}",
        "color": color,
        "footer": {
            "text": f"📅 {datetime.utcnow().strftime('%d.%m.%Y %H:%M UTC')}",
            "icon_url": author_avatar
        },
        "thumbnail": {
            "url": author_avatar
        }
    }
    return embed

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
    pr_url = pr.get("html_url", "")
    pr_title = pr.get("title", "")
    merged_at = pr.get("merged_at", "")
    commits_count = pr.get("commits", 0)
    changed_files = pr.get("changed_files", 0)

    changelog = extract_changelog(body)

    if not changelog:
        print("No valid changelog found. Skipping PR.")
        return

    embed = create_embed(changelog, author, avatar_url, branch)

    headers = {"Content-Type": "application/json"}
    payload = {"embeds": [embed]}
    response = requests.post(webhook_url, headers=headers, data=json.dumps(payload))
    if response.status_code >= 400:
        print(f"❌ Failed to send webhook: {response.status_code} - {response.text}")
    else:
        print("✅ Webhook sent successfully.")

if __name__ == "__main__":
    main()
