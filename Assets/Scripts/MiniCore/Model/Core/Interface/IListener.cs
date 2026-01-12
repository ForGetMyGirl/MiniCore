using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiniCore.Model
{
    public interface IListener
    {
        void StartListener();

        void StopListener();
    }
}
