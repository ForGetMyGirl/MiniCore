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

        /// <summary>
        /// Get an already-registered component.
        /// Throws when missing to keep hard dependency checks explicit.
        /// </summary>
        public T Get<T>() where T : AComponent, new()
        {
            if (components == null)
            {
                throw new InvalidOperationException($"Global component container is not initialized. Missing: {typeof(T).FullName}");
            }

            if (!components.TryGetValue(typeof(T), out var value) || value == null)
            {
                throw new InvalidOperationException($"Global component not found: {typeof(T).FullName}. Use TryGet/GetOrAdd or ensure Add<T>() in scene enter.");
            }

            return value as T;
        }

        /// <summary>
        /// Try get a component without throwing.
        /// </summary>
        public bool TryGet<T>(out T component) where T : AComponent, new()
        {
            component = null;
            if (components == null)
            {
                return false;
            }

            if (components.TryGetValue(typeof(T), out var value))
            {
                component = value as T;
                return component != null;
            }

            return false;
        }

        /// <summary>
        /// Get component when exists, otherwise create and register one.
        /// </summary>
        public T GetOrAdd<T>() where T : AComponent, new()
        {
            if (TryGet<T>(out var component))
            {
                return component;
            }

            return Add<T>();
        }

        /// <summary>
        /// Get component when exists, otherwise create and register one with constructor args.
        /// </summary>
        public T GetOrAdd<T>(object[] args) where T : AComponent, new()
        {
            if (TryGet<T>(out var component))
            {
                return component;
            }

            return Add<T>(args);
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
                throw new Exception("Component type already exists: " + type);
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
                throw new Exception("Component type already exists: " + type);
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
