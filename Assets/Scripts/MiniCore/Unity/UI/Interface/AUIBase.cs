using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MiniCore.Model
{

    public abstract class AUIBase : MonoBehaviour
    {

        public virtual UniTask OpenAsync() { return UniTask.CompletedTask; }

        public virtual UniTask CloseAsync() { return UniTask.CompletedTask; }

    }

}
