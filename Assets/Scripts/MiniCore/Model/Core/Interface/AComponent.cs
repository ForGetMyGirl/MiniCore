using System;
using System.Collections.Generic;

namespace MiniCore.Model
{

    public abstract class AComponent : IDisposable
    {
        private Dictionary<Type, AComponent> components;

        public bool IsActive { get; set; }

        public T GetComponent<T>() where T : AComponent, new()
        {
            return components[typeof(T)] as T;
        }


        public void AddComponent(AComponent component)
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

        public virtual void Awake() { }

        public virtual void Awake(object[] obj) { }

        public void RemoveComponent(AComponent component)
        {
            if (components.ContainsKey(component.GetType()))
            {
                component = null;
                components.Remove(component.GetType());
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
                foreach (var type in components.Keys)
                {
                    components[type].Dispose();
                    components[type] = null;
                }
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
                foreach (var component in components.Values)
                {
                    //if (component.IsActive)
                    component.MonoUpdate();
                }
            }
            Update();
        }
    }

}
