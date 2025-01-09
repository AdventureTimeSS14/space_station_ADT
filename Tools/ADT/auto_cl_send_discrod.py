import sys
import re
import disnake
from datetime import datetime, timezone
import logging
import asyncio
from disnake.ext import commands

# Устанавливаем максимальную длину для поля Embed
MAX_FIELD_LENGTH = 1024  # Максимальный размер поля для Embed
CHANGELOG_CHANNEL_ID = 1089490875182239754 # ID канала для чейнжлогов

# Создаем бота
bot = commands.Bot(command_prefix="!")

# Умная обрезка текста
def smart_truncate(text, max_length):
    """Умная обрезка текста: обрезает до максимальной длины, не разрывая слова или предложения."""
    if len(text) <= max_length:
        return text

    truncated_text = text[:max_length]
    last_period = truncated_text.rfind(".")
    if last_period == -1:
        return truncated_text[:max_length].strip() + "..."
    return truncated_text[:last_period + 1].strip() + "..."

# Функция для получения данных с GitHub
async def fetch_github_data(url, params=None):
    """Выполняет запрос к GitHub API и возвращает данные."""
    try:
        response = await bot.session.get(url, params=params)
        if response.status != 200:
            print(f"❌ Ошибка при запросе данных с GitHub: {response.status}")
            return None
        return await response.json()
    except Exception as e:
        print(f"❌ Ошибка при выполнении запроса к GitHub API: {e}")
        return None

# Извлекаем изменения из описания пулл-реквеста
def extract_pull_request_changes(description: str):
    """Извлекает текст изменений из описания пулл-реквеста."""
    description = re.sub(r"<!--.*?-->", "", description, flags=re.DOTALL)
    match = re.search(r"(:cl:.*?|\U0001F191.*?)(\n|$)", description, re.DOTALL)

    if not match:
        return None, None  # Если описание изменений не найдено

    cl_text = match.group(1).strip()
    remaining_lines = description[match.end():].strip()
    full_description = f"{cl_text}\n{remaining_lines}" if remaining_lines else cl_text
    return full_description, match

# Отправляем пулл-реквест в Discord канал
async def send_pull_request_to_disnake(pr, description, pr_title, pr_url, coauthors):
    """Отправляет информацию о замерженном пулл-реквесте в Discord канал CHANGELOG."""
    embed = disnake.Embed(
        title=f"Пулл-реквест замержен: {pr_title}",
        color=disnake.Color.dark_green(),
        timestamp=datetime.strptime(pr["merged_at"], "%Y-%m-%dT%H:%M:%SZ").replace(tzinfo=timezone.utc),
    )
    embed.add_field(name="Изменения:", value=description, inline=False)
    embed.add_field(name="Автор:", value=pr["user"]["login"], inline=False)
    pr_number = pr["number"]
    embed.add_field(
        name="Ссылка:",
        value=f"[PR #{pr_number}]({pr_url})",
        inline=False,
    )
    if coauthors:
        coauthors_str = "\n".join(coauthors)
        embed.add_field(name="Соавторы:", value=coauthors_str, inline=False)
    embed.set_footer(text="Дата мержа")

    changelog_channel = bot.get_channel(CHANGELOG_CHANNEL_ID)
    if changelog_channel is None:
        print(f"❌ Канал с ID {CHANGELOG_CHANNEL_ID} не найден.")
        return

    try:
        await changelog_channel.send(embed=embed)
        print(f"✅ Информация о замерженном PR #{pr_number} опубликована в CHANGELOG.")
    except disnake.Forbidden:
        print(f"❌ У бота нет прав для отправки сообщений в канал с ID {CHANGELOG_CHANNEL_ID}.")
    except disnake.HTTPException as e:
        print(f"❌ Ошибка при отправке Embed: {e}")

# Получаем последний замерженный пулл-реквест
async def post_last_merged_pull_request():
    """Получает последний замерженный пулл-реквест и отправляет информацию о нем в канал CHANGELOG."""
    url = f'https://api.github.com/repos/AdventureTimeSS14/space_station_ADT/pulls?state=closed'

    pull_requests = await fetch_github_data(url, {"Accept": "application/vnd.github.v3+json"})

    if not pull_requests:
        print("❌ Пулл-реквесты не найдены или произошла ошибка при запросе.")
        return

    merged_prs = [pr for pr in pull_requests if pr.get("merged_at")]
    if not merged_prs:
        print("❌ Нет замерженных пулл-реквестов.")
        return

    latest_pr = sorted(merged_prs, key=lambda pr: pr["merged_at"], reverse=True)[0]

    merged_at = datetime.strptime(latest_pr["merged_at"], "%Y-%m-%dT%H:%M:%SZ").replace(tzinfo=timezone.utc)
    pr_title = latest_pr["title"]
    pr_url = latest_pr["html_url"]
    description = latest_pr.get("body", "").strip()
    coauthors = latest_pr.get("coauthors", [])

    description, _ = extract_pull_request_changes(description)
    if not description:
        print(f"⚠️ Описание изменений для PR #{latest_pr['number']} не найдено.")
        return

    description = smart_truncate(description, MAX_FIELD_LENGTH)

    await send_pull_request_to_disnake(latest_pr, description, pr_title, pr_url, coauthors)

# Основная функция для запуска бота
async def main():
    token = sys.argv[2]
    bot.run(token)
    await post_last_merged_pull_request()
    await bot.close()  # Завершаем работу бота после выполнения отправки

if __name__ == "__main__":
    logging.basicConfig(level=logging.INFO)
    asyncio.run(main())
