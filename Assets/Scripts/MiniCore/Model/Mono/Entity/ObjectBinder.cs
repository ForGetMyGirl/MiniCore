using System;
using System.Collections.Generic;

namespace MiniCore
{
    public class ObjectBinder<T>
    {

        private Dictionary<T, List<object>> keyValuePairs = new Dictionary<T, List<object>>();
        private Dictionary<T, List<Type>> keyTypePairs = new Dictionary<T, List<Type>>();

        /// <summary>
        /// 将数据与另一个数据绑定
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <exception cref="Exception">如果同一个键的值重复会报错</exception>
        public void Bind(T key, object value)
        {
            Type valueType = value.GetType();
            //List<object> objectList;
            if (!keyTypePairs.TryGetValue(key, out List<Type> list))
            {
                list = new List<Type>();
                keyTypePairs.Add(key, list);
                keyValuePairs.Add(key, new List<object>());

            }
            else
            {
                if (list.Contains(valueType))
                {
                    throw new Exception($"已经存在的类型：{valueType},值：{value}");
                }
            }
            list.Add(valueType);
            keyValuePairs[key].Add(value);
            //keyValuePairs[key] = objectList;

        }

        /// <summary>
        /// 获取已经绑定的键的对应类型的值
        /// </summary>
        /// <typeparam name="K">要返回的类型</typeparam>
        /// <param name="key">键</param>
        /// <returns>如果不存在会返回null</returns>
        public K GetValue<K>(T key) where K : class
        {
            if (keyTypePairs.TryGetValue(key, out List<Type> list))
            {
                int index = list.IndexOf(typeof(K));
                if (index != -1)
                {
                    return keyValuePairs[key][index] as K;
                }

            };
            return null;
        }

        /// <summary>
        /// 将键与值解绑
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        public void Unbind(T key, object value)
        {
            if (keyTypePairs.TryGetValue(key, out List<Type> list))
            {
                int index = list.IndexOf(value.GetType());
                if (index != -1)
                {
                    list.RemoveAt(index);
                    keyValuePairs[key].RemoveAt(index);
                }


            }
        }

    }

}