@echo off
REM Проверка наличия Python
where python >nul 2>nul
if errorlevel 1 (
    echo Python не найден. Убедитесь, что он установлен и добавлен в PATH.
    pause
    exit /b 1
)

REM Обновление pip до последней версии
echo Обновление pip до последней версии...
python -m pip install --upgrade pip
if errorlevel 1 (
    echo Ошибка при обновлении pip.
    pause
    exit /b 1
)

REM Установка необходимых библиотек
echo Установка библиотек...
pip install customtkinter --no-warn-script-location
if errorlevel 1 (
    echo Ошибка при установке customtkinter.
    pause
    exit /b 1
)


REM Проверка успешности установки
if errorlevel 1 (
    echo Ошибка при установке одной из библиотек.
    pause
    exit /b 1
)

REM Запуск Python скрипта
echo Запуск скрипта build_start_ss14.py...
python ./build_start_ss14.py
if errorlevel 1 (
    echo Ошибка при выполнении скрипта build_start_ss14.py.
    pause
    exit /b 1
)

REM Завершение
echo Скрипт успешно выполнен.
pause
exit /b 0
