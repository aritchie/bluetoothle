@echo off
copy *.nupkg %HOMEPATH%\dropbox\nuget\ /y
nuget push .\Plugin.BluetoothLE\bin\Release\*.nupkg -Source https://www.nuget.org/api/v2/package
del .\Plugin.BluetoothLE\bin\Release\*.nupkg

nuget push .\MvvmCross.Plugin.BluetoothLE\bin\Release\*.nupkg -Source https://www.nuget.org/api/v2/package
del .\MvvmCross.Plugin.BluetoothLE\bin\Release\*.nupkg
pause