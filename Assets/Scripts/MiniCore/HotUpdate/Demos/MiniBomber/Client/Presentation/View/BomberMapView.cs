using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiniCore.Demo.MiniBomber.Unity
{
    /// <summary>
    /// 根据共享地图定义创建固定墙和木箱客户端表现。
    /// </summary>
    public sealed class BomberMapView : MonoBehaviour
    {
        #region UnityProperty Unity序列化字段

        [SerializeField] private BomberMapDefinition mapDefinition; // 客户端与服务器共用地图定义。
        [SerializeField] private Transform solidBlockRoot; // 固定墙根节点。
        [SerializeField] private Transform breakableBlockRoot; // 木箱根节点。
        [SerializeField] private GameObject solidBlockPrefab; // 固定墙 Prefab。
        [SerializeField] private GameObject breakableBlockPrefab; // 木箱 Prefab。

        #endregion

        #region Private 私有成员

        private readonly Dictionary<int, GameObject> breakableBlocks = new Dictionary<int, GameObject>(128); // 格索引到木箱表现。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 当前地图定义。
        /// </summary>
        public BomberMapDefinition Definition => mapDefinition;

        /// <summary>
        /// 清理旧表现并根据地图定义生成全部阻挡物。
        /// </summary>
        public void Build()
        {
            ValidateReferences();
            ClearChildren(solidBlockRoot);
            ClearChildren(breakableBlockRoot);
            breakableBlocks.Clear();
            for (int z = 0; z < mapDefinition.Height; z++)
            {
                for (int x = 0; x < mapDefinition.Width; x++)
                {
                    MiniBomberCellType type = mapDefinition.GetCell(x, z);
                    if (type == MiniBomberCellType.Solid)
                    {
                        CreateBlock(solidBlockPrefab, solidBlockRoot, x, z);
                    }
                    else if (type == MiniBomberCellType.Breakable)
                    {
                        GameObject block = CreateBlock(breakableBlockPrefab, breakableBlockRoot, x, z);
                        breakableBlocks.Add((z * mapDefinition.Width) + x, block);
                    }
                }
            }
        }

        /// <summary>
        /// 按服务器紧凑位图隐藏已摧毁木箱。
        /// </summary>
        /// <param name="destroyedCells">按格索引排列的 bitset。</param>
        public void ApplyDestroyedBreakables(byte[] destroyedCells)
        {
            if (destroyedCells == null)
            {
                return;
            }

            foreach (KeyValuePair<int, GameObject> pair in breakableBlocks)
            {
                int byteIndex = pair.Key >> 3;
                bool destroyed = byteIndex < destroyedCells.Length && (destroyedCells[byteIndex] & (1 << (pair.Key & 7))) != 0;
                if (destroyed && pair.Value != null)
                {
                    pair.Value.SetActive(false);
                }
            }
        }

        /// <summary>
        /// 在服务器可靠摧毁事件到达时立即隐藏指定木箱，不等待下一次状态快照。
        /// </summary>
        /// <param name="cellX">木箱横向格坐标。</param>
        /// <param name="cellZ">木箱纵向格坐标。</param>
        public void HideBreakable(int cellX, int cellZ)
        {
            if (mapDefinition == null || cellX < 0 || cellZ < 0 || cellX >= mapDefinition.Width || cellZ >= mapDefinition.Height)
            {
                return;
            }

            int cellIndex = (cellZ * mapDefinition.Width) + cellX;
            if (breakableBlocks.TryGetValue(cellIndex, out GameObject block) && block != null)
            {
                block.SetActive(false);
            }
        }

        #endregion

        #region Unity 生命周期函数

        /// <summary>
        /// 场景启用时构建地图表现。
        /// </summary>
        private void Start()
        {
            Build();
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 创建单个地图阻挡物。
        /// </summary>
        /// <param name="prefab">阻挡物 Prefab。</param>
        /// <param name="parent">所属根节点。</param>
        /// <param name="x">横向格坐标。</param>
        /// <param name="z">纵向格坐标。</param>
        /// <returns>新阻挡物实例。</returns>
        private static GameObject CreateBlock(GameObject prefab, Transform parent, int x, int z)
        {
            GameObject block = Instantiate(prefab, parent);
            block.transform.localPosition = new Vector3(x + 0.5f, 0.5f, z + 0.5f);
            return block;
        }

        /// <summary>
        /// 删除目标根节点下的旧地图表现。
        /// </summary>
        /// <param name="parent">目标根节点。</param>
        private static void ClearChildren(Transform parent)
        {
            for (int index = parent.childCount - 1; index >= 0; index--)
            {
                Destroy(parent.GetChild(index).gameObject);
            }
        }

        /// <summary>
        /// 验证地图生成所需的全部 Unity 引用。
        /// </summary>
        private void ValidateReferences()
        {
            if (mapDefinition == null || solidBlockRoot == null || breakableBlockRoot == null || solidBlockPrefab == null || breakableBlockPrefab == null)
            {
                throw new InvalidOperationException("BomberMapView 的地图、根节点或方块 Prefab 未配置完整。");
            }
        }

        #endregion
    }
}
