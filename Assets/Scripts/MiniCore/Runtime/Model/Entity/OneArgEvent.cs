using System;
using System.Collections.Generic;
namespace MiniCore.Model
{

    public class OneArgEvent<T>
    {

        private event Action<T> OnEvents;

        private List<Action<T>> registeredActionList = new List<Action<T>>();

        public void Invoke(T value)
        {
            OnEvents?.Invoke(value);
        }

        public void AddListener(Action<T> action)
        {
            OnEvents += action;
            registeredActionList.Add(action);
        }

        public void RemoveListener(Action<T> action)
        {
            OnEvents -= action;
            registeredActionList.Remove(action);
        }

        public void RemoveAllListeners()
        {
            for (int i = registeredActionList.Count - 1; i >= 0; i--)
            {
                Action<T> action = registeredActionList[i];
                OnEvents -= action;
                registeredActionList.Remove(action);
            }
        }

    }
}