using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MiniCore.Model;


namespace MiniCore.Core
{
    public class GlobalListenerComponent : AComponent
    {
    private Transform listenersContent;


    public void RegisterAllListeners(Transform listenersContent)
    {
        this.listenersContent = listenersContent;
        IListener[] listeners = listenersContent.GetComponentsInChildren<IListener>();
        for (int i = 0; i < listeners.Length; i++)
        {
            listeners[i].StartListener();
        }
    }


    public void RemoveAllListeners()
    {
        IListener[] listeners = listenersContent.GetComponentsInChildren<IListener>();
        for (int i = 0; i < listeners.Length; i++)
        {
            listeners[i].StopListener();
        }
    }

}

}
