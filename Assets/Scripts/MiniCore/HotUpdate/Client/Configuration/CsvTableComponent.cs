using Cysharp.Threading.Tasks;
using MiniCore.Model;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
namespace MiniCore.Core
{
    public class CsvTableComponent : AComponent
    {
        private readonly Dictionary<string, IExcelConfig> csvTables = new Dictionary<string, IExcelConfig>();

        private readonly Dictionary<string, Dictionary<long, ICsvTable>> csvTableDataDic = new Dictionary<string, Dictionary<long, ICsvTable>>();

        /// <summary>
        /// 获取Csv表中的所有数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        /// <returns></returns>
        public async UniTask<CsvTable<T>> GetCsvTable<T>(string path) where T : class, ICsvTable, new()
        {

            if (!csvTables.TryGetValue(path, out var table))
            {
                table = await ExcelTool.LoadCsvFileAsync<T>(path);
                csvTables.Add(path, table);
            }
            return table as CsvTable<T>;
        }


        /// <summary>
        /// 回收Csv表数据，不会回收通过GetTableDataById<T>方法获得的数据
        /// </summary>
        /// <param name="path"></param>
        public void CollectCsvTable(string path)
        {
            if (csvTables.TryGetValue(path, out IExcelConfig table))
            {
                table = null;
                csvTables.Remove(path);
            }
        }

        /// <summary>
        /// 通过Id获取表中的数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public async UniTask<T> GetTableDataById<T>(string path, int id) where T : class, ICsvTable, new()
        {

            //if (!csvTableDataDic.ContainsKey(path))
            //{
            await PreLoadSingleTableData<T>(path);

            //}
            return csvTableDataDic[path][id] as T;

        }

        /// <summary>
        /// 预加载单表结构的数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        /// <returns></returns>
        public async UniTask PreLoadSingleTableData<T>(string path) where T : class, ICsvTable, new()
        {
            if (!csvTableDataDic.TryGetValue(path, out Dictionary<long, ICsvTable> myData))
            {
                CsvTable<T> tableEntity = await ExcelTool.LoadCsvFileAsync<T>(path);
                csvTables[path] = tableEntity;  //顺便将数据全部存到csvTables中

                myData = new Dictionary<long, ICsvTable>();
                for (int i = 0; i < tableEntity.DataCount; i++)
                {
                    T data = tableEntity.RawDatas[i];
                    myData.Add(data.Id, data);
                }

                csvTableDataDic.Add(path, myData);
            }

        }

        /// <summary>
        /// 仅释放通过GetTableDataById<T>方法获得的数据表
        /// </summary>
        /// <param name="path"></param>
        public void CollectCsvTableDatas(string path)
        {
            if (csvTableDataDic.TryGetValue(path, out var myData))
            {
                myData = null;
                csvTableDataDic.Remove(path);
            }

        }


    }

}