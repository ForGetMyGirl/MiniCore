using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MiniCore.Model
{
    public static class Extension
    {
        public static void SetVisible(this CanvasGroup canvasGroup, bool isShow)
        {
            canvasGroup.alpha = isShow ? 1 : 0;
            canvasGroup.blocksRaycasts = isShow;
            canvasGroup.interactable = isShow;
        }

        /// <summary>
        /// 只设置显示，不影响交互性。
        /// </summary>
        public static void SetVisbileWithoutInteractable(this CanvasGroup canvasGroup, bool isShow)
        {
            canvasGroup.alpha = isShow ? 1 : 0;
        }

        /// <summary>
        /// 设置交互性。
        /// </summary>
        public static void SetInteractable(this CanvasGroup canvasGroup, bool isInteractable)
        {
            canvasGroup.blocksRaycasts = isInteractable;
            canvasGroup.interactable = isInteractable;
        }

        public static bool GetVisible(this CanvasGroup canvasGroup)
        {
            return !canvasGroup.interactable;
        }

        /// <summary>
        /// 首字母转为大写，其余保持不变。
        /// </summary>
        public static string FirstCharToUpper(this string str)
        {
            char[] chars = str.ToCharArray();
            chars[0] = char.ToUpper(chars[0]);
            return new string(chars);
        }

        /// <summary>
        /// 首字母转为小写，其余保持不变。
        /// </summary>
        public static string FirstCharToLower(this string str)
        {
            char[] chars = str.ToCharArray();
            chars[0] = char.ToLower(chars[0]);
            return new string(chars);
        }

        /// <summary>
        /// 将 int 转换为时间格式（如 0 -> 00）。
        /// </summary>
        public static string TimeFormat(this int time)
        {
            return time >= 0 && time <= 9 ? $"0{time}" : time.ToString();
        }

        /// <summary>
        /// 转换为百分比字符串（取整）。
        /// </summary>
        public static string ToPercent(this float progress)
        {
            float num = progress * 100;
            return $"{(int)num}%";
        }

        /// <summary>
        /// int 转换为分数文本。
        /// </summary>
        public static string ToScore(this int score)
        {
            return $"{score}分";
        }

        /// <summary>
        /// float 转换为分数文本。
        /// </summary>
        public static string ToScore(this float score)
        {
            return $"{score}分";
        }

        /// <summary>
        /// 将二进制数据转换为对象，解析格式为 UTF-8。
        /// </summary>
        public static T BytesToObject<T>(this byte[] buffer)
        {
            string jsonStr = Encoding.UTF8.GetString(buffer);
            EventCenter.Broadcast(GameEvent.LogInfo, "json:" + jsonStr);
            return JsonConvert.DeserializeObject<T>(jsonStr);
        }

        /// <summary>
        /// 将对象转换为二进制数据，解析格式为 UTF-8。
        /// </summary>
        public static byte[] ObjectToBytes(this object obj)
        {
            string jsonStr = GetLowerJson(obj);
            EventCenter.Broadcast(GameEvent.LogInfo, jsonStr);
            return Encoding.UTF8.GetBytes(jsonStr);
        }

        public static string GetLowerJson(object obj)
        {
            return JsonConvert.SerializeObject(obj, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });
        }

        /// <summary>
        /// 生成随机且不重复的数组。
        /// </summary>
        public static T[] GetUnrepeatRandom<T>(this T[] dataArray, int randomCount)
        {
            if (dataArray.Length < randomCount)
            {
                return null;
            }

            T[] result = new T[randomCount];
            int[] indexArr = GetRandomIndexArray(dataArray.Length, randomCount);
            for (int i = 0; i < indexArr.Length; i++)
            {
                int index = indexArr[i];
                result[i] = dataArray[index];
            }
            return result;
        }

        /// <summary>
        /// 生成随机且不重复的数组。
        /// </summary>
        public static T[] GetUnrepeatRandom<T>(this List<T> dataList, int randomCount)
        {
            if (dataList.Count < randomCount)
            {
                return null;
            }

            T[] result = new T[randomCount];
            int[] indexArr = GetRandomIndexArray(dataList.Count, randomCount);
            for (int i = 0; i < indexArr.Length; i++)
            {
                int index = indexArr[i];
                result[i] = dataList[index];
            }
            return result;
        }

        /// <summary>
        /// 获取不重复的随机数组（优化版）。
        /// </summary>
        public static T[] GetUnrepeatRandomArray<T>(this List<T> dataList, int randomCount)
        {
            if (dataList.Count < randomCount)
            {
                throw new System.Exception("需要的随机数量超过列表长度");
            }

            T[] result = new T[randomCount];
            int[] indexArr = GetUnrepeatIndexArray(dataList.Count, randomCount);
            for (int i = 0; i < indexArr.Length; i++)
            {
                int index = indexArr[i];
                result[i] = dataList[index];
            }
            return result;
        }

        /// <summary>
        /// 获取不重复的随机索引数组。
        /// </summary>
        public static int[] GetUnrepeatIndexArray(int arrLength, int randomCount)
        {
            return GetUnrepeatIndexList(arrLength, randomCount).ToArray();
        }

        public static List<int> GetUnrepeatIndexList(int arrLength, int randomCount)
        {
            if (arrLength < randomCount)
            {
                throw new System.Exception("需要的随机索引数量超过设置的长度");
            }

            List<int> list = new List<int>();
            for (int i = 0; i < arrLength; i++)
            {
                int index = Random.Range(0, list.Count + 1);
                list.Insert(index, i);
            }
            return list.Count == randomCount ? list : list.GetRange(0, randomCount);
        }

        private static int[] GetRandomIndexArray(int arrLength, int randomCount)
        {
            int[] resultIndex = new int[randomCount];
            int[] indexArray = new int[arrLength];
            for (int i = 0; i < indexArray.Length; i++)
            {
                indexArray[i] = i;
            }

            for (int j = 0; j < randomCount; j++)
            {
                int arrayIndex = Random.Range(0, indexArray.Length - j);
                resultIndex[j] = indexArray[arrayIndex];
                indexArray[arrayIndex] = indexArray[indexArray.Length - 1 - j];
            }

            return resultIndex;
        }

        public static async Task WaitingForNextFrameAsync()
        {
            int currentFrame = Time.frameCount;
            while (currentFrame == Time.frameCount)
            {
                await Task.Yield();
            }
        }

        /// <summary>
        /// 将文本中的特殊符号替换为换行符。
        /// </summary>
        public static string ConvertNewLine(this string oldStr, string specialChar = "<n>")
        {
            return oldStr.Replace(specialChar, "\n");
        }

        /// <summary>
        /// 将文本中的特殊符号替换为空格。
        /// </summary>
        public static string ConvertSpace(this string oldStr, string specialChara = "@")
        {
            return oldStr.Replace(specialChara, " ");
        }

        /// <summary>
        /// 秒数转换为 hh:mm:ss 格式。
        /// </summary>
        public static string ConvertSecondTime(this float seconds)
        {
            int hour = (int)(seconds / 3600);
            int minute = (int)(seconds % 3600) / 60;
            int second = (int)(seconds - hour * 3600 - minute * 60);

            return $"{hour.TimeFormat()}:{minute.TimeFormat()}:{second.TimeFormat()}";
        }
    }
}
