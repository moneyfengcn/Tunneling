@echo off
 
:: 1. 获取纯 CMD 日期变量
set "yy=%date:~2,2%"
set "mm=%date:~5,2%"
if "%mm:~0,1%"=="0" set "mm=%mm:~1%"
set "dd=%date:~8,2%"
if "%dd:~0,1%"=="0" set "dd=%dd:~1%"

:: 2. 拼接成 26.5.21.1 格式
set "MyVersion=%yy%.%mm%.%dd%.1"

:: 3. 打印版本号消息
echo ========================================
echo 当前生成的发布版本号为: %MyVersion%
echo ========================================
echo.

echo 清除发布文件夹
if exist output rmdir /s /q output
mkdir output

dotnet clean
dotnet restore --no-cache

echo  开始构建Tunneling.Client

dotnet publish  /p:Version=%MyVersion% /p:FileVersion=%MyVersion% /p:AssemblyVersion=%MyVersion% /p:InformationalVersion=%MyVersion%  ".\Tunneling.Client\Tunneling.Client.csproj" -c Release -f net10.0 -r win-x64 --self-contained true /p:PublishSingleFile=true -o ./output/win-x64/Tunneling.Client
dotnet publish  /p:Version=%MyVersion% /p:FileVersion=%MyVersion% /p:AssemblyVersion=%MyVersion% /p:InformationalVersion=%MyVersion%  ".\Tunneling.Client\Tunneling.Client.csproj" -c Release -f net10.0 -r win-x86 --self-contained true /p:PublishSingleFile=true -o ./output/win-x86/Tunneling.Client
dotnet publish  /p:Version=%MyVersion% /p:FileVersion=%MyVersion% /p:AssemblyVersion=%MyVersion% /p:InformationalVersion=%MyVersion%  ".\Tunneling.Client\Tunneling.Client.csproj" -c Release -f net10.0 -r win-arm64 --self-contained true /p:PublishSingleFile=true -o ./output/win-arm64/Tunneling.Client
dotnet publish  /p:Version=%MyVersion% /p:FileVersion=%MyVersion% /p:AssemblyVersion=%MyVersion% /p:InformationalVersion=%MyVersion%  ".\Tunneling.Client\Tunneling.Client.csproj" -c Release -f net10.0 -r linux-x64 --self-contained true /p:PublishSingleFile=true -o ./output/linux-x64/Tunneling.Client
dotnet publish  /p:Version=%MyVersion% /p:FileVersion=%MyVersion% /p:AssemblyVersion=%MyVersion% /p:InformationalVersion=%MyVersion%  ".\Tunneling.Client\Tunneling.Client.csproj" -c Release -f net10.0 -r linux-arm64 --self-contained true /p:PublishSingleFile=true -o ./output/linux-arm64/Tunneling.Client
dotnet publish  /p:Version=%MyVersion% /p:FileVersion=%MyVersion% /p:AssemblyVersion=%MyVersion% /p:InformationalVersion=%MyVersion%  ".\Tunneling.Client\Tunneling.Client.csproj" -c Release -f net10.0 -r linux-arm --self-contained true /p:PublishSingleFile=true -o ./output/linux-arm/Tunneling.Client
dotnet publish  /p:Version=%MyVersion% /p:FileVersion=%MyVersion% /p:AssemblyVersion=%MyVersion% /p:InformationalVersion=%MyVersion%  ".\Tunneling.Client\Tunneling.Client.csproj" -c Release -f net10.0 -r osx-x64 --self-contained true /p:PublishSingleFile=true -o ./output/osx-x64/Tunneling.Client
dotnet publish  /p:Version=%MyVersion% /p:FileVersion=%MyVersion% /p:AssemblyVersion=%MyVersion% /p:InformationalVersion=%MyVersion%  ".\Tunneling.Client\Tunneling.Client.csproj" -c Release -f net10.0 -r osx-arm64 --self-contained true /p:PublishSingleFile=true -o ./output/osx-arm64/Tunneling.Client

echo  构建Tunneling.Client完毕

echo ================================================

echo  开始构建Tunneling.Server

dotnet publish /p:Version=%MyVersion% /p:FileVersion=%MyVersion% /p:AssemblyVersion=%MyVersion% /p:InformationalVersion=%MyVersion%  ".\Tunneling.Server\Tunneling.Server.csproj" -c Release -f net10.0 -r win-x64 --self-contained true /p:PublishSingleFile=true -o ./output/win-x64/Tunneling.Server
dotnet publish /p:Version=%MyVersion% /p:FileVersion=%MyVersion% /p:AssemblyVersion=%MyVersion% /p:InformationalVersion=%MyVersion%  ".\Tunneling.Server\Tunneling.Server.csproj" -c Release -f net10.0 -r win-x86 --self-contained true /p:PublishSingleFile=true -o ./output/win-x86/Tunneling.Server
dotnet publish /p:Version=%MyVersion% /p:FileVersion=%MyVersion% /p:AssemblyVersion=%MyVersion% /p:InformationalVersion=%MyVersion%  ".\Tunneling.Server\Tunneling.Server.csproj" -c Release -f net10.0 -r win-arm64 --self-contained true /p:PublishSingleFile=true -o ./output/win-arm64/Tunneling.Server
dotnet publish /p:Version=%MyVersion% /p:FileVersion=%MyVersion% /p:AssemblyVersion=%MyVersion% /p:InformationalVersion=%MyVersion%  ".\Tunneling.Server\Tunneling.Server.csproj" -c Release -f net10.0 -r linux-x64 --self-contained true /p:PublishSingleFile=true -o ./output/linux-x64/Tunneling.Server
dotnet publish /p:Version=%MyVersion% /p:FileVersion=%MyVersion% /p:AssemblyVersion=%MyVersion% /p:InformationalVersion=%MyVersion%  ".\Tunneling.Server\Tunneling.Server.csproj" -c Release -f net10.0 -r linux-arm64 --self-contained true /p:PublishSingleFile=true -o ./output/linux-arm64/Tunneling.Server
dotnet publish /p:Version=%MyVersion% /p:FileVersion=%MyVersion% /p:AssemblyVersion=%MyVersion% /p:InformationalVersion=%MyVersion%  ".\Tunneling.Server\Tunneling.Server.csproj" -c Release -f net10.0 -r linux-arm --self-contained true /p:PublishSingleFile=true -o ./output/linux-arm/Tunneling.Server
dotnet publish /p:Version=%MyVersion% /p:FileVersion=%MyVersion% /p:AssemblyVersion=%MyVersion% /p:InformationalVersion=%MyVersion%  ".\Tunneling.Server\Tunneling.Server.csproj" -c Release -f net10.0 -r osx-x64 --self-contained true /p:PublishSingleFile=true -o ./output/osx-x64/Tunneling.Server
dotnet publish /p:Version=%MyVersion% /p:FileVersion=%MyVersion% /p:AssemblyVersion=%MyVersion% /p:InformationalVersion=%MyVersion%  ".\Tunneling.Server\Tunneling.Server.csproj" -c Release -f net10.0 -r osx-arm64 --self-contained true /p:PublishSingleFile=true -o ./output/osx-arm64/Tunneling.Server

echo  构建Tunneling.Server完毕

copy /y .\readme.md .\output\win-x64\readme.md 
copy /y .\readme.md .\output\win-x86\readme.md 
copy /y .\readme.md .\output\win-arm64\readme.md 
copy /y .\readme.md .\output\linux-x64\readme.md 
copy /y .\readme.md .\output\linux-arm64\readme.md 
copy /y .\readme.md .\output\linux-arm\readme.md 
copy /y .\readme.md .\output\osx-x64\readme.md 
copy /y .\readme.md .\output\osx-arm64\readme.md 

cd output
del /s /q *.pdb
del /s /q  aspnetcorev2_inprocess.dll
del /s /q  dotnet-tools.json
del /s /q  appsettings.Development.json
del /s /q  web.config


echo  打压缩包
set WINRAR_PATH="C:\Program Files\WinRAR\WinRAR.exe"

echo 压缩打包中 1/8 ...
 %WINRAR_PATH% a -afzip -ep1 -m5 -r "win-x64.zip" ".\win-x64\"
echo 压缩打包中 2/8 ...
 %WINRAR_PATH% a -afzip -ep1 -m5 -r "win-x86.zip" ".\win-x86\"
echo 压缩打包中 3/8 ...
 %WINRAR_PATH% a -afzip -ep1 -m5 -r "win-arm64.zip" ".\win-arm64\"
echo 压缩打包中 4/8 ...
 %WINRAR_PATH% a -afzip -ep1 -m5 -r "linux-x64.zip" ".\linux-x64\"
echo 压缩打包中 5/8 ...
 %WINRAR_PATH% a -afzip -ep1 -m5 -r "linux-arm64.zip" ".\linux-arm64\"
echo 压缩打包中 6/8 ...
 %WINRAR_PATH% a -afzip -ep1 -m5 -r "linux-arm.zip" ".\linux-arm\"
echo 压缩打包中 7/8 ...
 %WINRAR_PATH% a -afzip -ep1 -m5 -r "osx-x64.zip" ".\osx-x64\"
echo 压缩打包中 8/8 ...
 %WINRAR_PATH% a -afzip -ep1 -m5 -r "osx-arm64.zip" ".\osx-arm64\"

echo 清除文件

rmdir /s /q win-x64
rmdir /s /q win-x86
rmdir /s /q win-arm64
rmdir /s /q linux-arm64
rmdir /s /q linux-arm
rmdir /s /q linux-x64
rmdir /s /q osx-x64
rmdir /s /q osx-arm64

echo 操作完毕

cd ..
pause