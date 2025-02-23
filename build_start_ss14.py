

#     ╔════════════════════════════════════╗
#     ║   Schrödinger's Cat Code   🐾      ║
#     ║   /\_/\\                           ║
#     ║  ( o.o )  Meow!                    ║
#     ║   > ^ <                            ║
#     ╚════════════════════════════════════╝


import tkinter as tk
from tkinter import filedialog, messagebox
from tkinter import ttk
import os
import ctypes
import subprocess

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

# Основное окно
root = tk.Tk()
root.title("Controller Build SS14")
root.geometry("395x410")
root.config(bg="#f4f4f9")

# Список для хранения созданных кнопок
created_buttons = []

# Функция для запуска команд в Windows Terminal
def run_command_in_terminal(command, working_directory):
    try:
        full_command = f"{command}"
        subprocess.Popen(["wt", "-d", working_directory, "powershell", "-NoExit", "-Command", full_command])
    except Exception as e:
        messagebox.showerror("Error", f"Failed to open Windows Terminal: {str(e)}")

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
            messagebox.showerror("Error", f"Failed to open Git Bash: {str(e)}")
    else:
        messagebox.showerror("Error", "Git Bash not found!")

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

def run_bat_file(file_name):
    bat_file_path = os.path.join(script_dir, file_name)
    if os.path.exists(bat_file_path):
        try:
            subprocess.Popen(["wt", "-d", os.path.join(script_dir), "cmd", "/c", bat_file_path + " && pause"])
            messagebox.showinfo("Success", f"Successfully executed {file_name}")
        except Exception as e:
            messagebox.showerror("Error", f"Error executing {file_name}: {e}")
    else:
        messagebox.showerror("Error", f"File not found: {file_name}")

def reset_configurations():
    """Сбрасываем все настройки к дефолтным"""
    messagebox.showinfo("Reset", "Configurations have been reset to default.")

# Создание рамок для красивого оформления
frame_left = tk.Frame(root, bg="#e1e1e1", padx=20, pady=10)
frame_left.grid(row=0, column=0, padx=10, pady=10, sticky="nsew")

# Метки для разделения
develop_adt_label = tk.Label(frame_left, text=folder_name, font=("Arial", 14, "bold"), bg="#e1e1e1")
develop_adt_label.grid(row=0, column=0, columnspan=2, pady=10)

# Кнопки
btn_git_bash = tk.Button(frame_left, text="Git Develop_ADT", command=git_develop_adt, width=20, bg="#808080", fg="white", font=("Arial", 10, "bold"))
btn_git_bash.grid(row=1, column=0, pady=0)

btn_build_develop_adt = tk.Button(frame_left, text="Build Develop_ADT", command=build_develop_adt, width=20, bg="#4CAF50", fg="white", font=("Arial", 10, "bold"))
btn_build_develop_adt.grid(row=2, column=0, pady=0)

btn_build_sorted_develop_adt = tk.Button(frame_left, text="Build(Sort) Develop_ADT", command=build_sorted_develop_adt, width=20, bg="#4CAF50", fg="white", font=("Arial", 10, "bold"))
btn_build_sorted_develop_adt.grid(row=3, column=0, pady=0)

btn_run_server = tk.Button(frame_left, text="Run Server", command=run_server, width=20, bg="#2196F3", fg="white", font=("Arial", 10, "bold"))
btn_run_server.grid(row=4, column=0, pady=0)

btn_run_client = tk.Button(frame_left, text="Run Client", command=run_client, width=20, bg="#2196F3", fg="white", font=("Arial", 10, "bold"))
btn_run_client.grid(row=5, column=0, pady=0)

btn_run_server_release = tk.Button(frame_left, text="Run Server (Release)", command=run_server_release, width=20, bg="#FF5722", fg="white", font=("Arial", 10, "bold"))
btn_run_server_release.grid(row=6, column=0, pady=0)

btn_run_client_release = tk.Button(frame_left, text="Run Client (Release)", command=run_client_release, width=20, bg="#FF5722", fg="white", font=("Arial", 10, "bold"))
btn_run_client_release.grid(row=7, column=0, pady=0)

btn_runserver_bat = tk.Button(frame_left, text="Run runserver.bat", command=lambda: run_bat_file("runserver.bat"), width=20, bg="#9C27B0", fg="white", font=("Arial", 10, "bold"))
btn_runserver_bat.grid(row=8, column=0, pady=0)

btn_runclient_bat = tk.Button(frame_left, text="Run runclient.bat", command=lambda: run_bat_file("runclient.bat"), width=20, bg="#9C27B0", fg="white", font=("Arial", 10, "bold"))
btn_runclient_bat.grid(row=9, column=0, pady=0)

# Кнопки 2 столбец
btn_test1 = tk.Button(frame_left, text="Test1", command=lambda: run_bat_file("runclient.bat"), width=20, bg="#9C27B0", fg="white", font=("Arial", 10, "bold"))
btn_test1.grid(row=1, column=1, pady=0)

btn_test2 = tk.Button(frame_left, text="Test1", command=lambda: run_bat_file("runclient.bat"), width=20, bg="#9C27B0", fg="white", font=("Arial", 10, "bold"))
btn_test2.grid(row=2, column=1, pady=0)

# Кнопки настроек
frame_settings = tk.Frame(root, bg="#f4f4f9", padx=10, pady=10)
frame_settings.grid(row=1, column=0, columnspan=2, pady=10)

btn_reset_config = tk.Button(frame_settings, text="Reset Configurations", command=reset_configurations, width=20, bg="#F44336", fg="white", font=("Arial", 10, "bold"))
btn_reset_config.grid(row=0, column=1, padx=0)

# Установка гибкости для колонки и строки
root.grid_rowconfigure(0, weight=1)
root.grid_columnconfigure(0, weight=1)

root.mainloop()
