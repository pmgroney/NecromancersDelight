@echo off
call "%~dp0\.venv\Scripts\activate.bat"
python "%~dp0\scripts\ask-kimi-with-files.py" %*
