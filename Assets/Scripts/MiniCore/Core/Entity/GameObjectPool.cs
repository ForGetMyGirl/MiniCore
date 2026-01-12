using Cysharp.Threading.Tasks;
using MiniCore.Model;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiniCore.Core
{

    public class GameObjectPool
    {
        private string typeName;
        private string groupName;
        public GameObjectPool(string typeName, string groupName)
        {
            this.typeName = typeName;
            this.groupName = groupName;
        }

        private List<IPoolObject> unusedObjList = new List<IPoolObject>(); //未被使用的池对象列表

        private List<IPoolObject> usingObjList = new List<IPoolObject>();  //正在被使用的池对象列表


        /// <summary>
        /// 获得一个可用的池对象，如果没有可用的对象，将返回null
        /// </summary>
        /// <returns></returns>
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
        /// 异步创建一个池对象
        /// </summary>
        /// <param name="path">对象加载的路径</param>
        /// <param name="parent"></param>
        /// <returns></returns>
        public async UniTask<IPoolObject> CreateObjectAsync(string path, Transform parent = null)
        {
            GameObject obj = await Global.Com.Get<AssetsComponent>().InstantiateAsync(path, parent);
            obj.name = $"{typeName}_{groupName}_{Guid.NewGuid()}";
            IPoolObject poolObj = obj.GetComponent<IPoolObject>();
            poolObj.GroupName = groupName;
            poolObj.IsUseful = false;
            poolObj.Init();
            usingObjList.Add(poolObj);
            return poolObj;
        }

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
                EventCenter.Broadcast(GameEvent.LogInfo, $"尝试移除不存在的池对象\n{e}");
            }

        }

    }

}