using MiniCore;
using MiniCore.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace MiniCore.Core
{
    public class FileComponent : AComponent
    {
        public void SelectAndSaveFile(string filter, string defExt, string fileProfileName, object data)
        {
            string path = SaveFile(filter, defExt, fileProfileName);
            using (FileStream fsStream = new FileStream(path, FileMode.OpenOrCreate))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(fsStream, data);
            }
        }

        public T SelectAndReadFile<T>(string filter, string defExt)
        {
            string path = OpenFile(filter, defExt);
            using (FileStream fsStream = new FileStream(path, FileMode.Open))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                object data = formatter.Deserialize(fsStream);
                return (T)data;
            }
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
                EventCenter.Broadcast(GameEvent.LogError, e);
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
                EventCenter.Broadcast(GameEvent.LogInfo, "Path :" + filepath);
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

    public class DialogFileHelper
    {
        [DllImport("Comdlg32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool GetOpenFileName([In, Out] OpenFileDialog ofn);

        [DllImport("Comdlg32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool GetSaveFileName([In, Out] OpenFileDialog ofn);

        [DllImport("shell32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr SHBrowseForFolder([In, Out] OpenDirDialog ofn);

        [DllImport("shell32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool SHGetPathFromIDList([In] IntPtr pidl, [In, Out] char[] fileName);
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public class OpenDirDialog
    {
        public IntPtr hwndOwner = IntPtr.Zero;
        public IntPtr pidlRoot = IntPtr.Zero;
        public string pszDisplayName = null;
        public string lpszTitle = null;
        public uint ulFlags = 0;
        public IntPtr lpfn = IntPtr.Zero;
        public IntPtr lParam = IntPtr.Zero;
        public int iImage = 0;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public class OpenFileDialog
    {
        public int structSize = 0;
        public IntPtr dlgOwner = IntPtr.Zero;
        public IntPtr instance = IntPtr.Zero;
        public string filter = null;
        public string customFilter = null;
        public int maxCustFilter = 0;
        public int filterIndex = 0;
        public string file = null;
        public int maxFile = 0;
        public string fileTitle = null;
        public int maxFileTitle = 0;
        public string initialDir = null;
        public string title = null;
        public int flags = 0;
        public short fileOffset = 0;
        public short fileExtension = 0;
        public string defExt = null;
        public IntPtr custData = IntPtr.Zero;
        public IntPtr hook = IntPtr.Zero;
        public string templateName = null;
        public IntPtr reservedPtr = IntPtr.Zero;
        public int reservedInt = 0;
        public int flagsEx = 0;
    }
}
