using System;
using System.Collections.Generic;

namespace MiniCore.Demo.MiniBomber
{

    /// <summary>
    /// 权威模拟使用的只读地图数据副本。
    /// </summary>
    public sealed class MiniBomberBattleMap
    {
        #region Private 私有成员

        private readonly byte[] cells; // 地图格类型紧凑数组。
        private readonly MiniBomberCell[] spawnCells; // 玩家出生格数组。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 地图横向格数。
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// 地图纵向格数。
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// 单格边长毫米数。
        /// </summary>
        public int CellSizeMillimeters { get; }

        /// <summary>
        /// 出生格数量。
        /// </summary>
        public int SpawnCount => spawnCells.Length;

        /// <summary>
        /// 创建权威模拟持有的地图数据。
        /// </summary>
        /// <param name="width">地图横向格数。</param>
        /// <param name="height">地图纵向格数。</param>
        /// <param name="cellSizeMillimeters">单格边长毫米数。</param>
        /// <param name="cellData">长度必须等于宽乘高的格子数据。</param>
        /// <param name="spawns">至少包含一个出生格的数组。</param>
        public MiniBomberBattleMap(int width, int height, int cellSizeMillimeters, byte[] cellData, MiniBomberCell[] spawns)
        {
            if (width <= 0 || height <= 0 || cellSizeMillimeters <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width), "地图尺寸与格子边长必须大于零。");
            }

            if (cellData == null || cellData.Length != width * height)
            {
                throw new ArgumentException("地图格数据长度与宽高不一致。", nameof(cellData));
            }

            if (spawns == null || spawns.Length == 0)
            {
                throw new ArgumentException("地图至少需要一个出生格。", nameof(spawns));
            }

            Width = width;
            Height = height;
            CellSizeMillimeters = cellSizeMillimeters;
            cells = (byte[])cellData.Clone();
            spawnCells = (MiniBomberCell[])spawns.Clone();
        }

        /// <summary>
        /// 获取指定格子的初始地图类型。
        /// </summary>
        /// <param name="x">横向格坐标。</param>
        /// <param name="z">纵向格坐标。</param>
        /// <returns>地图外返回固定墙，地图内返回地图配置类型。</returns>
        public MiniBomberCellType GetCell(int x, int z)
        {
            return x < 0 || z < 0 || x >= Width || z >= Height
                ? MiniBomberCellType.Solid
                : (MiniBomberCellType)cells[(z * Width) + x];
        }

        /// <summary>
        /// 获取指定序号对应的循环出生格。
        /// </summary>
        /// <param name="index">出生格序号。</param>
        /// <returns>合法出生格。</returns>
        public MiniBomberCell GetSpawn(int index)
        {
            int normalized = index % spawnCells.Length;
            return spawnCells[normalized < 0 ? normalized + spawnCells.Length : normalized];
        }

        #endregion
    }
}
