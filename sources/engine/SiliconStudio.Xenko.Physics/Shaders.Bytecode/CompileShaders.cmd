@echo off
setlocal
set XenkoSdkDir=%~dp0..\..\..\..\
set XenkoSdkBinDir=%XenkoSdkDir%Bin\Windows-Direct3D11\
%XenkoSdkBinDir%SiliconStudio.Assets.CompilerApp.exe --platform=Windows --profile=Windows --output-path=%~dp0obj\app_data --build-path=%~dp0obj\build_app_data --package-file=Physics.pdxpkg
