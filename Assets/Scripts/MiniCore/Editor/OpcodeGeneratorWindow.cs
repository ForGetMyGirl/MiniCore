using UnityEditor;
using UnityEngine;

namespace MiniCore.EditorTools
{
    /// <summary>
    /// 显示 opcode 稳定清单的手动同步与校验入口。
    /// </summary>
    public class OpcodeGeneratorWindow : EditorWindow
    {
        #region Private 私有成员

        private Vector2 scroll; // 生成与校验日志的滚动位置。
        private string log = string.Empty; // 最近一次操作的结果日志。

        /// <summary>
        /// 打开 opcode 手动同步与校验窗口。
        /// </summary>
        [MenuItem("MiniCore/Opcode/Generate (HotUpdate)", priority = 2100)]
        private static void Open()
        {
            GetWindow<OpcodeGeneratorWindow>("Opcode Generator").Show();
        }

        /// <summary>
        /// 执行 OnGUI 相关处理。
        /// </summary>
        private void OnGUI()
        {
            GUILayout.Label("HotUpdate Opcode Generator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Opcode 由稳定清单维护：新增协议仅追加编号，已删除协议保留编号。", MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("同步"))
            {
                OpcodeRegistryGenerator.Synchronize(true, out log);
            }

            if (GUILayout.Button("校验"))
            {
                log = OpcodeRegistryGenerator.Validate(out string error) ? "Opcode 清单与生成映射校验通过。" : error;
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(8);
            GUILayout.Label("Log", EditorStyles.boldLabel);
            scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(240));
            EditorGUILayout.TextArea(log, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        #endregion
    }
}
