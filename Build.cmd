@echo off


:: Set up directories.
set ROOT_DIRECTORY=%~dp0
set USER_DIRECTORY=%cd%


set TEMP_DIRECTORY=%ROOT_DIRECTORY%/.intermediate

if "%USER_DIRECTORY%\" == "%ROOT_DIRECTORY%" (
  set BUILD_DIRECTORY=%ROOT_DIRECTORY%/Build/%1/Live2D/Cubism
) else (
  set BUILD_DIRECTORY=%USER_DIRECTORY%/Live2D/Cubism
)


set COMPONENTS_DIRECTORY=%ROOT_DIRECTORY%/Live2DCubismComponents
set EXTENSIONS_DIRECTORY=%ROOT_DIRECTORY%/Live2DCubismComponents_EditorExtensions
set RESOURCES_DIRECTORY=%ROOT_DIRECTORY%/Live2DCubismComponents_Resources


set COMPONENTS_PROJECT=%ROOT_DIRECTORY%/Live2DCubismComponents/Live2DCubismComponents.csproj


:: Change build directory if run from root.
if "%BUILD_DIRECTORY%\" == "%ROOT_DIRECTORY%" (
  set BUILD_DIRECTORY=%ROOT_DIRECTORY%/Build/%1
)


:: Make sure output directories exists.
mkdir "%BUILD_DIRECTORY%"
mkdir "%BUILD_DIRECTORY%/Editor"
mkdir "%BUILD_DIRECTORY%/Framework"
mkdir "%BUILD_DIRECTORY%/Rendering"
mkdir "%BUILD_DIRECTORY%/Resources"


:: Build Core wrapper 'flavors' and move output to build directory.
MSBuild.exe %COMPONENTS_PROJECT% /t:Build /p:Configuration="%1";OutDir="%TEMP_DIRECTORY%";DefineConstants="UNITY_IOS";AssemblyName="CoreWrapper_iOS"
xcopy /q /s /v /y "%TEMP_DIRECTORY%" "%BUILD_DIRECTORY%"

MSBuild.exe %COMPONENTS_PROJECT% /t:Build /p:Configuration="%1";OutDir="%TEMP_DIRECTORY%";AssemblyName="CoreWrapper"
xcopy /q /s /v /y /i "%TEMP_DIRECTORY%" "%BUILD_DIRECTORY%"


:: Copy over framework, rendering, and UnityEditor extension source files.
xcopy /q /s /v /y /i "%COMPONENTS_DIRECTORY%/Live2D/Cubism/Framework" "%BUILD_DIRECTORY%/Framework"
xcopy /q /s /v /y /i "%COMPONENTS_DIRECTORY%/Live2D/Cubism/Rendering" "%BUILD_DIRECTORY%/Rendering"

xcopy /q /s /v /y /i "%EXTENSIONS_DIRECTORY%/Live2D/Cubism" "%BUILD_DIRECTORY%/Editor"


:: Copy over resources.
xcopy /q /s /v /y /i "%RESOURCES_DIRECTORY%" "%BUILD_DIRECTORY%/Resources"


:: Delete temporary folder.
rmdir /q /s "%TEMP_DIRECTORY%"
