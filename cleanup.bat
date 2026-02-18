@echo off
echo Cleaning project artifacts...
for /d /r . %%d in (bin,obj,publish) do @if exist "%%d" rd /s /q "%%d"
echo Done! Only source files remain.
pause