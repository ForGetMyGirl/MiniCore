using System;
using System.Collections.Generic;
namespace MiniCore.Model
{

    public class RegisterEvent
    {

        private event Action OnEvents;

        private List<Action> registeredActionList = new List<Action>();

        public void Invoke()
        {
            OnEvents?.Invoke();
        }

        public void AddListener(Action action)
        {
            OnEvents += action;
            registeredActionList.Add(action);
        }

        public void RemoveListener(Action action)
        {
            OnEvents -= action;
            registeredActionList.Remove(action);
        }

        public void RemoveAllListeners()
        {
            for (int i = registeredActionList.Count - 1; i >= 0; i--)
            {
                Action action = registeredActionList[i];
                OnEvents -= action;
                registeredActionList.Remove(action);
            }
        }

    }
}