@echo off
del *.nupkg
nuget pack Acr.Ble.nuspec
nuget pack Plugin.BluetoothLE.nuspec
pause