# Скрипт для автоматического добавления категорий к TTS голосам
$ttsFile = "Resources\Prototypes\Corvax\tts-voices.yml"

# Словарь категорий по ключевым словам
$categories = @{
    # Роботы и синтетические голоса
    'Robot' = @('robot', 'bot', 'glados', 'wheatley', 'turret', 'sentry', 'ai', 'синт', 'робот')
    
    # Животные
    'Animal' = @('dog', 'cat', 'wolf', 'lion', 'tiger', 'bear', 'bird', 'собак', 'кот', 'кошк', 'волк')
    
    # Инопланетяне
    'Alien' = @('vox', 'alien', 'unath', 'skrell', 'tajaran', 'дракон', 'слайм')
    
    # Мемы и стримеры
    'Memes' = @('папич', 'бэбэй', 'мориарти', 'пучков', 'neco', 'неко', 'сквидвард', 
                'squidward', 'шрек', 'shrek', 'тони', 'tony', 'брайн', 'brian')
    
    # Особые/знаменитости/персонажи
    'Special' = @('charlotte', 'шарлотта', 'planya', 'планя', 'mana', 'мана', 'amina', 'амина',
                  'biden', 'байден', 'obama', 'обама', 'trump', 'трамп', 'gman', 'g-man',
                  'alyx', 'аликс', 'barni', 'барни', 'vance', 'вэнс', 'kleiner', 'кляйнер',
                  'mossman', 'моссман', 'grigori', 'григорий', 'briman', 'брин')
    
    # Человеческие голоса (по умолчанию для остальных)
    'Human' = @()
}

function Get-Category($name, $description, $speaker) {
    $searchText = "$name $description $speaker".ToLower()
    
    foreach ($category in $categories.Keys) {
        if ($category -eq 'Human') { continue }
        
        foreach ($keyword in $categories[$category]) {
            if ($searchText -contains $keyword) {
                return $category
            }
        }
    }
    
    return 'Human'
}

# Читаем файл
$content = Get-Content $ttsFile -Raw -Encoding UTF8

# Разбиваем на отдельные записи голосов
$voices = $content -split '(?=- type: ttsVoice)'

$newContent = ""
$modified = 0
$skipped = 0

foreach ($voice in $voices) {
    if ($voice.Trim() -eq "") { continue }
    
    # Проверяем, есть ли уже категория
    if ($voice -match 'category:') {
        $newContent += $voice
        $skipped++
        continue
    }
    
    # Извлекаем информацию
    if ($voice -match 'name:\s*(.+)') { $name = $matches[1] }
    if ($voice -match 'description:\s*(.+)') { $description = $matches[1] }
    if ($voice -match 'speaker:\s*(.+)') { $speaker = $matches[1] }
    if ($voice -match 'id:\s*(.+)') { $id = $matches[1] }
    
    # Определяем категорию
    $category = Get-Category $name $description $speaker
    
    # Добавляем категорию после id
    $voice = $voice -replace "(id:\s*.+)", "`$1`n  category: $category"
    
    $newContent += $voice
    $modified++
}

# Сохраняем файл
$newContent | Set-Content $ttsFile -Encoding UTF8 -NoNewline

Write-Host "Готово! Изменено: $modified голосов, пропущено (уже с категориями): $skipped"
Write-Host "Проверьте файл $ttsFile и при необходимости скорректируйте категории вручную."
