using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MiniCore.Model;
using MiniCore.Core;


namespace MiniCore.HotUpdate
{
public class TestBall : MonoBehaviour, IPoolObject
{
    bool IPoolObject.IsUseful { get; set; }
    string IPoolObject.GroupName { get; set; }


    void IPoolObject.Clear()
    {
        gameObject.SetActive(false);
    }

    void IPoolObject.Init()
    {
        EventCenter.Broadcast(GameEvent.LogInfo, $"鐢熸垚浜嗕竴涓猅estBall瀵硅薄锛屽綋鍓嶅璞″悕绉帮細{name}锛屽綋鍓嶄綅缃細{transform.position}");
        gameObject.SetActive(true);
    }
}

}
