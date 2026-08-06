using UnityEngine;

namespace MiniCore.Bootstrap
{
    /// <summary>
    /// 为只存在于 YooAsset 动态内容中的 Unity 原生类型建立显式 AOT 代码依赖。
    /// </summary>
    public static class UnityEngineTypePreserver
    {
        #region Public 公共成员

        /// <summary>
        /// 通过启动流程可达的类型引用保留客户端动态角色、动画和粒子所需的 Unity 原生实现。
        /// </summary>
        public static void ProtectDynamicContentTypes()
        {
#if !UNITY_EDITOR && !UNITY_SERVER
            Debug.Log(typeof(AnimationClip));
            Debug.Log(typeof(Avatar));
            Debug.Log(typeof(SkinnedMeshRenderer));
            Debug.Log(typeof(ParticleSystem));
            Debug.Log(typeof(ParticleSystemRenderer));

            var nativeTypeAnchor = new GameObject("MiniCore.NativeTypeAnchor")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            nativeTypeAnchor.SetActive(false);
            nativeTypeAnchor.AddComponent<SkinnedMeshRenderer>();
            Object.Destroy(nativeTypeAnchor);
#endif
        }

        #endregion
    }
}
