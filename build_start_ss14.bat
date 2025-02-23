@echo off
REM Проверка наличия Python
where python >nul 2>nul
if errorlevel 1 (
    echo Python не найден. Убедитесь, что он установлен и добавлен в PATH.
    pause
    exit /b 1
)

@REM REM Проверка версии Python
@REM python -c "import sys; exit(0) if sys.version_info >= (3, 6) else exit(1)"
@REM if errorlevel 1 (
@REM     echo Требуется Python версии 3.6 или выше.
@REM     pause
@REM     exit /b 1
@REM )

REM Установка tkinter
python -c "import tkinter" >nul 2>nul
if errorlevel 1 (
    echo Установка tkinter...
    pip install tk --no-warn-script-location
    if errorlevel 1 (
        echo Ошибка при установке tkinter.
        pause
        exit /b 1
    )
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
