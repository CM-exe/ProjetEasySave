@echo off
@REM This script is used to set up a test environment for EasySave
@REM It creates a temporary directory structure for testing purposes
@REM and creates configuration file.
@REM Usage: test.bat [optional: number of jobs]

setlocal enabledelayedexpansion

@REM Check if the script is run in the root directory of the project
@REM by checking if the current directory contains "README.md"
if not exist "README.md" (
    cd ..
    if not exist "README.md" (
        echo This script must be run from the root directory of the EasySave project.
        exit /b 1
    )
)

@REM Retrieve the number of jobs from the command line argument or default to 1
set "numJobs=%1"
if "%numJobs%"=="" set "numJobs=1"

@REM Create the temporary directory structure
set "baseDir=%temp%\EasySave"
if not exist "%baseDir%" (
	mkdir "%baseDir%"
)

set LF=^


for /L %%i in (1,1,%numJobs%) do (
    set "jobDir=!baseDir!\Job%%i"
	if not exist "!jobDir!\Source" (
    	mkdir "!jobDir!\Source"
	)
	if not exist "!jobDir!\Destination" (
		mkdir "!jobDir!\Destination"
	)

    @REM Fill the Source directory with dummy files
    for /L %%j in (1,1,100) do (
		echo Lorem ipsum dolor sit amet, consectetur adipiscing elit. Vivamus pretium, justo ut tincidunt ultricies, est lectus pulvinar quam, sed aliquet diam ligula in leo. Nunc non augue felis. Sed nec odio ornare, egestas massa quis, vulputate quam. Aliquam dictum leo vitae sodales rutrum. Praesent vel dolor in purus tempor rhoncus a et metus. Nunc maximus mauris et felis egestas, id aliquet justo cursus. Cras efficitur fringilla purus consequat efficitur. Quisque ligula elit, dictum eu dolor vitae, laoreet vulputate libero. Vivamus pulvinar elit et pharetra faucibus. Ut non metus non sem interdum dictum a quis lorem.!LF!^
Phasellus ornare nibh vitae mi elementum aliquam. Sed imperdiet dolor magna, id condimentum magna sagittis vel. Duis faucibus fringilla consequat. Cras varius eu mauris non dictum. Cras dapibus tincidunt imperdiet. Phasellus congue metus elit. Nulla ornare consequat ante quis porttitor. Pellentesque sit amet ultrices turpis, eu tincidunt justo. Donec placerat ipsum placerat felis varius, at mollis nisl sollicitudin. Orci varius natoque penatibus et magnis dis parturient montes, nascetur ridiculus mus. Vivamus mattis libero ac scelerisque tristique. Nullam euismod lectus quis nisi egestas, rutrum semper ex malesuada. Vestibulum eleifend tristique risus ullamcorper molestie. Sed non placerat erat, nec convallis ex.!LF!^
Proin dictum odio id tincidunt commodo. Sed gravida dapibus ante, sed eleifend massa posuere at. Etiam vitae iaculis justo. Donec at ligula a nibh dapibus tristique. Vivamus rutrum massa ut velit semper convallis. Phasellus laoreet odio mauris, ac blandit velit pellentesque a. Sed placerat tempor ultrices. Mauris ex nunc, pharetra at mauris nec, pellentesque sagittis diam. Etiam vitae elit eget elit consequat scelerisque lacinia ut risus. Fusce condimentum mauris augue.!LF!^
Phasellus eget est fringilla, vulputate nulla vitae, volutpat mauris. Donec eros nunc, bibendum quis massa nec, vestibulum semper ipsum. Nam neque tortor, dignissim non augue eget, pulvinar tempus arcu. Interdum et malesuada fames ac ante ipsum primis in faucibus. Vestibulum mattis maximus elit nec consectetur. Aenean luctus massa a tortor molestie, in tincidunt urna suscipit. Aliquam iaculis mauris id mauris ultricies fringilla. Aliquam suscipit dui enim, sit amet vulputate metus lacinia vitae. Suspendisse non euismod lacus. Cras eu nisi porta, aliquet ante sed, finibus purus. Nulla aliquet ex id erat blandit, sit amet congue quam pharetra. Quisque vitae consequat turpis.!LF!^
Phasellus rhoncus nisi lorem. Aenean pulvinar fermentum leo commodo euismod. Vivamus id euismod tortor. Vestibulum ante ipsum primis in faucibus orci luctus et ultrices posuere cubilia curae; Nam rhoncus lorem id scelerisque commodo. Vivamus non velit quis augue vehicula gravida. Nunc aliquet vel ex et finibus. Class aptent taciti sociosqu ad litora torquent per conubia nostra, per inceptos himenaeos. Phasellus rutrum dui in vulputate lobortis. Suspendisse potenti. Mauris dignissim orci in mi vestibulum convallis. Duis vehicula, elit tincidunt scelerisque imperdiet, nisl mi consequat risus, quis feugiat mauris lectus et leo. > "!jobDir!\Source\File%%j.txt"
		
    )

    echo "" > "!jobDir!\Source\Test.docx"
)

@REM Create a configuration file for the jobs
set "configFile=!cd!\EasySave\bin\Debug\net8.0-windows\configuration.json"

set configContent=
for /L %%i in (1,1,!numJobs!) do (
    set "jobName=Job%%i"
    set "sourcePath=!baseDir!\Job%%i\Source"
    set "destinationPath=!baseDir!\Job%%i\Destination"

    set configContent=!configContent!    {!LF!^
      "Name": "!jobName!",!LF!^
      "Source": "!sourcePath:\=\\!",!LF!^
      "Destination": "!destinationPath:\=\\!",!LF!^
      "Type": "Complete"!LF!^
    }

	if %%i lss !numJobs! (
		set configContent=!configContent!,!LF!
	)
)

echo {!LF!^
  "Language": "FR",!LF!^
  "StateFile": "state.json",!LF!^
  "LogFile": "logs.json",!LF!^
  "CryptoFile": "CryptoSoft/CryptoSoft.exe",!LF!^
  "CryptoKey": "7A2F8D15E9C3B6410D5F78A92E64B0C3DB91A527F836E45C0B2D7498C1E5A3F6",!LF!^
  "Processes": [!LF!^
    "CalculatorApp"!LF!^
  ],!LF!^
  "CryptoExtentions": [!LF!^
    "txt"!LF!^
  ],!LF!^
  "Jobs": [!LF!^
!configContent!!LF!^
  ]!LF!^
} > "%configFile%"
