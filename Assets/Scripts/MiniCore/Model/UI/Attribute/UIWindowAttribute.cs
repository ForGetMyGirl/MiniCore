using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MiniCore.Model
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class UIWindowAttribute : Attribute
    {
        //public string WindowName { get; private set; }
        public Type PresenterType { get; private set; }
        public UICanvasLayer CanvasLayer { get; private set; }

        /*public UIWindowAttribute(string windowName, UICanvasLayer layer)
        {
            WindowName = windowName;
            CanvasLayer = layer;
        }*/

        public UIWindowAttribute(Type presenterType)
        {
            PresenterType = presenterType;
        }
    }

}
