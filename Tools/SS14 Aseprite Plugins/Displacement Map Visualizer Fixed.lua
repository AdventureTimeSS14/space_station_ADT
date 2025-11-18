-- =========================================
-- DISPLACEMENT MAP VISUALIZER (FIXED)
-- =========================================
-- Исправленная версия скрипта для работы с ТЕКУЩИМ кадром
-- Используйте клавиши , и . для переключения между кадрами
-- Автор: PJB3005 (оригинал), исправления для текущего кадра
-- =========================================

local scale = 4  -- Начальный масштаб
local canvasWidth = 256  -- Начальная ширина canvas (небольшая)
local canvasHeight = 200  -- Уменьшенная высота canvas
local offsetX = 0  -- Смещение view по X
local offsetY = 0  -- Смещение view по Y
local isDragging = false  -- Состояние перетаскивания
local lastMouseX = 0
local lastMouseY = 0

-- Проверка UI
if not app.isUIAvailable then
    return
end

-- Функция получения пикселя с учётом смещения
local getOffsetPixel = function(x, y, image, rect)
    local posX = x - rect.x
    local posY = y - rect.y

    if posX < 0 or posX >= image.width or posY < 0 or posY >= image.height then
        return image.spec.transparentColor
    end

    return image:getPixel(posX, posY)
end

-- Конвертация значения пикселя в цвет
local pixelValueToColor = function(sprite, value)
    return Color(value)
end

-- Применение displacement map
local applyDisplacementMap = function(width, height, size, displacement, displacementRect, target, targetRect)
    local image = target:clone()
    image:resize(width, height)
    image:clear()

    for x = 0, width - 1 do
        for y = 0, height - 1 do
            local value = getOffsetPixel(x, y, displacement, displacementRect)
            local color = pixelValueToColor(sprite, value)

            if color.alpha ~= 0 then
                -- Вычисление смещения на основе R и G каналов
                local offset_x = (color.red - 128) / 127 * size
                local offset_y = (color.green - 128) / 127 * size

                local colorValue = getOffsetPixel(x + offset_x, y + offset_y, target, targetRect)
                image:drawPixel(x, y, colorValue)
            end
        end
    end

    return image
end

local dialog = nil

local sprite = app.editor.sprite
local spriteChanged = sprite.events:on("change",
    function(ev)
        if dialog then
            dialog:repaint()
        end
    end)

-- Получение списка слоёв
local layers = {}
for i,layer in ipairs(sprite.layers) do
    table.insert(layers, 1, layer.name)
end

-- Поиск слоя по имени
local findLayer = function(sprite, name)
    for i, layer in ipairs(sprite.layers) do
        if layer.name == name then
            return layer
        end
    end
    return nil
end

-- Режим рисования
local paintMode = {
    active = false,
    offsetX = 0,
    offsetY = 0,
    selectionBounds = nil  -- Сохраняем границы выделения
}

-- Применение смещения к выделенной области в displacement layer
local applyOffsetToSelection = function(dx, dy)
    local currentFrame = app.frame or sprite.frames[1]
    local layerDisplacement = findLayer(sprite, dialog.data["displacement-select"])
    
    if not layerDisplacement then
        app.alert("Выберите displacement слой!")
        return
    end
    
    local celDisplacement = layerDisplacement:cel(currentFrame)
    if not celDisplacement then
        app.alert("Нет cel displacement на текущем кадре!")
        return
    end
    
    local selection = sprite.selection
    
    -- Проверка выделения
    if selection.isEmpty then
        app.alert("Сделайте выделение (M)! Выделите область, которую хотите сместить.")
        return
    end
    
    local image = celDisplacement.image:clone()
    
    -- Применение смещения к каждому пикселю в выделении
    for x = selection.bounds.x, selection.bounds.x + selection.bounds.width - 1 do
        for y = selection.bounds.y, selection.bounds.y + selection.bounds.height - 1 do
            local xImg = x - celDisplacement.position.x
            local yImg = y - celDisplacement.position.y
            
            if xImg >= 0 and xImg < image.width and yImg >= 0 and yImg < image.height then
                local pixelValue = image:getPixel(xImg, yImg)
                local color = Color(pixelValue)

                -- Изменение R и G каналов
                color.red = math.min(255, math.max(0, color.red + dx))
                color.green = math.min(255, math.max(0, color.green + dy))

                image:drawPixel(xImg, yImg, app.pixelColor.rgba(color.red, color.green, color.blue, color.alpha))
            end
        end
    end
    
    celDisplacement.image = image
    if dialog then
        dialog:repaint()
    end
end

-- Создание диалогового окна
dialog = Dialog{
    title = "Displacement Map Preview (ТЕКУЩИЙ КАДР)",
    onclose = function(ev)
        sprite.events:off(spriteChanged)
    end
}

-- Canvas для предпросмотра
dialog:canvas{
    id = "canvas",
    width = canvasWidth,
    height = canvasHeight,
    onpaint = function(ev)
        local context = ev.context
        
        -- ИСПРАВЛЕНИЕ: Получаем ТЕКУЩИЙ кадр, а не всегда первый
        local currentFrame = app.frame or sprite.frames[1]

        local layerDisplacement = findLayer(sprite, dialog.data["displacement-select"])
        local layerTarget = findLayer(sprite, dialog.data["reference-select"])
        local layerBackground = findLayer(sprite, dialog.data["background-select"])
        
        if not layerDisplacement or not layerTarget or not layerBackground then
            context:fillText("Выберите слои!", 10, 10)
            return
        end

        -- ИСПРАВЛЕНИЕ: Используем текущий кадр
        local celDisplacement = layerDisplacement:cel(currentFrame)
        local celTarget = layerTarget:cel(currentFrame)
        local celBackground = layerBackground:cel(currentFrame)
        
        -- Проверка наличия cel
        if not celDisplacement or not celTarget or not celBackground then
            context:fillText("Нет cel на текущем кадре!", 10, 10)
            context:fillText("Используйте , и . для переключения кадров", 10, 30)
            return
        end

        -- Рисуем фон (с учётом смещения view)
        context:drawImage(
            celBackground.image,
            0, 0,
            celBackground.image.width, celBackground.image.height,
            celBackground.position.x * scale + offsetX, celBackground.position.y * scale + offsetY,
            celBackground.image.width * scale, celBackground.image.height * scale)

        -- Применяем displacement map
        local image = applyDisplacementMap(
            sprite.width, sprite.height,
            dialog.data["size"],
            celDisplacement.image, celDisplacement.bounds,
            celTarget.image, celTarget.bounds)

        -- Рисуем результат (с учётом смещения view)
        context:drawImage(
            image,
            0, 0,
            image.width, image.height,
            offsetX, offsetY,
            image.width * scale, image.height * scale)
            
        -- Рисуем выделение, если оно есть (с учётом смещения view)
        local selection = sprite.selection
        if not selection.isEmpty then
            context.color = Color{r=255, g=255, b=0, a=128}
            context:strokeRect(
                Rectangle(
                    selection.bounds.x * scale + offsetX,
                    selection.bounds.y * scale + offsetY,
                    selection.bounds.width * scale,
                    selection.bounds.height * scale
                )
            )
        end
    end,
    onwheel = function(ev)
        local oldScale = scale
        
        -- Zoom на колёсико мыши
        if ev.deltaY < 0 then
            -- Прокрутка вверх - увеличить
            scale = math.min(16, scale + 1)
        else
            -- Прокрутка вниз - уменьшить
            scale = math.max(1, scale - 1)
        end
        
        -- Центрируем зум на позицию мыши
        if oldScale ~= scale then
            local scaleRatio = scale / oldScale
            offsetX = (offsetX - ev.x) * scaleRatio + ev.x
            offsetY = (offsetY - ev.y) * scaleRatio + ev.y
        end
        
        dialog:repaint()
        return true
    end,
    onmousedown = function(ev)
        if ev.button == MouseButton.LEFT then
            isDragging = true
            lastMouseX = ev.x
            lastMouseY = ev.y
        elseif ev.button == MouseButton.MIDDLE then
            -- Средняя кнопка - сброс позиции
            offsetX = 0
            offsetY = 0
            dialog:repaint()
        end
    end,
    onmouseup = function(ev)
        if ev.button == MouseButton.LEFT then
            isDragging = false
        end
    end,
    onmousemove = function(ev)
        if isDragging then
            local dx = ev.x - lastMouseX
            local dy = ev.y - lastMouseY
            
            offsetX = offsetX + dx
            offsetY = offsetY + dy
            
            lastMouseX = ev.x
            lastMouseY = ev.y
            
            dialog:repaint()
        end
    end
}

-- Выбор слоя displacement map
dialog:combobox{
    id = "displacement-select",
    label = "Displacement слой",
    options = layers,
    onchange = function(ev)
        dialog:repaint()
    end
}

-- Выбор слоя с предметом/одеждой
dialog:combobox{
    id = "reference-select",
    label = "Предмет/Одежда",
    options = layers,
    onchange = function(ev)
        dialog:repaint()
    end
}

-- Выбор фонового слоя
dialog:combobox{
    id = "background-select",
    label = "Фон (тело)",
    options = layers,
    onchange = function(ev)
        dialog:repaint()
    end
}

-- Размер displacement
dialog:slider{
    id = "size",
    label = "Размер displacement",
    min = 127,
    max = 127,
    value = 127,
    onchange = function(ev)
        dialog:repaint()
    end
}

dialog:separator{text = "УПРАВЛЕНИЕ (выделите область в Aseprite)"}

-- Кнопки направлений
dialog:button{
    id = "moveUp",
    text = "▲ Вверх",
    onclick = function(ev)
        applyOffsetToSelection(0, 1)
    end
}

dialog:button{
    id = "moveDown",
    text = "▼ Вниз",
    onclick = function(ev)
        applyOffsetToSelection(0, -1)
    end
}

dialog:button{
    id = "moveLeft",
    text = "◄ Влево",
    onclick = function(ev)
        applyOffsetToSelection(1, 0)
    end
}

dialog:button{
    id = "moveRight",
    text = "► Вправо",
    onclick = function(ev)
        applyOffsetToSelection(-1, 0)
    end
}

dialog:show{wait = false}
