using MiniCore.Threading;
using MiniCore.Model;
using MiniCore.Service;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace MiniCore.Core
{

    public class CsvTable<T> : IExcelConfig where T : ICsvTable
    {
        /// <summary>
        /// 行数
        /// </summary>
        public int DataCount { get => RawDatas.Count; }

        /// <summary>
        /// 第二行的字段名数组，为了查找数据对应的字段名
        /// </summary>
        public string[] FieldNames { get; set; }

        /// <summary>
        /// 每行的数据
        /// </summary>
        public List<T> RawDatas { get; set; }
    }

}
