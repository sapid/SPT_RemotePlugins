# Stop the script on the first error
$ErrorActionPreference = 'Stop'

# Change to the directory where the script is located
Set-Location $PSScriptRoot

# Delete and recreate the build directory
Remove-Item -Recurse -Force -Path .\build -ErrorAction Ignore
New-Item -ItemType Directory -Path .\build\BepInEx\patchers -Force
New-Item -ItemType Directory -Path .\build\user\mods\RemotePlugins\node_modules -Force

# Change to the client directory and build the .NET project
Push-Location client
dotnet restore
dotnet msbuild /p:Configuration=Release /p:TargetFramework=net46 /t:Rebuild
Copy-Item -Path .\bin\Release\RemotePlugins.dll -Destination ..\build\BepInEx\patchers\
Pop-Location

# Change to the server directory and build the project
Push-Location server
npm run build

# Copy everything except node_modules to the build directory
Get-ChildItem -Path . -Recurse | Where-Object { $_.FullName -notmatch 'node_modules' } | Copy-Item -Destination { Join-Path ..\build\user\mods\RemotePlugins\ $_.FullName.Substring($pwd.Path.Length) } -Recurse -Force
Pop-Location

Compress-Archive -Path .\build\* -DestinationPath .\build.zip

Write-Output "Build complete"