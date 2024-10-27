set CMAKE_PATH="C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\CommonExtensions\Microsoft\CMake\CMake\bin\cmake"
set SOURCE=%~dp0..\..\externals\MonoGame.Samples\AutoPong\AutoPong.DesktopGL
dotnet build %SOURCE% -c Release
if errorlevel 1 exit /b
dotnet run --project ..\..\IL2CXX.Console --target Win32NT^
 --out out %SOURCE%\bin\Release\net8.0\AutoPong.DesktopGL.dll^
 --reflection "Program, AutoPong.DesktopGL"
if errorlevel 1 exit /b
mkdir out\build
if errorlevel 1 exit /b
cd out\build
if errorlevel 1 exit /b
%CMAKE_PATH% ..
if errorlevel 1 exit /b
%CMAKE_PATH% --build . --config Release -j8
if errorlevel 1 exit /b
copy /y %SOURCE%\bin\Release\net8.0\runtimes\win-x64\native\*.dll Release
