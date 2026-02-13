@rem Copy Files in the build folder of CryptoSoft to the build folder of EasySave
@rem Configuration is in arguments : Debug or Release
xcopy /h /i /c /k /e /r /y "..\CryptoSoft\bin\%1\net8.0-windows" "bin\%1\net8.0-windows\CryptoSoft"

@rem Copy Resources directory to the build folder of EasySave
@REM xcopy /h /i /c /k /e /r /y "Resources" "bin\%1\net8.0-windows\Resources"