@echo off

@rem If the script is not run from the root of the project or from the Scripts folder
if not exist "*.sln" (
    cd ..
    if not exist "*.sln" (
        echo "Please run this script from the root of the project or from the Scripts folder."
        exit /b 1
    )
)

@rem Compile the project
@rem Windows 64-bit
dotnet publish EasySave/EasySave.csproj -c Release -r win-x64 --self-contained true -o ./publish/easysave-win-x64
dotnet publish CryptoSoft/CryptoSoft.csproj -c Release -r win-x64 --self-contained true -o ./publish/easysave-win-x64/CryptoSoft
dotnet publish CryptoSoft/CryptoSoft.csproj -c Release -r win-x64 --self-contained true -o ./publish/cryptosoft-win-x64
dotnet publish EasyRemote/EasyRemote.csproj -c Release -r win-x64 --self-contained true -o ./publish/easyremote-win-x64

@rem Create the zip files
@rem Windows 64-bit
powershell -Command "Compress-Archive -Path ./publish/easysave-win-x64/* -DestinationPath ./publish/easysave-win-x64.zip"
powershell -Command "Compress-Archive -Path ./publish/cryptosoft-win-x64/* -DestinationPath ./publish/cryptosoft-win-x64.zip"
powershell -Command "Compress-Archive -Path ./publish/easyremote-win-x64/* -DestinationPath ./publish/easyremote-win-x64.zip"