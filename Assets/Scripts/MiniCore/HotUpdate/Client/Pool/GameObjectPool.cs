using Cysharp.Threading.Tasks;
using MiniCore.Model;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiniCore.Core
{

    /// <summary>
    /// 管理同一类型对象的可用与使用中实例。
    /// 异步创建期间会以对象池自身为 owner 短暂持有 AssetsComponent，避免跨 await 使用失效组件。
    /// </summary>
    public class GameObjectPool : IDisposable
    {
        #region Private 私有成员

        private readonly string typeName; // 池中对象的类型名称。
        private readonly string groupName; // 池所属的分组名称。
        private readonly List<IPoolObject> unusedObjList = new List<IPoolObject>(); // 未被使用的池对象列表。
        private readonly List<IPoolObject> usingObjList = new List<IPoolObject>(); // 正在被使用的池对象列表。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 使用对象类型和分组名称创建对象池。
        /// </summary>
        /// <param name="typeName">池中对象的类型名称。</param>
        /// <param name="groupName">池所属的分组名称。</param>
        public GameObjectPool(string typeName, string groupName)
        {
            this.typeName = typeName;
            this.groupName = groupName;
        }

        /// <summary>
        /// 获得一个可用的池对象。
        /// 没有缓存对象时返回 null。
        /// </summary>
        /// <returns>已初始化的可用对象；没有缓存对象时返回 null。</returns>
        public IPoolObject GetUsefulObj()
        {
            if (unusedObjList.Count == 0) return null;

            IPoolObject obj = unusedObjList[0];
            if (obj != null)
            {
                obj.IsUseful = false;       //设置为不可用
                obj.Init();                 //对象进行初始化
                usingObjList.Add(obj);      //放到正在使用列表中
                unusedObjList.Remove(obj);  //从未使用列表中移除
                return obj;
            }
            return null;
        }

        /// <summary>
        /// 异步创建一个池对象。
        /// </summary>
        /// <param name="path">对象加载的路径</param>
        /// <param name="parent">新对象的父节点。</param>
        /// <returns>已初始化并进入使用中列表的池对象。</returns>
        public async UniTask<IPoolObject> CreateObjectAsync(string path, Transform parent = null)
        {
            AssetsComponent assetsComponent = Global.Get<AssetsComponent>(this);
            try
            {
                GameObject obj = await assetsComponent.InstantiateAsync(path, parent);
                obj.name = $"{typeName}_{groupName}_{Guid.NewGuid()}";
                IPoolObject poolObj = obj.GetComponent<IPoolObject>();
                poolObj.GroupName = groupName;
                poolObj.IsUseful = false;
                poolObj.Init();
                usingObjList.Add(poolObj);
                return poolObj;
            }
            finally
            {
                Global.Remove<AssetsComponent>(this);
            }
        }

        /// <summary>
        /// 释放对象池仍持有的全部全局组件引用。
        /// 当前异步创建流程会在 finally 中逐项释放，此方法用于异常路径的生命周期兜底。
        /// </summary>
        public void Dispose()
        {
            Global.ReleaseAll(this);
        }

        /// <summary>
        /// 回收一个正在使用的池对象。
        /// </summary>
        /// <param name="obj">要回收的池对象。</param>
        public void CollectObject(IPoolObject obj)
        {
            try
            {
                obj.IsUseful = true;
                obj.Clear();
                unusedObjList.Add(obj);
                usingObjList.Remove(obj);
            }
            catch (Exception e)
            {
                LogSwitch.Info($"尝试移除不存在的池对象\n{e}");
            }

        }

        #endregion
    }

}
