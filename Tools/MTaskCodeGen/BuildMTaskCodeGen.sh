#!/usr/bin/env bash
set -euo pipefail

# Unity 2021.3 的 ILPostProcessor 仅引用编辑器自带程序集；不复制 Cecil 或 Burst 到仓库。
script_dir="$(cd "$(dirname "$0")" && pwd)"
project_dir="$(cd "$script_dir/../.." && pwd)"
unity_root="${UNITY_EDITOR_ROOT:-/Applications/Unity/Hub/Editor/2021.3.45f2c1/Unity.app/Contents}"
csc="$unity_root/MonoBleedingEdge/lib/mono/msbuild/Current/bin/Roslyn/csc.exe"
managed="$unity_root/Managed"
output="$project_dir/Assets/Plugins/MiniCore/MTask/Editor/MiniCore.MTask.CodeGen.dll"

if [[ ! -f "$csc" || ! -f "$managed/Unity.CompilationPipeline.Common.dll" || ! -f "$managed/Unity.Cecil.dll" ]]; then
    echo "无法找到 Unity 2021.3 编译依赖。请通过 UNITY_EDITOR_ROOT 指向 Unity.app/Contents。" >&2
    exit 1
fi

mkdir -p "$(dirname "$output")"
mono "$csc" \
    -nologo \
    -langversion:9.0 \
    -target:library \
    -optimize+ \
    -out:"$output" \
    -r:"$managed/Unity.CompilationPipeline.Common.dll" \
    -r:"$managed/Unity.Cecil.dll" \
    "$script_dir/MTaskOwnerILPostProcessor.cs"

echo "已生成 Unity 2021.3 MTask CodeGen：$output"
