@echo off
del *.nupkg
rem nuget pack Acr.Ble.nuspec
nuget pack Plugin.BluetoothLE.nuspec
pause