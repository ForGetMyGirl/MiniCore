using Cysharp.Threading.Tasks;
using MiniCore.Model;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace MiniCore.Core
{
    /// <summary>
    /// 接口用于表明是Excel格式表
    /// </summary>
    public interface IExcelConfig
    {
    }

    public interface ICsvTable
    {
        int Id { get; set; }
    }

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


    public static class ExcelTool
    {


        /// <summary>
        /// 异步加载Csv文件
        /// </summary>
        /// <typeparam name="T">Csv对应的字段类型组成的类</typeparam>
        /// <param name="path">配置文件所在的Addressable路径</param>
        /// <returns>返回CsvTable<T>类型的对象</returns>
        public static async UniTask<CsvTable<T>> LoadCsvFileAsync<T>(string path) where T : ICsvTable, new()
        {
            TextAsset context = await Global.Com.Get<AssetsComponent>().LoadAssetAsync<TextAsset>(path);
            string contextResult = context.text.TrimEnd('\r', '\n');
            return DeserializeContext<T>(contextResult);
        }


        /// <summary>
        /// 反序列化Csv文本信息
        /// </summary>
        /// <typeparam name="T">反序列化后的行数据的格式类</typeparam>
        /// <param name="context">Csv文本信息</param>
        /// <returns>CsvTable类型的表数据对象</returns>
        public static CsvTable<T> DeserializeContext<T>(string context) where T : ICsvTable, new()
        {

            CsvTable<T> csvTable = new CsvTable<T>()
            {
                RawDatas = new List<T>()
            };




            //string[] allLines = context.Split('\n');
            CsvDataSet allLineDataSet = new CsvDataSet(context, '\n');  //获取所有行数据

            //string[] fieldNames;    //字段名称
            //前两行是字段信息
            try
            {
                //这个try是解析字段名称

                //第一行是中文提示，不存储
                allLineDataSet.Next();
                //第二行是 字段名#数据类型，程序只需要保存第一个字段名即可。类型通过T中的字段类型进行动态创建
                string fieldNameRaw = allLineDataSet.Next();
                CsvDataSet headRawDataSet = new CsvDataSet(fieldNameRaw, ',');
                //fieldNames = new string[headRawDataSet.DataCount];
                csvTable.FieldNames = new string[headRawDataSet.DataCount];
                int colIndex = 0;
                while (headRawDataSet.HaveNext)
                {
                    CsvDataSet fieldNameAndType = new CsvDataSet(headRawDataSet.Next(), '#');
                    csvTable.FieldNames[colIndex++] = fieldNameAndType.Next();
                }

                #region 筛选只用于Excel配置不用于程序读取的属性（参数）
                //读取所有的 不需要存储到内存中的属性
                List<string> propertyNameList = new List<string>();
                Type dataType = typeof(T);
                //dataType.GetCustomAttributes<UnreadPropertyAttribute>();
                PropertyInfo[] propertyInfos = dataType.GetProperties();
                for (int i = 0; i < propertyInfos.Length; i++)
                {
                    PropertyInfo propertyInfo = propertyInfos[i];
                    if (propertyInfo.GetCustomAttribute<UnreadPropertyAttribute>() != null)
                    {
                        propertyNameList.Add(propertyInfo.Name);
                    }
                }
                #endregion


                //理论上这个耗时操作应该开启异步

                //这里应该是从第三行开始
                while (allLineDataSet.HaveNext)
                {

                    T objectT = new T();

                    int rawIndex = 0;   //当前是第几行数据
                    string lineData = allLineDataSet.Next();    //当前行数据
                    CsvDataSet rawDataSet = new CsvDataSet(lineData, ',');   //当前行 数据集
                    int columnIndex = 0;    //当前列数
                    while (rawDataSet.HaveNext)
                    {
                        //列数据
                        string colValue = rawDataSet.Next();    //当前列（一个格子）的值
                                                                //解析当前列的数据类型
                        string fieldName = csvTable.FieldNames[columnIndex++];



                        #region 将格子中的数据放到对应的字段值中

                        //判断该属性是否需要读取
                        //if (propertyNameList.Contains(fieldName))
                        //    InsertDefaultValue(objectT, fieldName);
                        //else
                        if (!propertyNameList.Contains(fieldName))
                            InsertValueByPropertyName(objectT, fieldName, colValue);
                        //不需要读取的属性直接不赋值
                        #endregion

                    }
                    csvTable.RawDatas.Add(objectT);
                    rawIndex++;
                }

            }
            catch (Exception e)
            {
                EventCenter.Broadcast(GameEvent.LogInfo, $"Csv数据解析异常：\n{e}");
            }

            return csvTable;
        }


        private static void InsertDefaultValue(object obj, string propertyName)
        {

            //Type propertyType = ReflectionUtils.GetPropertyTypeByNameIgnoreCase(obj, propertyName);
            //ReflectionUtils.SetPropertyValueByNameIgnoreCase(obj, propertyName, default(ValueType));
            //Reflection
            //不往里面插入值也实现了。。。
        }
        /// <summary>
        /// 通过属性名插入值
        /// </summary>
        /// <param name="obj">类对象</param>
        /// <param name="propertyName">属性名</param>
        /// <param name="colValue">值</param>
        /// <param name="rawIndex">行索引，在字段类型为数组的时候使用</param>
        /// <param name="length">如果值类型对象为空，则创建的数据类型长度</param>
        private static void InsertValueByPropertyName(object obj, string propertyName, string colValue)
        {

            //反射获取当前字段的数据类型
            Type propertyType = ReflectionUtils.GetPropertyTypeByNameIgnoreCase(obj, propertyName);
            //如果属性类型是short,int,float,double,bool,string,则直接赋值
            bool isValueStruct = IsValueStruct(propertyType);
            if (isValueStruct)
            {
                //如果是值类型，则将数据转换为对应类型后直接赋值
                object correctValue = Convert.ChangeType(colValue, propertyType);   //真正的值：转换到正确类型后的值
                ReflectionUtils.SetPropertyValueByNameIgnoreCase(obj, propertyName, correctValue);
            }
            else
            {
                SetPropertyValueByType(propertyType, obj, propertyName, colValue);
            }

        }

        /// <summary>
        /// 通过属性类型设置属性值，属性为：数组、List、Dictionary（暂时不包含）时
        /// </summary>
        /// <param name="type">属性类型</param>
        /// <param name="obj">要设置的类对象</param>
        /// <param name="propertyName">属性名</param>
        /// <param name="colValue">属性值</param>
        /// <param name="rawIndex">值在属性中的索引</param>
        /// <param name="length">如果值类型对象为空，则创建的数据类型长度</param>
        private static void SetPropertyValueByType(Type type, object obj, string propertyName, string colValue)
        {

            //现在只处理数组、List类型、Dictionary
            CsvDataSet unitDataSet = new CsvDataSet(colValue, ';'); //单元格内的数据以 英文的分号作为分隔符
            if (type.IsArray)
            {

                //判断数组是否还没有创建
                //type
                Type elementType = type.GetElementType();
                //elementType.MakeArrayType();
                Array myArr = Array.CreateInstance(elementType, unitDataSet.DataCount);
                while (unitDataSet.HaveNext)
                {
                    //如果有下一个数据，放进数组中
                    int index = unitDataSet.CurrentIndex;
                    string unitData = unitDataSet.Next();
                    var oneUnitData = Convert.ChangeType(unitData, elementType);  //转换成对应的类型
                    myArr.SetValue(oneUnitData, index);
                }

                ReflectionUtils.SetPropertyValueByNameIgnoreCase(obj, propertyName, myArr);


            }
            else if (type.IsGenericType && type == typeof(List<>).MakeGenericType(type.GetGenericArguments()[0]))
            {

                //获取list的泛型类型
                Type listArgumentType = type.GetGenericArguments()[0];
                //创建List<listArgumentType>对象

                Type fullListType = typeof(List<>).MakeGenericType(listArgumentType);       //创建List<T>类型
                object listObj = Activator.CreateInstance(fullListType);  //创建List<T>实例
                MethodInfo methodInfo = listObj.GetType().GetMethod("Add"); //获取Add方法
                while (unitDataSet.HaveNext)
                {
                    //如果有下一个数据，添加到list中
                    //int index = unitDataSet.CurrentIndex;
                    string unitData = unitDataSet.Next();
                    var oneUnitData = Convert.ChangeType(unitData, listArgumentType);
                    methodInfo.Invoke(listObj, new object[] { oneUnitData });   //Add 数据
                }
                ReflectionUtils.SetPropertyValueByNameIgnoreCase(obj, propertyName, listObj);


            }
            else if (type.IsEnum)
            {
                object value = Enum.Parse(type, colValue);
                ReflectionUtils.SetPropertyValueByNameIgnoreCase(obj, propertyName, value);
            }
        }

        /// <summary>
        /// 是否是可以直接赋值的类型
        /// </summary>
        /// <param name="propertyType">属性类型</param>
        /// <returns></returns>
        private static bool IsValueStruct(Type propertyType)
        {
            //bool result;

            //return propertyType == typeof(short) || propertyType == typeof(int) || propertyType == typeof(long) ||
            //    propertyType == typeof(ushort) || propertyType == typeof(uint) || propertyType == typeof(ulong) ||
            //    propertyType == typeof(float) || propertyType == typeof(double) ||
            //    propertyType == typeof(bool) || propertyType == typeof(string) || propertyType == typeof(char);

            //Primitive 类型： Boolean、Byte、SByte、Int16、UInt16、Int32、UInt32、Int64、UInt64、Char、Double、Single
            return propertyType.IsPrimitive || propertyType == typeof(string);

        }
    }

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