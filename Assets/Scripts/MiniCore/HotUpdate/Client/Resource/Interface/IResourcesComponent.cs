using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MiniCore.Model
{

    public interface IResourcesComponent
    {
        /// <summary>
        /// 加载资源
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        UniTask<T> LoadAssetAsync<T>(string key) where T : Object;

        /// <summary>
        /// 实例化对象
        /// </summary>
        /// <param name="key">资源名/资源地址</param>
        /// <param name="parent">父对象</param>
        /// <returns></returns>
        UniTask<GameObject> InstantiateAsync(string key, Transform parent = null);

        /// <summary>
        /// 预加载资源
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="key">资源名/资源地址</param>
        /// <returns></returns>
        UniTask<T> PreloadAssetsAsync<T>(string key) where T : Object;

        /// <summary>
        /// 释放资源
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        bool ReleaseAssetAsync(string key);

        /// <summary>
        /// 释放实例化对象
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        bool ReleaseInstance(GameObject obj);
    }

}