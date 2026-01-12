using System.Collections;
using System.Collections.Generic;
using MiniCore.Model;
using UnityEngine;

namespace MiniCore.Core
{

    public class TagsComponent : AComponent
    {

        //public const string PlayerTag = "Player";
        private Transform preloadPool;
        public Transform PreloadPool
        {
            get
            {
                preloadPool = preloadPool == null ? GameObject.FindGameObjectWithTag("PreloadGameObjects_Pool").transform : preloadPool;
                return preloadPool;
            }
        }

        private Transform mainCanvas;
        public Transform MainCanvas
        {
            get
            {
                mainCanvas = mainCanvas == null ? GameObject.FindGameObjectWithTag("MainCanvas").transform : mainCanvas;
                return mainCanvas;
            }
        }


        private Transform popupWindowCanvas;
        public Transform PopupWindowCanvas
        {
            get
            {
                popupWindowCanvas = popupWindowCanvas == null ? GameObject.FindGameObjectWithTag("PopupWindowCanvas").transform : popupWindowCanvas;
                return popupWindowCanvas;
            }
        }

        private Transform topCanvas;
        public Transform TopCanvas
        {
            get
            {
                topCanvas = topCanvas == null ? GameObject.FindGameObjectWithTag("TopCanvas").transform : topCanvas;
                return topCanvas;
            }
        }


        private Transform usefulPoolObjects;
        public Transform UsefulPoolObjects
        {
            get
            {
                usefulPoolObjects = usefulPoolObjects != null ? usefulPoolObjects : GameObject.FindGameObjectWithTag("UsefulPoolObjects").transform;
                return usefulPoolObjects;
            }
        }

        private Transform errorCodeCanvas;
        public Transform ErrorCodeCanvas
        {
            get
            {

                errorCodeCanvas = errorCodeCanvas != null ? errorCodeCanvas : GameObject.FindGameObjectWithTag("ErrorCodeCanvas").transform;
                return errorCodeCanvas;
            }
        }

        private Transform bottomCanvas;
        public Transform BottomCanvas
        {
            get
            {

                bottomCanvas = bottomCanvas != null ? bottomCanvas : GameObject.FindGameObjectWithTag("BottomCanvas").transform;
                return bottomCanvas;
            }
        }


    }

}