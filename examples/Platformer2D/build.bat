set CMAKE_PATH="C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\CommonExtensions\Microsoft\CMake\CMake\bin\cmake"
set SOURCE=%~dp0..\..\externals\MonoGame.Samples\Platformer2D\Platformer2D.DesktopGL
dotnet build %SOURCE% -c Release
if errorlevel 1 exit /b
dotnet run --project ..\..\IL2CXX.Console --target Win32NT^
 --out out %SOURCE%\bin\Release\net8.0\Platformer2D.DesktopGL.dll^
 --reflection^
  "Microsoft.Xna.Framework.Content.CharReader, MonoGame.Framework"^
  "Microsoft.Xna.Framework.Content.Int32Reader, MonoGame.Framework"^
  "Microsoft.Xna.Framework.Content.ListReader`1[[System.Char, System.Private.CoreLib]], MonoGame.Framework"^
  "Microsoft.Xna.Framework.Content.ListReader`1[[Microsoft.Xna.Framework.Rectangle, MonoGame.Framework]], MonoGame.Framework"^
  "Microsoft.Xna.Framework.Content.ListReader`1[[Microsoft.Xna.Framework.Vector3, MonoGame.Framework]], MonoGame.Framework"^
  "Microsoft.Xna.Framework.Content.RectangleReader, MonoGame.Framework"^
  "Microsoft.Xna.Framework.Content.SongReader, MonoGame.Framework"^
  "Microsoft.Xna.Framework.Content.SoundEffectReader, MonoGame.Framework"^
  "Microsoft.Xna.Framework.Content.SpriteFontReader, MonoGame.Framework"^
  "Microsoft.Xna.Framework.Content.Texture2DReader, MonoGame.Framework"^
  "Microsoft.Xna.Framework.Content.Vector3Reader, MonoGame.Framework"^
  "Program, Platformer2D.DesktopGL"
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
if errorlevel 1 exit /b
xcopy %SOURCE%\bin\Release\net8.0\Content Release\Content /s /e /i /y
