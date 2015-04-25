@ECHO OFF
IF NOT EXIST version.txt (
	ECHO version.txt missing!
	GOTO :showerror
)

REM Get the version and comment from version.txt lines 2 and 3
SET "release="
SET "comment="
FOR /F "skip=1 delims=" %%i IN (version.txt) DO IF NOT DEFINED release SET "release=%%i"
FOR /F "skip=2 delims=" %%i IN (version.txt) DO IF NOT DEFINED comment SET "comment=%%i"

REM If there's arguments on the command line overrule version.txt and use that as the version
IF [%1] NEQ [] (SET release=%1)
IF [%2] NEQ [] (SET comment=%2) ELSE (IF [%1] NEQ [] (SET "comment="))

SET version=%release%

IF [%comment%] EQU [] (SET version=%release%) ELSE (SET version=%release%-%comment%)
ECHO Building FormsProvider version %version%


%windir%\Microsoft.NET\Framework\v4.0.30319\msbuild.exe "Build.proj"
..\.nuget\NuGet.exe Pack NuSpecs\Umbraco.Courier.FormsProvider.nuspec -Version %version%
..\.nuget\NuGet.exe Pack NuSpecs\Umbraco.Courier.FormsTreeProvider.nuspec -Version %version%

IF ERRORLEVEL 1 GOTO :showerror

GOTO :EOF

:showerror
PAUSE
