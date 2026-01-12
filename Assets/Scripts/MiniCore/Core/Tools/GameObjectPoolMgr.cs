using Cysharp.Threading.Tasks;
using MiniCore.Model;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiniCore.Core
{

    public class GameObjectPoolMgr : MonoSingleton<GameObjectPoolMgr>
    {
        private const string DefaultGroupName = "DefaultGroup";
        private Dictionary<Type, Dictionary<string, GameObjectPool>> typeAndGroupPoolDic;
        private Dictionary<IPoolObject, string> poolObjAndGroupDic;     //对象及其所在组的字典

        protected override void Init()
        {
            base.Init();
            DontDestroyOnLoad(gameObject);
            //gameObjectPoolDic = new Dictionary<Type, GameObjectPool>();
            typeAndGroupPoolDic = new Dictionary<Type, Dictionary<string, GameObjectPool>>();
            poolObjAndGroupDic = new Dictionary<IPoolObject, string>();
        }

        public async UniTask<T> GeneratePoolObject<T>(string path, string group = DefaultGroupName, Transform parent = null) where T : MonoBehaviour, IPoolObject, new()
        {
            Type type = typeof(T);
            T poolObj;
            //检查是否有对应类型的对象池字典
            if (!typeAndGroupPoolDic.ContainsKey(type))
            {
                typeAndGroupPoolDic.Add(type, new Dictionary<string, GameObjectPool>());
                //添加对应类型的对象池字典
            }
            //判断组是否为空
            if (!typeAndGroupPoolDic[type].ContainsKey(group))
            {
                typeAndGroupPoolDic[type].Add(group, new GameObjectPool(type.Name, group));
            }
            GameObjectPool pool = typeAndGroupPoolDic[type][group];
            //已经有存过对应类型的对象池
            poolObj = pool.GetUsefulObj() as T;
            if (poolObj == null)
            {
                poolObj = await pool.CreateObjectAsync(path, parent) as T;
                poolObjAndGroupDic.Add(poolObj, group);
            }
            else
            {
                //如果找到了可用的对象
                poolObj.transform.SetParent(parent);
            }
            return poolObj;

        }

        public void CollectPoolObject<T>(T obj) where T : MonoBehaviour, IPoolObject
        {
            Type type = obj.GetType();
            if (typeAndGroupPoolDic.TryGetValue(type, out Dictionary<string, GameObjectPool> groupPool))
            {
                //根据类型找到组和池后 ，查找所在的组
                if (poolObjAndGroupDic.TryGetValue(obj, out string group))
                {
                    Transform collectParent;
                    //将池对象从原先的父物体中移除，增加到新的父对象中
                    if ((collectParent = transform.Find($"{type.Name}_{group}_Pool")) == null)
                    {
                        //如果原先子对象没有这种类型的池，则创建一个空物体管理对象
                        collectParent = new GameObject($"{type.Name}_{group}_Pool").transform;
                        collectParent.SetParent(transform);
                    }
                    //获取对应组的对象池
                    if (!groupPool.TryGetValue(group, out GameObjectPool pool))
                    {
                        EventCenter.Broadcast(GameEvent.LogInfo, $"没有找到对应的对象池分组：{group}");
                    }
                    else
                    {
                        pool.CollectObject(obj);
                        //将其从原先的父对象中移除
                        //ReflectionUtils.SetPropertyValueByNameIgnoreCase(obj, "parent", collectParent);
                        obj.transform.SetParent(collectParent);
                    }
                }
            }
            else
            {
                EventCenter.Broadcast(GameEvent.LogInfo, $"没有找到对应的类型:{type}");
            }
        }

        //后续需要补Release的方法
        //TODO:------
        //-----------
    }

}