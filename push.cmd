@echo off
copy *.nupkg %HOMEPATH%\dropbox\nuget\ /y
nuget push .\Plugin.BluetoothLE\bin\Release\*.nupkg -Source https://www.nuget.org/api/v2/package
pause