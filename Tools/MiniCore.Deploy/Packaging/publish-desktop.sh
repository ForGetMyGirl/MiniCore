#!/bin/sh
set -eu

AVALONIA_TELEMETRY_OPTOUT=1
export AVALONIA_TELEMETRY_OPTOUT
DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER=1
export DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER
MSBUILDDISABLENODEREUSE=1
export MSBUILDDISABLENODEREUSE

script_directory=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
solution_directory=$(dirname "$script_directory")
artifact_directory="$solution_directory/Artifacts/Desktop"
project_path="$solution_directory/MiniCore.Deploy.Desktop/MiniCore.Deploy.Desktop.csproj"
mac_runtime="osx-$(uname -m)"

if [ "$mac_runtime" = "osx-x86_64" ]; then
  mac_runtime="osx-x64"
fi

mkdir -p "$artifact_directory/win-x64"
dotnet restore "$project_path" --disable-parallel --disable-build-servers \
  --runtime win-x64 --property:NuGetAudit=false -m:1
dotnet publish "$project_path" --configuration Release --runtime win-x64 --self-contained true --no-restore --disable-build-servers \
  --property:PublishSingleFile=true --property:DebugType=None --output "$artifact_directory/win-x64" -m:1

app_directory="$artifact_directory/$mac_runtime/MiniCore Deploy.app"
mkdir -p "$app_directory/Contents/MacOS" "$app_directory/Contents/Resources"
dotnet restore "$project_path" --disable-parallel --disable-build-servers \
  --runtime "$mac_runtime" --property:NuGetAudit=false -m:1
dotnet publish "$project_path" --configuration Release --runtime "$mac_runtime" --self-contained true --no-restore --disable-build-servers \
  --property:PublishSingleFile=true --property:DebugType=None --output "$app_directory/Contents/MacOS" -m:1
cp "$script_directory/Info.plist" "$app_directory/Contents/Info.plist"

echo "Windows: $artifact_directory/win-x64/MiniCore.Deploy.Desktop.exe"
echo "macOS: $app_directory"
