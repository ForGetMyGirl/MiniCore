using System;
using System.Collections.Generic;
using System.Threading;
using MiniCore.Threading;

namespace MiniCore.Service
{

    /// <summary>
    /// 提供本地持久化数据根目录与受控子目录的系统服务契约。
    /// 存档和本地运行数据应通过该服务取得路径，不能硬编码框架目录名称。
    /// </summary>
    public interface IStoragePathService : IAppService
    {
        /// <summary>
        /// 获取当前项目解析后的本地持久化根目录。
        /// </summary>
        string RootPath { get; }

        /// <summary>
        /// 获取并确保指定用途的一级子目录存在。
        /// </summary>
        /// <param name="directoryName">不包含路径分隔符的用途目录名称。</param>
        /// <returns>已创建或已存在的子目录绝对路径。</returns>
        string GetDirectory(string directoryName);
    }
}
