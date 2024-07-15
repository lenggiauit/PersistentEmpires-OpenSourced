@echo off
set "source_folder=.\GUI"
set "destination_folder=D:\Steam games\steamapps\common\Mount & Blade II Bannerlord\Modules\DragonVStudio\GUI"

echo Copying GUI folder...

xcopy /s /i "%source_folder%" "%destination_folder%"

echo GUI folder copied successfully.
pause