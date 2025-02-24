

#     ╔════════════════════════════════════╗
#     ║   Schrödinger's Cat Code   🐾      ║
#     ║   /\_/\\                           ║
#     ║  ( o.o )  Meow!                    ║
#     ║   > ^ <                            ║
#     ╚════════════════════════════════════╝


import customtkinter as ctk
import os
import ctypes
import subprocess
import webbrowser
import subprocess
import platform

# Скрытие консоли
ctypes.windll.user32.ShowWindow(ctypes.windll.kernel32.GetConsoleWindow(), 0)

# Определяем имя папки и текущий путь
script_dir = os.path.dirname(os.path.abspath(__file__))  # Путь к директории скрипта
folder_name = os.path.basename(script_dir)               # Имя папки

# Получаем путь к домашней директории
home_dir = os.path.expanduser("~")

# Для работы с Git Bash ищем путь установки Git через переменную среды
git_bash_path = None
if os.path.exists(os.path.join(home_dir, "AppData", "Local", "Programs", "Git", "bin", "bash.exe")):
    git_bash_path = os.path.join(home_dir, "AppData", "Local", "Programs", "Git", "bin", "bash.exe")
elif os.path.exists(os.path.join(os.environ.get("PROGRAMFILES", ""), "Git", "bin", "bash.exe")):
    git_bash_path = os.path.join(os.environ.get("PROGRAMFILES", ""), "Git", "bin", "bash.exe")
else:
    git_bash_path = None

# Основное окно с кастомизацией
ctk.set_appearance_mode("dark")
ctk.set_default_color_theme("green")
root = ctk.CTk()
root.title("Controller Build SS14")
root.geometry("330x425")

# Список для хранения созданных кнопок
created_buttons = []

# Функция для запуска команд в Windows Terminal
def run_command_in_terminal(command, working_directory):
    try:
        full_command = f"{command}"
        subprocess.Popen(["wt", "-d", working_directory, "powershell", "-NoExit", "-Command", full_command])
    except Exception as e:
        print(f"❌ Произошла ошибка: {str(e)}")

# Функция для запуска Git Bash
def git_develop_adt():
    working_directory = os.path.join(script_dir)
    if git_bash_path:
        try:
            subprocess.Popen(
                [git_bash_path, "--login", "-i"],
                cwd=working_directory,
                env=os.environ,
                creationflags=subprocess.CREATE_NEW_CONSOLE
            )
        except Exception as e:
            print(f"❌ Произошла ошибка: {str(e)}")
    else:
        print(f"Git Bash not found!")

def build_develop_adt():
    command = "dotnet build"
    working_directory = os.path.join(script_dir)
    run_command_in_terminal(command, working_directory)

def build_sorted_develop_adt():
    command = "dotnet build | findstr /i \"error\""
    working_directory = os.path.join(script_dir)
    run_command_in_terminal(command, working_directory)

def run_server():
    command = "dotnet run --no-build --project Content.Server"
    working_directory = os.path.join(script_dir)
    run_command_in_terminal(command, working_directory)

def run_client():
    command = "dotnet run --no-build --project Content.Client"
    working_directory = os.path.join(script_dir)
    run_command_in_terminal(command, working_directory)

def run_server_release():
    command = "dotnet run --property:Configuration=Release --project Content.Server"
    working_directory = os.path.join(script_dir)
    run_command_in_terminal(command, working_directory)

def run_client_release():
    command = "dotnet run --property:Configuration=Release --project Content.Client"
    working_directory = os.path.join(script_dir)
    run_command_in_terminal(command, working_directory)

def open_github_repo():
    try:
        # Выполняем команду git remote get-url origin
        result = subprocess.run(
            ["git", "remote", "get-url", "origin"],
            cwd=script_dir,
            capture_output=True,
            text=True
        )
        if result.returncode != 0:
            print("❌ Ошибка: Не удалось получить URL репозитория.")
            print(result.stderr)
            return
        # Получаем URL репозитория
        repo_url = result.stdout.strip()
        # Преобразуем SSH-URL в HTTPS-URL (если используется SSH)
        if repo_url.startswith("git@"):
            repo_url = repo_url.replace(":", "/").replace("git@", "https://")
        elif repo_url.startswith("https://"):
            pass  # Уже HTTPS
        else:
            print("❌ Ошибка: Неизвестный формат URL репозитория.")
            return

        # Открываем URL в браузере
        print(f"🌐 Открываю репозиторий: {repo_url}")
        webbrowser.open(repo_url)

    except Exception as e:
        print(f"❌ Произошла ошибка: {e}")

def open_explorer():
    try:
        # Определяем команду для открытия проводника в зависимости от ОС
        if platform.system() == "Windows":
            os.startfile(script_dir)  # Для Windows
        elif platform.system() == "Darwin":
            os.system(f"open '{script_dir}'")  # Для macOS
        else:
            os.system(f"xdg-open '{script_dir}'")  # Для Linux

        print(f"📂 Проводник открыт в папке: {script_dir}")
    except Exception as e:
        print(f"❌ Произошла ошибка: {e}")

def python_run_this():
    command = "python RUN_THIS.py"
    working_directory = os.path.join(script_dir)
    run_command_in_terminal(command, working_directory)

def run_bat_file(file_name):
    bat_file_path = os.path.join(script_dir, file_name)
    if os.path.exists(bat_file_path):
        try:
            subprocess.Popen(["wt", "-d", os.path.join(script_dir), "cmd", "/c", bat_file_path + " && pause"])
        except Exception as e:
            ctk.CTkMessagebox(title="Error", message=f"Error executing {file_name}: {e}")
    else:
        ctk.CTkMessagebox(title="Error", message=f"File not found: {file_name}")

def reset_configurations():
    """Сбрасываем все настройки к дефолтным"""
    # Создаем новое всплывающее окно
    dialog = ctk.CTkToplevel(root)
    dialog.title("Reset")
    # Получаем размеры экрана
    screen_width = dialog.winfo_screenwidth()
    screen_height = dialog.winfo_screenheight()
    # Устанавливаем размеры окна в зависимости от текста
    dialog.geometry("300x100")  # Можно настроить размер окна, если нужно
    # Центрируем окно
    window_width = 300
    window_height = 100
    position_top = int(screen_height / 2 - window_height / 2)
    position_right = int(screen_width / 2 - window_width / 2)
    dialog.geometry(f"{window_width}x{window_height}+{position_right}+{position_top}")
    # Добавляем сообщение
    message = ctk.CTkLabel(dialog, text="Configurations have been reset to default.", font=("Arial", 12))
    message.pack(padx=20, pady=20, fill="both", expand=True)  # Расширяем на всю ширину
    # Добавляем кнопку для закрытия окна
    ok_button = ctk.CTkButton(dialog, text="OK", command=dialog.destroy)
    ok_button.pack(pady=10)

# Настраиваемая ширина кнопок
button_width = 200

# Основной фрейм для кнопок
frame_left = ctk.CTkFrame(root)
frame_left.grid(row=0, column=0, padx=5, pady=5, sticky="nsew")

develop_adt_label = ctk.CTkLabel(frame_left, text=folder_name, font=("Arial", 14, "bold"))
develop_adt_label.grid(row=0, column=0, columnspan=2, pady=5)

# Кнопки 1 столбец
btn_git_bash = ctk.CTkButton(frame_left, text="GitHub console", command=git_develop_adt, width=button_width)
btn_git_bash.grid(row=1, column=0, pady=5, padx=(5, 5), sticky="ew")

btn_build_develop_adt = ctk.CTkButton(frame_left, text="Dotnet Build", command=build_develop_adt, width=button_width)
btn_build_develop_adt.grid(row=2, column=0, pady=5, padx=(5, 5), sticky="ew")

btn_build_sorted_develop_adt = ctk.CTkButton(frame_left, text="Dotnet Build(Sort)", command=build_sorted_develop_adt, width=button_width)
btn_build_sorted_develop_adt.grid(row=3, column=0, pady=5, padx=(5, 5), sticky="ew")

btn_run_server = ctk.CTkButton(frame_left, text="Run Server (Debug)", command=run_server, width=button_width)
btn_run_server.grid(row=4, column=0, pady=5, padx=(5, 5), sticky="ew")

btn_run_client = ctk.CTkButton(frame_left, text="Run Client (Debug)", command=run_client, width=button_width)
btn_run_client.grid(row=5, column=0, pady=5, padx=(5, 5), sticky="ew")

btn_run_server_release = ctk.CTkButton(frame_left, text="Run Server (Release)", command=run_server_release, width=button_width)
btn_run_server_release.grid(row=6, column=0, pady=5, padx=(5, 5), sticky="ew")

btn_run_client_release = ctk.CTkButton(frame_left, text="Run Client (Release)", command=run_client_release, width=button_width)
btn_run_client_release.grid(row=7, column=0, pady=5, padx=(5, 5), sticky="ew")

btn_runserver_bat = ctk.CTkButton(frame_left, text="Run runserver.bat", command=lambda: run_bat_file("runserver.bat"), width=button_width)
btn_runserver_bat.grid(row=8, column=0, pady=5, padx=(5, 5), sticky="ew")

btn_runclient_bat = ctk.CTkButton(frame_left, text="Run runclient.bat", command=lambda: run_bat_file("runclient.bat"), width=button_width)
btn_runclient_bat.grid(row=9, column=0, pady=5, padx=(5, 5), sticky="ew")

# Кнопки 2 столбец
btn_repo_browser = ctk.CTkButton(frame_left, text="Repo in browser", command=open_github_repo, width=button_width)
btn_repo_browser.grid(row=1, column=1, pady=5, padx=(5, 5), sticky="ew")

btn_file_explorer = ctk.CTkButton(frame_left, text="File Explorer", command=open_explorer, width=button_width)
btn_file_explorer.grid(row=2, column=1, pady=5, padx=(5, 5), sticky="ew")

btn_run_this = ctk.CTkButton(frame_left, text="Update submodule", command=python_run_this, width=button_width)
btn_run_this.grid(row=3, column=1, pady=5, padx=(5, 5), sticky="ew")


# Кнопки настроек
frame_settings = ctk.CTkFrame(root)
frame_settings.grid(row=1, column=0, columnspan=2, pady=5, sticky="ew")

btn_reset_config = ctk.CTkButton(frame_settings, text="Reset Configurations", command=reset_configurations, width=button_width)
btn_reset_config.grid(row=0, column=0, padx=5, pady=5)

# Установка гибкости для колонок и строк
root.grid_rowconfigure(1, weight=0)
root.grid_columnconfigure(0, weight=1)

frame_left.grid_rowconfigure(0, weight=0)
frame_left.grid_rowconfigure(1, weight=1)
frame_left.grid_columnconfigure(0, weight=1)
frame_left.grid_columnconfigure(1, weight=1)

frame_settings.grid_columnconfigure(0, weight=1)


root.mainloop()
