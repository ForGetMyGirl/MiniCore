using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MiniCore.Model
{
    public class MouseInput : MonoBehaviour
    {
        //private float valueX, valueY;
        [Tooltip("从鼠标松开开始，鼠标移动速度降低的缓冲率，数值越高，停下越快")]
        [Range(0f, 1f)]
        public float moveLerpDamp = 0.1f;

        [Tooltip("从停止滚动开始，鼠标滚动速度降低的缓冲率，数值越高，停下越快")]
        [Range(0f, 1f)]
        public float scrollLerpDamp = 0.1f;

        [Tooltip("鼠标移动灵敏度")]
        public float mouseMoveSensitive = 40;

        [Tooltip("认为移动过程到达临界点的数值")]
        public float moveReachedValue = 0.001f;

        [Tooltip("滚轮滚动的灵敏度")]
        public float scrollSensitive = 200;

        [Tooltip("认为滚动过程到达临界点的数值")]
        public float scrollReachedValue = 0.001f;

        private MouseOutput mouseOutput = new MouseOutput();        //用于接受鼠标实时的移动数据

        [Tooltip("是否开启鼠标移动")]
        public bool mouseMoveEnable = true;
        [Tooltip("是否开启滚轮滚动")]
        public bool mouseScrollEnable = true;

        /// <summary>
        /// <para>当鼠标左键按下且移动时触发的事件</para>
        /// <para>返回值：当前的鼠标移动速率（带缓动慢慢归零且已包含了Time.deltaTime）</para>
        /// </summary>
        public event Action<MouseOutput> OnMouseMove;
        /// <summary>
        /// <para>当鼠标滚轮滚动时触发的事件</para>
        /// <para>返回值：当前滚动的速率（带缓动，会慢慢归零已包含了Time.deltaTime）</para>
        /// </summary>
        public event Action<float> OnMouseWheel;

        private MouseOutput currentFrameMouseOut = new MouseOutput();       //用于接受当前帧的鼠标移动数据
        private float currentFrameMouseScroll;          //用于接受当前帧的滚轮数据

        #region 函数方法内需要用到的变量
        float mouseX, mouseY;
        float mouseScoll;
        private float mouseScrollValue;
        #endregion

        /// <summary>
        /// 是否在可交互区域内
        /// </summary>
        public bool WithInInteractiveArea { get; set; } = true;

        /// <summary>
        /// 监听鼠标移动事件，添加了缓动
        /// </summary>
        /// <param name="currentMouseOutput">当前帧返回的鼠标速率</param>
        public void MouseMoveUpdate()
        {


            mouseX = Input.GetAxis("Mouse X");
            mouseY = Input.GetAxis("Mouse Y");

            if (Input.GetMouseButton(0) && WithInInteractiveArea /*&& mouseX != 0*/)
            {
                mouseOutput.ValueX += mouseX;
                mouseOutput.ValueY += mouseY;
            }

            if (mouseOutput.ValueX != 0 || mouseOutput.ValueY != 0)
            {

                currentFrameMouseOut.ValueX = mouseOutput.ValueX * mouseMoveSensitive * Time.deltaTime;      //当前帧的值为鼠标输出值*灵敏对
                currentFrameMouseOut.ValueY = mouseOutput.ValueY * mouseMoveSensitive * Time.deltaTime;

                //触发事件
                //if (WithInInteractiveArea)     //如果不在可交互区域，不执行
                OnMouseMove?.Invoke(currentFrameMouseOut);
            }

            mouseOutput.ValueX = Mathf.Lerp(mouseOutput.ValueX, 0, moveLerpDamp);
            mouseOutput.ValueY = Mathf.Lerp(mouseOutput.ValueY, 0, moveLerpDamp);

            if (Mathf.Abs(mouseOutput.ValueX) <= scrollReachedValue)      //接近0 
                mouseOutput.ValueX = 0;
            if (Mathf.Abs(mouseOutput.ValueY) <= scrollReachedValue)
                mouseOutput.ValueY = 0;

        }


        /// <summary>
        /// 监听鼠标滚轮滚动事件，添加了缓动
        /// </summary>
        /// <param name="mouseScroll">当前帧返回的鼠标滚动动值</param>
        public void MouseWheelUpdate()
        {
            //if (!WithInInteractiveArea)     //如果不在可交互区域，不执行任何检测
            //    return;

            mouseScoll = Input.GetAxis("Mouse ScrollWheel");
            if (mouseScoll != 0 && WithInInteractiveArea)
            {
                mouseScrollValue += mouseScoll;

            }

            currentFrameMouseScroll = mouseScrollValue * scrollSensitive * Time.deltaTime;

            OnMouseWheel?.Invoke(currentFrameMouseScroll);  //触发鼠标滚动事件

            mouseScrollValue = Mathf.Lerp(mouseScrollValue, 0, scrollLerpDamp);

            if (Mathf.Abs(mouseScrollValue) <= moveReachedValue)      //接近0 
                mouseScrollValue = 0;
        }


        private void Update()
        {
            if (mouseMoveEnable)
            {
                //开启了鼠标移动检测
                MouseMoveUpdate();
            }
            if (mouseScrollEnable)
            {
                //开启了滚动滚动检测
                MouseWheelUpdate();
            }

        }


    }



    public struct MouseOutput
    {
        public float ValueX { get; set; }
        public float ValueY { get; set; }


    }
}