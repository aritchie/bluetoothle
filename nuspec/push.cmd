@echo off
copy *.nupkg C:\users\allan.ritchie\dropbox\nuget\ /y
rem nuget push *.nupkg -Source https://www.nuget.org/api/v2/package
pause