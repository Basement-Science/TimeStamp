@echo off
color 0A
setlocal EnableDelayedExpansion
set "originalDir=%CD%"
set "workingDir=.\bin\#publish\net6.0-win-x64"
cd /D "%workingDir%"

set "exeName=Timestamp-win-x64.exe"

call %exeName% --version
echo.
echo Local modes:
call %exeName% -c 0 -l
call %exeName% -l
call %exeName% -c 2 -l
echo Piped global:
echo This text was piped! | %exeName% -c 0
echo This text was piped! | %exeName%
echo This text was piped! | %exeName% -c 2
echo.
call %exeName% -h
call %exeName% -x
echo.
echo pipe an empty line
echo. | %exeName% -c 2
echo This should | %exeName% -c 0 -o testFile.txt
echo go to | %exeName% -o testFile.txt
echo a File! | %exeName% -c 2 -o testFile.txt
echo printing testFile.txt
cat testFile.txt
echo press ctrl+C to stop it
ping -n 6 8.8.8.8 | %exeName% -l -c 2 -o testFile.txt | tee -a "test-ping-google.log" 
::del testFile.txt
ping -n 4 localhost | call %exeName% -c 2
cd %originalDir%
pause