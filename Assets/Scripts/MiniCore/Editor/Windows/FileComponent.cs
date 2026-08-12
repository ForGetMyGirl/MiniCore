using MiniCore;
using MiniCore.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using UnityEngine;

namespace MiniCore.Core
{
    public class FileComponent : AComponent
    {
        /// <summary>
        /// 选择文件位置并以 JSON 格式保存数据。
        /// </summary>
        /// <param name="filter">Windows 文件筛选器。</param>
        /// <param name="defExt">默认扩展名。</param>
        /// <param name="fileProfileName">建议的文件名。</param>
        /// <param name="data">待保存数据。</param>
        public void SelectAndSaveFile(string filter, string defExt, string fileProfileName, object data)
        {
            string path = SaveFile(filter, defExt, fileProfileName);
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            File.WriteAllText(path, JsonConvert.SerializeObject(data, Formatting.Indented));
        }

        /// <summary>
        /// 选择 JSON 文件并反序列化为目标类型。
        /// </summary>
        /// <typeparam name="T">目标数据类型。</typeparam>
        /// <param name="filter">Windows 文件筛选器。</param>
        /// <param name="defExt">默认扩展名。</param>
        /// <returns>读取的对象；用户取消时返回默认值。</returns>
        public T SelectAndReadFile<T>(string filter, string defExt)
        {
            string path = OpenFile(filter, defExt);
            if (string.IsNullOrWhiteSpace(path))
            {
                return default;
            }

            return JsonConvert.DeserializeObject<T>(File.ReadAllText(path));
        }

        public string OpenFile(string filter)
        {
            OpenFileDialog fileSetting = new OpenFileDialog();
            fileSetting.structSize = Marshal.SizeOf(fileSetting);
            fileSetting.filter = filter;
            fileSetting.file = new string(new char[256]);
            fileSetting.maxFile = fileSetting.file.Length;
            fileSetting.fileTitle = new string(new char[64]);
            fileSetting.maxFileTitle = fileSetting.fileTitle.Length;
            fileSetting.initialDir = Application.dataPath.Replace("/", "\\");
            fileSetting.title = "选择文件";
            fileSetting.defExt = "dat";
            fileSetting.flags = 0x00080000 | 0x00001000 | 0x00000800 | 0x00000200 | 0x00000008;
            if (DialogFileHelper.GetOpenFileName(fileSetting))
            {
                return fileSetting.file;
            }
            return "";
        }

        public string GetDirPath()
        {
            OpenDirDialog ofn2 = new OpenDirDialog();
            ofn2.pszDisplayName = new string(new char[2048]);
            ofn2.lpszTitle = "选择保存路径";
            IntPtr pidlPtr = DialogFileHelper.SHBrowseForFolder(ofn2);

            char[] charArray = new char[2048];
            for (int i = 0; i < 2048; i++)
            {
                charArray[i] = '\0';
            }

            DialogFileHelper.SHGetPathFromIDList(pidlPtr, charArray);
            string fullDirPath = new string(charArray);
            fullDirPath = fullDirPath.Substring(0, fullDirPath.IndexOf('\0'));

            return fullDirPath;
        }

        /// <summary>
        /// 打开 Windows 浏览器选择目录。
        /// </summary>
        public string GetPathFromWindowsExplorer(string dialogtitle = "选择保存路径")
        {
            try
            {
                OpenDirDialog ofn2 = new OpenDirDialog();
                ofn2.pszDisplayName = new string(new char[2048]);
                ofn2.lpszTitle = dialogtitle;
                ofn2.ulFlags = 0x00000040;
                IntPtr pidlPtr = DialogFileHelper.SHBrowseForFolder(ofn2);

                char[] charArray = new char[2048];
                for (int i = 0; i < 2048; i++)
                {
                    charArray[i] = '\0';
                }

                DialogFileHelper.SHGetPathFromIDList(pidlPtr, charArray);
                string res = new string(charArray);
                res = res.Substring(0, res.IndexOf('\0'));
                return res;
            }
            catch (Exception e)
            {
                LogSwitch.Error(e.ToString());
            }

            return string.Empty;
        }

        /// <summary>
        /// 保存文件。
        /// </summary>
        public string SaveFile(string filter, string defExt, string fileProfileName)
        {
            OpenFileDialog dialogFile = new OpenFileDialog();
            dialogFile.structSize = Marshal.SizeOf(dialogFile);
            dialogFile.filter = filter;
            dialogFile.file = fileProfileName;
            dialogFile.maxFile = 256;
            dialogFile.fileTitle = new string(new char[64]);
            dialogFile.maxFileTitle = dialogFile.fileTitle.Length;
            dialogFile.initialDir = Application.dataPath.Replace("/", "\\");
            dialogFile.title = "保存文件";
            dialogFile.defExt = defExt;
            dialogFile.flags = 0x00080000 | 0x00001000 | 0x00000800 | 0x00000200 | 0x00000008;
            if (DialogFileHelper.GetSaveFileName(dialogFile))
            {
                string filepath = dialogFile.file;
                LogSwitch.Info("Path :" + filepath);
                return filepath;
            }
            return "";
        }

        /// <summary>
        /// 打开文件。
        /// </summary>
        public string OpenFile(string filter, string defExt)
        {
            OpenFileDialog pth = new OpenFileDialog();
            pth.structSize = Marshal.SizeOf(pth);
            pth.filter = filter;
            pth.file = new string(new char[256]);
            pth.maxFile = pth.file.Length;
            pth.fileTitle = new string(new char[64]);
            pth.maxFileTitle = pth.fileTitle.Length;
            pth.initialDir = Application.dataPath.Replace("/", "\\");
            pth.title = "打开文件";
            pth.defExt = defExt;
            pth.flags = 0x00080000 | 0x00001000 | 0x00000800 | 0x00000200 | 0x00000008;
            if (DialogFileHelper.GetOpenFileName(pth))
            {
                string filepath = pth.file;
                return filepath;
            }
            return "";
        }
    }
}
