using System;
using System.Collections.Generic;

namespace MiniCore.Model
{

    public abstract class AComponent : IDisposable
    {
        private Dictionary<Type, AComponent> components;
        private List<AComponent> componentSnapshot;

        public bool IsActive { get; set; }

        public T GetComponent<T>() where T : AComponent, new()
        {
            return components[typeof(T)] as T;
        }


        public void AddComponent(AComponent component)
        {
            if (component == null)
            {
                throw new ArgumentNullException(nameof(component));
            }

            Type type = component.GetType();
            if (components == null)
            {
                components = new Dictionary<Type, AComponent>();
            }

            if (!components.ContainsKey(type))
            {
                component.Awake();
                components.Add(type, component);
                component.IsActive = true;
            }
        }

        public virtual void Awake() { }

        public virtual void Awake(object[] obj) { }

        public void RemoveComponent(AComponent component)
        {
            if (component == null || components == null)
            {
                return;
            }

            Type type = component.GetType();
            if (components.ContainsKey(type))
            {
                components.Remove(type);
                component.IsActive = false;
            }
        }

        public T AddComponent<T>() where T : AComponent, new()
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

        public void RemoveComponent<T>() where T : AComponent, new()
        {
            Type type = typeof(T);
            if (components.ContainsKey(type))
            {
                components[type].IsActive = false;
                components[type] = null;
                components.Remove(type);
            }
        }

        public T AddComponent<T>(object[] args) where T : AComponent, new()
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


        public virtual void Dispose()
        {
            //调用子类的Dispose
            //foreach (AComponent component in components.Values)
            //{
            //    component.Dispose();
            //}
            if (components != null)
            {
                int snapshotCount = RefreshSnapshot();
                if (snapshotCount == 0)
                {
                    IsActive = false;
                    return;
                }

                for (int i = 0; i < componentSnapshot.Count; i++)
                {
                    var component = componentSnapshot[i];
                    component?.Dispose();
                }
                componentSnapshot.Clear();
                components.Clear();
            }
            IsActive = false;
        }

        protected virtual void Update() { }

        public void MonoUpdate()
        {
            if (!IsActive) return;
            if (components != null)
            {
                int snapshotCount = RefreshSnapshot();
                if (snapshotCount == 0)
                {
                    Update();
                    return;
                }

                for (int i = 0; i < componentSnapshot.Count; i++)
                {
                    var component = componentSnapshot[i];
                    component?.MonoUpdate();
                }
            }
            Update();
        }

        private int RefreshSnapshot()
        {
            if (components == null || components.Count == 0)
            {
                return 0;
            }

            if (componentSnapshot == null)
            {
                componentSnapshot = new List<AComponent>(Math.Max(components.Count, 2));
            }

            componentSnapshot.Clear();
            componentSnapshot.AddRange(components.Values);
            return componentSnapshot.Count;
        }
    }

}
