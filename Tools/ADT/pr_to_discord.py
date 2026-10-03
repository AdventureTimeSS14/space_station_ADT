#!/usr/bin/env python3
import os
import json
import re
import requests
from datetime import datetime, timedelta, timezone

EMOJI_MAP = {
    "add": "✨",
    "remove": "❌",
    "delete": "🗑️",
    "tweak": "🔧",
    "fix": "🐛"
}

EMOJI_ORDER = ["add", "remove", "delete", "tweak", "fix"]
DEFAULT_COLOR = 0xE91E63  # Розовый по умолчанию

DESCRIPTION_LIMIT = 4096

def extract_changelog(text):
    match = re.search(r"(?:\:cl\:|🆑)\s*(.*?)\s*(?:<!--|\Z)", text, re.DOTALL)
    if not match:
        return None, None, None

    content = match.group(1).strip()
    lines = content.splitlines()
    changelog_authors = None
    real_author_name = None

    if lines:
        first_line = lines[0].strip()
        if not first_line.startswith("-") and first_line:
            if " " not in first_line and not any(char in first_line for char in ["@", "#", "_", "-"]):
                real_author_name = first_line
            else:
                changelog_authors = first_line
            content = "\n".join(lines[1:]).strip()

    groups = {key: [] for key in EMOJI_MAP.keys()}

    for line in content.splitlines():
        line = line.strip()
        if not line.startswith("-"):
            continue
        line_content = line[1:].strip()
        for key in EMOJI_MAP:
            if line_content.lower().startswith(f"{key}:"):
                desc = line_content[len(key) + 1:].strip()
                desc = desc[:1].upper() + desc[1:] if desc else desc
                desc = re.sub(r'\s+', ' ', desc).strip()
                groups[key].append(f"{EMOJI_MAP[key]} {desc}")
                break

    if all(len(v) == 0 for v in groups.values()):
        return None, None, None

    entries = []
    for key in EMOJI_ORDER:
        if groups[key]:
            entries.extend(groups[key])
            entries.append("")

    if entries and entries[-1] == "":
        entries.pop()

    return entries, changelog_authors, real_author_name


def chunk_entries(entries, header, limit=DESCRIPTION_LIMIT):
    chunks = []
    current_lines = []
    current_len = len(header) if header else 0

    def flush():
        nonlocal current_lines, current_len
        text = "\n".join(current_lines).strip()
        text = re.sub(r'\n\s*\n\s*\n+', '\n\n', text)
        if text:
            chunks.append(text)
        current_lines = []
        current_len = 0

    for entry in entries:
        add_len = len(entry) + 1
        if current_lines and current_len + add_len > limit:
            flush()
        current_lines.append(entry)
        current_len += add_len

    if current_lines:
        flush()

    if not chunks:
        chunks = [""]

    return chunks


def create_embeds(entries, author_name, author_avatar, pr_url, pr_title, merged_at,
                   changelog_authors=None, real_author_name=None):
    joined = "\n".join(entries)
    if "✨" in joined and "❌" not in joined:
        color = 0x4CAF50
    elif "❌" in joined and "✨" not in joined:
        color = 0xF44336
    elif "🔧" in joined:
        color = 0xFF9800
    else:
        color = DEFAULT_COLOR

    if changelog_authors:
        author_display = f"👥 **Авторы:** {changelog_authors}"
    elif real_author_name:
        author_display = f"👤 **Автор:** {real_author_name}"
    else:
        author_display = f"👤 **Автор:** {author_name}"

    header = f"{author_display}\n\n"
    footer_tail = "\n_ _"

    effective_limit = DESCRIPTION_LIMIT - len(footer_tail)
    chunks = chunk_entries(entries, header, limit=effective_limit)

    embeds = []
    total = len(chunks)
    for i, chunk in enumerate(chunks, start=1):
        if i == 1:
            description = f"{header}{chunk}{footer_tail}"
            title = f"🚀 Обновление: {pr_title}"
        else:
            description = f"{chunk}{footer_tail}"
            title = f"🚀 Обновление: {pr_title} ({i}/{total})"

        embed = {
            "title": title,
            "url": pr_url,
            "description": description,
            "color": color,
        }

        if i == total:
            embed["footer"] = {
                "text": f"{author_name} • 📅 {(datetime.now(timezone.utc) + timedelta(hours=3)).strftime('%d.%m.%Y %H:%M МСК')}",
                "icon_url": author_avatar
            }

        embeds.append(embed)

    return embeds


def main():
    event_path = os.environ.get("GITHUB_EVENT_PATH")
    bot_token = os.environ.get("DISCORD_BOT_TOKEN")
    channel_id = 1089490875182239754

    if not event_path or not bot_token or not channel_id:
        print("❌ Missing required environment variables.")
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
    pr_url = pr.get("html_url", "")
    pr_title = pr.get("title", "")
    merged_at = pr.get("merged_at", "")

    entries, changelog_authors, real_author_name = extract_changelog(body)
    if not entries:
        print("No valid changelog found. Skipping PR.")
        return

    embeds = create_embeds(
        entries, author, avatar_url, pr_url, pr_title, merged_at,
        changelog_authors, real_author_name
    )

    headers = {
        "Authorization": f"Bot {bot_token}",
        "Content-Type": "application/json"
    }

    message_ids = []
    api_url = f"https://discord.com/api/v10/channels/{channel_id}/messages"
    for i in range(0, len(embeds), 10):
        batch = embeds[i:i + 10]
        payload = {"embeds": batch}
        response = requests.post(api_url, headers=headers, data=json.dumps(payload))

        if response.status_code >= 400:
            print(f"❌ Failed to send message: {response.status_code} - {response.text}")
            return

        message = response.json()
        message_id = message.get("id")
        message_ids.append(message_id)
        print(f"✅ Message sent! ID: {message_id}")

    for message_id in message_ids:
        crosspost_url = f"https://discord.com/api/v10/channels/{channel_id}/messages/{message_id}/crosspost"
        publish_response = requests.post(crosspost_url, headers=headers)

        if publish_response.status_code == 200:
            print(f"📢 Message {message_id} published to news channel!")
        elif publish_response.status_code == 403:
            print("⚠️ Bot doesn't have permission to publish messages")
        elif publish_response.status_code == 400:
            print("⚠️ Channel is not a news channel or message already published")
        else:
            print(f"⚠️ Failed to publish message: {publish_response.status_code} - {publish_response.text}")


if __name__ == "__main__":
    main()
