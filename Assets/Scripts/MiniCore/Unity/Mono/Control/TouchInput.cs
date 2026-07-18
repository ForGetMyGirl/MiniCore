using System;
using UnityEngine;
namespace MiniCore.Model
{
    public class TouchInput : MonoBehaviour
    {
        //private float valueX, valueY;
        [Tooltip("从手指松开开始，触摸移动速度降低的缓冲率，数值越高，停下越快")]
        [Range(0f, 1f)]
        public float moveLerpDamp = 0.1f;

        [Tooltip("从停止缩放开始，缩放速度降低的缓冲率，数值越高，停下越快")]
        [Range(0f, 1f)]
        public float touchZoomDamp = 0.1f;

        [Tooltip("触摸移动灵敏度")]
        public float touchMoveSensitive = 40;

        [Tooltip("认为移动过程到达临界点的数值")]
        public float moveReachedValue = 0.001f;

        [Tooltip("双指缩放的灵敏度")]
        public float touchZoomSensitive = 200;

        [Tooltip("认为滚动过程到达临界点的数值")]
        public float touchZoomReachedValue = 0.001f;

        [Tooltip("是否开启鼠标移动")]
        public bool touchMoveEnable = true;
        [Tooltip("是否开启滚轮滚动")]
        public bool touchZoomEnable = true;

        /// <summary>
        /// <para>当单指移动时触发的事件</para>
        /// <para>返回值：当前的手指移动速率（带缓动慢慢归零且已包含了Time.deltaTime）</para>
        /// </summary>
        public event Action<Vector2> OnTouchMove;

        /// <summary>
        /// <para>当双指缩放时触发的事件</para>
        /// <para>返回值：当前缩放的速率（带缓动，会慢慢归零已包含了Time.deltaTime）</para>
        /// </summary>
        public event Action<float> OnTouchZoom;

        //private MouseOutput currentFrameMouseOut = new MouseOutput();       //用于接受当前帧的鼠标移动数据
        private Vector2 touchMovePosition;              //最后接收到的移动数据值

        #region 函数方法内需要用到的变量
        private Vector2 currentTouchMovePosition;       //当前帧的触摸移动数据
        private Vector2 deltaTouchMovePosition;         //当前帧较上一帧的移动差值
        private Vector2 lastTouchPosition;



        private float lastTouchDistance;    //上一次双指的距离
        private float currentTouchDistance; //本次双指的距离
        private float deltaTouchDistance;   //本次跟上次双指距离的差值

        private float touchDistance;        //最后接受到的触摸距离
        private float touchDistanceOut;     //输出的距离值
        #endregion

        private bool isMoving;      //是否正在滑动
        private bool isZooming;     //是否正在缩放，缩放的时候禁止滑动

        /// <summary>
        /// 每帧检测触摸移动，带缓动
        /// </summary>
        public void TouchMoveUpdate()
        {
            if (isZooming) return;
            if (Input.touchCount == 1)
            {
                isMoving = true;
                //单指触摸
                //记录滑动值
                if (Input.touches[0].phase == TouchPhase.Began)
                {
                    //第一次检测
                    lastTouchPosition = Input.touches[0].position;
                }
                else if (Input.touches[0].phase == TouchPhase.Moved)
                {
                    //移动检测
                    deltaTouchMovePosition = Input.touches[0].position - lastTouchPosition;  //计算上一帧的位移
                    lastTouchPosition = Input.touches[0].position;          //
                    touchMovePosition += deltaTouchMovePosition;

                    currentTouchMovePosition = touchMovePosition * touchMoveSensitive * Time.deltaTime;

                }

            }

            if (isMoving)
            {
                OnTouchMove?.Invoke(currentTouchMovePosition);
                //添加缓动
                touchMovePosition = Vector2.Lerp(touchMovePosition, Vector2.zero, moveLerpDamp);

                if (Vector2.Distance(touchMovePosition, Vector2.zero) <= moveReachedValue)
                {
                    //当滑动接近0，判定为0
                    touchMovePosition = Vector2.zero;
                    isMoving = false;
                }
            }


        }

        /// <summary>
        /// 每帧检测双指缩放，带缓动
        /// </summary>
        public void TouchZoomUpdate()
        {
            if (Input.touchCount == 2)
            {
                isZooming = true;
                //双指缩放
                //判断是否是第一次双指
                Touch touch1 = Input.touches[0];
                Touch touch2 = Input.touches[1];
                if (touch1.phase == TouchPhase.Began || touch2.phase == TouchPhase.Began)
                {
                    //第一次双指距离记录
                    lastTouchDistance = Vector2.Distance(touch1.position, touch2.position);
                }
                else if (touch1.phase == TouchPhase.Moved)
                {
                    //记录新的位置
                    currentTouchDistance = Vector2.Distance(touch1.position, touch2.position);
                    //计算距离差值
                    deltaTouchDistance = currentTouchDistance - lastTouchDistance;
                    lastTouchDistance = currentTouchDistance;   //当前差变为上一帧的差值

                    touchDistance += deltaTouchDistance;

                    touchDistanceOut = touchDistance * touchZoomSensitive * Time.deltaTime;

                }
            }

            if (isZooming)
            {
                OnTouchZoom?.Invoke(touchDistanceOut);
                touchDistance = Mathf.Lerp(touchDistance, 0, touchZoomDamp);

                if (Mathf.Abs(touchDistance) <= touchZoomReachedValue)
                {
                    touchDistance = 0;
                    isZooming = false;
                }
            }
        }

        private void Update()
        {
            if (touchMoveEnable)
            {
                //开启了鼠标移动检测
                TouchMoveUpdate();
            }
            if (touchZoomEnable)
            {
                //开启了滚动滚动检测
                TouchZoomUpdate();
            }

        }


    }

}