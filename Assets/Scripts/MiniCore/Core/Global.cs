using System;
using System.Collections;
using System.Collections.Generic;
using MiniCore.Model;
using UnityEngine;
namespace MiniCore.Core
{

    public class Global : MonoBehaviour
    {
        private bool disposed;
        private bool isQuitting;
        //public MiniCoreComponent Com { get; private set; }
        //protected override void Init()
        //{
        //    //base.Init();
        //    Com = new MiniCoreComponent();
        //}

        #region Mono单例
        private static Global com;
        public static Global Com
        {
            get
            {
                if (com == null)
                {
                    com = FindObjectOfType<Global>();
                    if (com == null)
                    {
                        //保证脚本先Awake再进行Init();
                        new GameObject($"Global_Singleton").AddComponent<Global>();
                    }
                    else
                    {
                        com.Init();
                    }
                }
                return com;
            }
        }

        #endregion

        #region 生命周期函数

        private void Awake()
        {
            if (com == null)
            {
                com = this;
                Init();
            }
        }

        private void OnApplicationQuit()
        {
            isQuitting = true;
            Shutdown();
        }

        private void OnDestroy()
        {
            Shutdown();
        }

        #endregion

        #region 组件控制
        private Dictionary<Type, AComponent> components;

        public T Get<T>() where T : AComponent, new()
        {
            return components[typeof(T)] as T;
        }


        public void Add(AComponent component)
        {
            Type type = component.GetType();
            if (components == null)
            {
                components = new Dictionary<Type, AComponent>();
            }
            else
            {
                if (!components.ContainsKey(type))
                {
                    component.Awake();
                    components.Add(type, component);
                    component.IsActive = true;
                }
            }
        }


        public T Add<T>() where T : AComponent, new()
        {
            Type type = typeof(T);
            if (components == null)
            {
                components = new Dictionary<Type, AComponent>();
            }
            T obj;
            if (!components.ContainsKey(type))
            {
                //创建一个Component实例
                obj = Activator.CreateInstance<T>();
                obj.Awake();
                components.Add(type, obj);
                obj.IsActive = true;
            }
            else
            {
                throw new Exception("已经存在的组件类型：" + type);
            }
            return obj;
        }

        public void Remove<T>() where T : AComponent, new()
        {
            Type type = typeof(T);
            if (components.ContainsKey(type))
            {
                components[type].IsActive = false;
                components[type] = null;
                components.Remove(type);
            }
        }

        public T Add<T>(object[] args) where T : AComponent, new()
        {
            Type type = typeof(T);
            if (components == null)
            {
                components = new Dictionary<Type, AComponent>();
            }
            T obj;
            if (!components.ContainsKey(type))
            {
                //创建一个Component实例
                obj = Activator.CreateInstance<T>();
                obj.Awake(args);
                components.Add(type, obj);
                obj.IsActive = true;
            }
            else
            {
                throw new Exception("已经存在的组件类型：" + type);
            }
            return obj;
        }

        void Update()
        {
            if (components != null)
            {
                foreach (var component in components.Values)
                {
                    component.MonoUpdate();
                }
            }
        }

        #endregion
        protected virtual void Init()
        {
            DontDestroyOnLoad(gameObject);
        }

        public virtual void Dispose()
        {
            //调用子类的Dispose
            foreach (AComponent component in components.Values)
            {
                component.Dispose();
            }
            if (!isQuitting)
            {
                Destroy(gameObject);
            }
        }

        private void Shutdown()
        {
            if (disposed)
            {
                return;
            }
            disposed = true;
            if (components == null)
            {
                return;
            }
            Dispose();
        }
    }

    //public class MiniCoreComponent : AComponent
    //{
    //}
}
