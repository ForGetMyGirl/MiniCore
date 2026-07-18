using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MiniCore.Model
{
    [Serializable]
    public class ReferenceData
    {
        public string key;
        public Object value;
    }
    public class ReferenceCollector : MonoBehaviour, ISerializationCallbackReceiver
    {
        //public TestSceneEntry testSceneEntry;
        //public List<ManualInteractive> manualInteractives = new List<ManualInteractive>();

        public List<ReferenceData> referenceDatas;
        private readonly Dictionary<string, Object> referencesDic = new Dictionary<string, Object>();

        //public string manualName = "";
        //public ManualInteractive manualInteractive;
        public ReferenceData referenceData = new ReferenceData();

#if UNITY_EDITOR
        public void Register()
        {
            referenceDatas.Add(referenceData);
            //referencesDic.Add(referenceData.key, referenceData.value);
            referenceData = new ReferenceData();
        }

        public void Unregister(int index)
        {
            //referencesDic.Remove(referenceDatas[index].key);
            referenceDatas.RemoveAt(index);
        }

#endif

        public T Get<T>(string key) where T : class
        {

            if (!referencesDic.TryGetValue(key, out Object value))
            {
                return null;
            }
            return value as T;
        }

        public Object GetObject(string key)
        {
            if (referencesDic.TryGetValue(key, out Object value))
            {
                return null;
            }
            return value;
        }

        public void OnBeforeSerialize()
        {
        }

        /// <summary>
        /// 反序列化之后
        /// </summary>
        public void OnAfterDeserialize()
        {
            referencesDic.Clear();
            foreach (ReferenceData data in referenceDatas)
            {
                if (!referencesDic.ContainsKey(data.key))
                {
                    referencesDic.Add(data.key, data.value);
                }
            }
        }

        public Dictionary<string, Object>.KeyCollection Keys => referencesDic.Keys;

    }

}
