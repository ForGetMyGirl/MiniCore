using System;
using UnityEngine;

namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// 可被服务器和客户端共同加载的 MiniBomber 权威地图定义。
    /// </summary>
    [CreateAssetMenu(fileName = "MiniBomberDefaultMap", menuName = "MiniCore/Demos/MiniBomber/Map Definition")]
    public sealed class BomberMapDefinition : ScriptableObject
    {
        #region Private 私有成员

        [SerializeField] private string displayName = "默认竞技场"; // 地图显示名。
        [SerializeField, Min(1)] private int version = 1; // 地图内容版本。
        [SerializeField, Min(5)] private int width = 17; // 地图横向格数。
        [SerializeField, Min(5)] private int height = 13; // 地图纵向格数。
        [SerializeField, Min(100)] private int cellSizeMillimeters = 1000; // 单格边长毫米数。
        [SerializeField] private byte[] cells = new byte[17 * 13]; // 按 Z 行、X 列保存的格子类型。
        [SerializeField] private Vector2Int[] spawnCells =
        {
            new Vector2Int(1, 1),
            new Vector2Int(15, 1),
            new Vector2Int(1, 11),
            new Vector2Int(15, 11)
        }; // 玩家出生格。

        #endregion

        #region Public 公共成员

        /// <summary>获取地图显示名称。</summary>
        public string DisplayName => displayName;
        /// <summary>获取地图内容版本。</summary>
        public int Version => version;
        /// <summary>获取地图横向格子数。</summary>
        public int Width => width;
        /// <summary>获取地图纵向格子数。</summary>
        public int Height => height;
        /// <summary>获取单格边长，单位为毫米。</summary>
        public int CellSizeMillimeters => cellSizeMillimeters;
        /// <summary>获取地图总格子数。</summary>
        public int CellCount => width * height;
        /// <summary>获取可用出生格数量。</summary>
        public int SpawnCount => spawnCells?.Length ?? 0;

        /// <summary>
        /// 获取指定地图格子的权威类型。
        /// </summary>
        /// <param name="x">横向格坐标。</param>
        /// <param name="z">纵向格坐标。</param>
        /// <returns>地图外返回固定墙，地图内返回序列化类型。</returns>
        public MiniBomberCellType GetCell(int x, int z)
        {
            if (x < 0 || z < 0 || x >= width || z >= height)
            {
                return MiniBomberCellType.Solid;
            }

            EnsureCellStorage();
            return (MiniBomberCellType)cells[(z * width) + x];
        }

        /// <summary>
        /// 设置指定地图格子的权威类型，供编辑器地图绘制工具调用。
        /// </summary>
        /// <param name="x">横向格坐标。</param>
        /// <param name="z">纵向格坐标。</param>
        /// <param name="value">新的格子类型。</param>
        public void SetCell(int x, int z, MiniBomberCellType value)
        {
            if (x < 0 || z < 0 || x >= width || z >= height)
            {
                throw new ArgumentOutOfRangeException(nameof(x), "地图格坐标超出范围。");
            }

            EnsureCellStorage();
            cells[(z * width) + x] = (byte)value;
        }

        /// <summary>
        /// 获取指定序号的出生格。
        /// </summary>
        /// <param name="index">出生格序号。</param>
        /// <returns>合法出生格。</returns>
        public Vector2Int GetSpawnCell(int index)
        {
            if (spawnCells == null || spawnCells.Length == 0)
            {
                return new Vector2Int(1, 1);
            }

            int normalized = index % spawnCells.Length;
            return spawnCells[normalized < 0 ? normalized + spawnCells.Length : normalized];
        }

        /// <summary>
        /// 复制地图格数据，供无 Unity 依赖的权威模拟持有。
        /// </summary>
        /// <returns>与当前地图尺寸完全一致的格子数组。</returns>
        public byte[] CopyCells()
        {
            EnsureCellStorage();
            var copy = new byte[cells.Length];
            Array.Copy(cells, copy, cells.Length);
            return copy;
        }

        /// <summary>
        /// 复制全部出生格，避免权威模拟持有可变的 Unity 序列化数组。
        /// </summary>
        /// <returns>出生格数组副本。</returns>
        public Vector2Int[] CopySpawnCells()
        {
            if (spawnCells == null)
            {
                return Array.Empty<Vector2Int>();
            }

            var copy = new Vector2Int[spawnCells.Length];
            Array.Copy(spawnCells, copy, spawnCells.Length);
            return copy;
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 保证序列化格子数组与当前宽高一致，并保留仍位于新范围内的数据。
        /// </summary>
        private void EnsureCellStorage()
        {
            int required = width * height;
            if (cells != null && cells.Length == required)
            {
                return;
            }

            var replacement = new byte[required];
            if (cells != null)
            {
                Array.Copy(cells, replacement, Math.Min(cells.Length, replacement.Length));
            }

            cells = replacement;
        }

        #endregion
    }
}
