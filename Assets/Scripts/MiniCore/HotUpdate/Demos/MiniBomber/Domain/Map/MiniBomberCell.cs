using System;
using System.Collections.Generic;

namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// 不依赖 Unity 类型的 MiniBomber 地图格坐标。
    /// </summary>
    public readonly struct MiniBomberCell
    {
        #region Public 公共成员

        /// <summary>
        /// 横向格坐标。
        /// </summary>
        public int X { get; }

        /// <summary>
        /// 纵向格坐标。
        /// </summary>
        public int Z { get; }

        /// <summary>
        /// 创建地图格坐标。
        /// </summary>
        /// <param name="x">横向坐标。</param>
        /// <param name="z">纵向坐标。</param>
        public MiniBomberCell(int x, int z)
        {
            X = x;
            Z = z;
        }

        #endregion
    }
}
