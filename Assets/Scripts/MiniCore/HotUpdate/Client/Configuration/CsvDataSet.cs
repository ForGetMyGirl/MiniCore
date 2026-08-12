using MiniCore.Threading;
using MiniCore.Model;
using MiniCore.Service;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace MiniCore.Core
{

    /// <summary>
    /// CSV行数据集
    /// 构造：传入读取的行数据和可选参数：分隔符
    /// </summary>
    public class CsvDataSet
    {

        private string[] datas;

        public int CurrentIndex { get; set; }

        /// <summary>
        /// 数据长度
        /// </summary>
        public int DataCount { get => datas.Length; }

        /// <summary>
        /// 是否有下一组数据
        /// </summary>
        public bool HaveNext { get => CurrentIndex < DataCount; }

        /// <summary>
        /// 生成CsvDataSet对象
        /// </summary>
        /// <param name="data">数据</param>
        /// <param name="splitChar">数据之间的分隔符</param>
        public CsvDataSet(string data, char splitChar)
        {
            CurrentIndex = 0;
            datas = data.Split(splitChar);
        }

        /// <summary>
        /// 获取下一组数据，如果没有数据会抛出数组越界异常
        /// </summary>
        /// <returns></returns>
        public string Next()
        {
            return datas[CurrentIndex++].TrimEnd('\r');
        }

    }

}
