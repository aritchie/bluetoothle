@echo off
del *.nupkg
nuget pack Acr.Ble.nuspec
rem nuget pack Plugin.BluetoothLE.nuspec
pause