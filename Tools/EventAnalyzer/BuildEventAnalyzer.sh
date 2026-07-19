#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "$0")" && pwd)"
project_dir="$(cd "$script_dir/../.." && pwd)"
unity_root="${UNITY_EDITOR_ROOT:-/Applications/Unity/Hub/Editor/2021.3.45f2c1/Unity.app/Contents}"
roslyn="$unity_root/MonoBleedingEdge/lib/mono/msbuild/Current/bin/Roslyn"
output="$project_dir/Assets/Plugins/MiniCore/Eventing/Editor/MiniCore.Eventing.Analyzers.dll"

if [[ ! -f "$roslyn/csc.exe" || ! -f "$roslyn/Microsoft.CodeAnalysis.dll" || ! -f "$roslyn/Microsoft.CodeAnalysis.CSharp.dll" ]]; then
    echo "无法找到 Unity Roslyn 编译依赖。请通过 UNITY_EDITOR_ROOT 指向 Unity.app/Contents。" >&2
    exit 1
fi

mkdir -p "$(dirname "$output")"
mono "$roslyn/csc.exe" \
    -nologo \
    -langversion:9.0 \
    -target:library \
    -optimize+ \
    -out:"$output" \
    -r:"$roslyn/Microsoft.CodeAnalysis.dll" \
    -r:"$roslyn/Microsoft.CodeAnalysis.CSharp.dll" \
    -r:"$roslyn/System.Collections.Immutable.dll" \
    -r:"$unity_root/UnityReferenceAssemblies/unity-4.8-api/Facades/netstandard.dll" \
    "$script_dir/EventSubscriptionAnalyzer.cs"

echo "已生成 MiniCore Eventing Analyzer：$output"
