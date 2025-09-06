#!/usr/bin/env python3
import os
import json
import re
import requests
from datetime import datetime, timedelta

EMOJI_MAP = {
    "add": "- ✨ add:",
    "remove": "- ❌ remove:",
    "delete": "- 🗑️ delete:",
    "tweak": "- 🔧 tweak:",
    "fix": "- 🐛 fix:"
}

EMOJI_ORDER = ["add", "remove", "delete", "tweak", "fix"]
DEFAULT_COLOR = 0xE91E63  # Красный цвет эмбеда

def extract_changelog(text):
    match = re.search(r"(?:\:cl\:|🆑)\s*(.*?)\s*(?:<!--|\Z)", text, re.DOTALL)
    if not match:
        return None, None

    content = match.group(1).strip()

    # Извлекаем авторов из первой строки
    lines = content.splitlines()
    changelog_authors = None
    real_author_name = None
    if lines:
        first_line = lines[0].strip()
        # Проверяем, есть ли авторы в первой строке (не начинается с -)
        if not first_line.startswith("-") and first_line:
            # Если это одно имя без пробелов и не содержит типичные символы никнеймов
            if " " not in first_line and not any(char in first_line for char in ["@", "#", "_", "-"]):
                real_author_name = first_line
            else:
                changelog_authors = first_line
            # Убираем первую строку с авторами из контента
            content = "\n".join(lines[1:]).strip()

    groups = {key: [] for key in EMOJI_MAP.keys()}

    for line in content.splitlines():
        line = line.strip()
        if not line.startswith("-"):
            continue
        line_content = line[1:].strip()
        for key in EMOJI_MAP:
            if line_content.lower().startswith(f"{key}:"):
                desc = line_content[len(key)+1:].strip().capitalize()
                # Убираем переносы строк и лишние пробелы
                desc = re.sub(r'\s+', ' ', desc).strip()
                groups[key].append(f"{EMOJI_MAP[key]} {desc}")
                break

    if all(len(v) == 0 for v in groups.values()):
        return None, None

    grouped_output = []
    for key in EMOJI_ORDER:
        if key in groups and groups[key]:
            grouped_output.extend(groups[key])
            grouped_output.append("")

    if grouped_output and grouped_output[-1] == "":
        grouped_output.pop()

    # Очищаем финальный вывод от лишних символов
    final_output = "\n".join(grouped_output)
    # Убираем только тройные и более переносы строк, оставляя двойные для разделения категорий
    final_output = re.sub(r'\n\s*\n\s*\n+', '\n\n', final_output)
    # Убираем невидимые символы и нормализуем пробелы
    final_output = re.sub(r'[\u200b-\u200d\ufeff]', '', final_output)
    final_output = re.sub(r'[ \t]+', ' ', final_output)

    return final_output, changelog_authors, real_author_name

def create_embed(changelog, author_name, author_avatar, branch, pr_url, pr_title, merged_at, commits_count, changed_files, additions, deletions, created_at, changelog_authors=None, real_author_name=None):
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

    # Форматируем время слияния (Москва, UTC+3)
    if merged_at:
        try:
            # Парсим время и конвертируем в московское время
            utc_time = datetime.fromisoformat(merged_at.replace('Z', '+00:00'))
            moscow_time = utc_time.replace(tzinfo=None) + timedelta(hours=3)
            merged_time = moscow_time.strftime('%d.%m.%Y %H:%M МСК')
        except:
            merged_time = "Неизвестно"
    else:
        merged_time = "Неизвестно"

    # Формируем строку с авторами
    if changelog_authors:
        author_display = f"👤 **Авторы:** {changelog_authors}"
    elif real_author_name:
        author_display = f"👤 **Автор:** {real_author_name}"
    else:
        author_display = f"**Автор:** {author_name}"

    embed = {
        "title": f"🚀 Обновление: {pr_title}",
        "url": pr_url,
        "description": f"> {author_display}\n> **📊 Изменений:** +{additions} -{deletions} строк\n> **📝 Коммитов:** {commits_count}\n> **📁 Файлов:** {changed_files}\n\n{changelog}\n_ _",
        "color": color,
        "footer": {
            "text": f"{author_name} • 📅 {(datetime.utcnow() + timedelta(hours=3)).strftime('%d.%m.%Y %H:%M МСК')}",
            "icon_url": author_avatar
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
    created_at = pr.get("created_at", "")
    commits_count = pr.get("commits", 0)
    changed_files = pr.get("changed_files", 0)
    additions = pr.get("additions", 0)
    deletions = pr.get("deletions", 0)

    changelog, changelog_authors, real_author_name = extract_changelog(body)

    if not changelog:
        print("No valid changelog found. Skipping PR.")
        return

    embed = create_embed(changelog, author, avatar_url, branch, pr_url, pr_title, merged_at, commits_count, changed_files, additions, deletions, created_at, changelog_authors, real_author_name)

    headers = {"Content-Type": "application/json"}
    payload = {"embeds": [embed]}
    response = requests.post(webhook_url, headers=headers, data=json.dumps(payload))
    if response.status_code >= 400:
        print(f"❌ Failed to send webhook: {response.status_code} - {response.text}")
    else:
        print("✅ Webhook sent successfully.")

if __name__ == "__main__":
    main()
