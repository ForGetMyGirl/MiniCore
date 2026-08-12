using MiniCore.Model;
using UnityEngine;

namespace MiniCore.HotUpdate
{
    /// <summary>
    /// 用于演示对象池生命周期的测试球组件。
    /// </summary>
    public sealed class TestBall : MonoBehaviour, IPoolObject
    {
        #region Interface 接口实现

        /// <summary>
        /// 清理本次租用状态并隐藏对象。
        /// </summary>
        void IPoolObject.Clear()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 初始化新一次租用并显示对象。
        /// </summary>
        void IPoolObject.Init()
        {
            gameObject.SetActive(true);
        }

        #endregion
    }
}
