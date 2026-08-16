using System.Text.Json;

namespace MiniCore.Server.Rpc;

/// <summary>
/// 从 Unity 项目唯一 Opcode Manifest 加载稳定类型映射。
/// </summary>
public sealed class OpcodeManifest
{
    #region Private 私有成员

    private readonly Dictionary<string, uint> byTypeName;

    private OpcodeManifest(Dictionary<string, uint> byTypeName)
    {
        this.byTypeName = byTypeName;
    }

    #endregion

    #region Public 公共成员

    /// <summary>
    /// 从部署输出目录加载构建时复制的 OpcodeManifest.json。
    /// </summary>
    /// <param name="path">Manifest 文件完整路径。</param>
    /// <returns>不可变查询对象。</returns>
    public static OpcodeManifest Load(string path)
    {
        using FileStream stream = File.OpenRead(path);
        ManifestDocument document = JsonSerializer.Deserialize<ManifestDocument>(stream, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidDataException("Opcode Manifest 不是有效 JSON。");
        var mappings = new Dictionary<string, uint>(StringComparer.Ordinal);
        foreach (ManifestEntry entry in document.Entries)
        {
            mappings.Add(entry.TypeName, entry.Opcode);
        }

        return new OpcodeManifest(mappings);
    }

    /// <summary>
    /// 获取指定 Protobuf 类型的稳定 Opcode。
    /// </summary>
    /// <typeparam name="T">目标 Protobuf 消息类型。</typeparam>
    /// <returns>Manifest 中登记的稳定 Opcode。</returns>
    public uint Get<T>()
    {
        string name = typeof(T).FullName ?? throw new InvalidOperationException("协议类型没有完整名称。");
        return byTypeName.TryGetValue(name, out uint opcode)
            ? opcode
            : throw new KeyNotFoundException($"Opcode Manifest 未登记协议类型：{name}。");
    }

    #endregion

    #region Private 私有成员

    private sealed class ManifestDocument
    {
        public List<ManifestEntry> Entries { get; set; } = [];
    }

    private sealed class ManifestEntry
    {
        public string TypeName { get; set; } = string.Empty;
        public uint Opcode { get; set; }
    }

    #endregion
}
