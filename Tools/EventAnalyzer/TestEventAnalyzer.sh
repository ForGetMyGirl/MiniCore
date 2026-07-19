#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "$0")" && pwd)"
project_dir="$(cd "$script_dir/../.." && pwd)"
unity_root="${UNITY_EDITOR_ROOT:-/Applications/Unity/Hub/Editor/2021.3.45f2c1/Unity.app/Contents}"
roslyn="$unity_root/MonoBleedingEdge/lib/mono/msbuild/Current/bin/Roslyn"
analyzer="$project_dir/Assets/Plugins/MiniCore/Eventing/Editor/MiniCore.Eventing.Analyzers.dll"
output="/private/tmp/MiniCore.Eventing.AnalyzerFixture.dll"

diagnostics="$(mono "$roslyn/csc.exe" \
    -nologo \
    -langversion:9.0 \
    -target:library \
    -out:"$output" \
    -r:"$unity_root/UnityReferenceAssemblies/unity-4.8-api/Facades/netstandard.dll" \
    -analyzer:"$analyzer" \
    "$script_dir/Tests/AnalyzerFixture.cs" 2>&1 || true)"

printf '%s\n' "$diagnostics"
for diagnostic_id in MCEVT001 MCEVT002 MCEVT003; do
    if ! grep -q "$diagnostic_id" <<< "$diagnostics"; then
        echo "未收到预期分析器诊断：$diagnostic_id" >&2
        exit 1
    fi
done

if grep -q "error CS" <<< "$diagnostics"; then
    echo "分析器测试夹具存在 C# 编译错误。" >&2
    exit 1
fi

echo "MiniCore Eventing Analyzer 诊断测试通过。"
