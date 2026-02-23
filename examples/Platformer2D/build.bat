set CMAKE_PATH="C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\CommonExtensions\Microsoft\CMake\CMake\bin\cmake"
set SOURCE=%~dp0..\..\externals\MonoGame.Samples\Platformer2D\Desktop
dotnet build %SOURCE% -c Release
if errorlevel 1 exit /b
dotnet run --project ..\..\IL2CXX.Console --target Win32NT^
 --out out %SOURCE%\bin\Release\net10.0\Platformer2D.dll^
 --bundle^
  "System.Text.Json.Serialization.Converters.EnumConverter`1[[Platformer2D.Core.Effects.ParticleEffectType, Core]], System.Text.Json"^
  "System.Text.Json.Serialization.Converters.ObjectDefaultConverter`1[[Platformer2D.Core.Settings.Platformer2DLeaderboard, Core]], System.Text.Json"^
  "System.Text.Json.Serialization.Converters.ObjectDefaultConverter`1[[Platformer2D.Core.Settings.Platformer2DSettings, Core]], System.Text.Json"^
  "System.Text.Json.Serialization.Metadata.JsonPropertyInfo`1[[Platformer2D.Core.Effects.ParticleEffectType, Core]], System.Text.Json"^
 --reflection^
  "System.Text.Json.Serialization.Converters.EnumConverter`1[[Platformer2D.Core.Effects.ParticleEffectType, Core]], System.Text.Json"^
  "System.Text.Json.Serialization.Converters.ObjectDefaultConverter`1[[Platformer2D.Core.Settings.Platformer2DLeaderboard, Core]], System.Text.Json"^
  "System.Text.Json.Serialization.Converters.ObjectDefaultConverter`1[[Platformer2D.Core.Settings.Platformer2DSettings, Core]], System.Text.Json"^
  "System.Text.Json.Serialization.Metadata.JsonPropertyInfo`1[[System.Boolean, System.Private.CoreLib]], System.Text.Json"^
  "System.Text.Json.Serialization.Metadata.JsonPropertyInfo`1[[System.Int32, System.Private.CoreLib]], System.Text.Json"^
  "System.Text.Json.Serialization.Metadata.JsonPropertyInfo`1[[System.TimeSpan, System.Private.CoreLib]], System.Text.Json"^
  "System.Text.Json.Serialization.Metadata.JsonPropertyInfo`1[[Platformer2D.Core.Effects.ParticleEffectType, Core]], System.Text.Json"^
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
  "Platformer2D.Core.Effects.ParticleEffectType, Core"^
  "Platformer2D.Core.Settings.Platformer2DLeaderboard, Core"^
  "Platformer2D.Core.Settings.Platformer2DSettings, Core"^
  "Program, Platformer2D"
if errorlevel 1 exit /b
mkdir out\build
if errorlevel 1 exit /b
cd out\build
if errorlevel 1 exit /b
%CMAKE_PATH% ..
if errorlevel 1 exit /b
%CMAKE_PATH% --build . --config Release -j8
if errorlevel 1 exit /b
copy /y %SOURCE%\bin\Release\net10.0\runtimes\win-x64\native\*.dll Release
if errorlevel 1 exit /b
xcopy %SOURCE%\bin\Release\net10.0\Content Release\Content /s /e /i /y
