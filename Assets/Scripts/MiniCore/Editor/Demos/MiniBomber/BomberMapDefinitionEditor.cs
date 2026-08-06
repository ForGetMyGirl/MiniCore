using MiniCore.Demo.MiniBomber;
using UnityEditor;
using UnityEngine;

namespace MiniCore.EditorTools.Demos.MiniBomber
{
    /// <summary>
    /// 在 Inspector 中以可点击网格绘制 MiniBomber 地图格类型。
    /// </summary>
    [CustomEditor(typeof(BomberMapDefinition))]
    public sealed class BomberMapDefinitionEditor : UnityEditor.Editor
    {
        #region Private 私有成员

        private MiniBomberCellType paintType = MiniBomberCellType.Breakable; // 当前画笔类型。

        #endregion

        #region Unity 生命周期函数

        /// <summary>
        /// 绘制默认字段、画笔选择和 17×13 可点击格子。
        /// </summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();
            BomberMapDefinition map = (BomberMapDefinition)target;
            EditorGUILayout.Space();
            paintType = (MiniBomberCellType)EditorGUILayout.EnumPopup("当前画笔", paintType);
            float buttonSize = Mathf.Clamp((EditorGUIUtility.currentViewWidth - 40f) / Mathf.Max(1, map.Width), 18f, 30f);
            for (int z = map.Height - 1; z >= 0; z--)
            {
                EditorGUILayout.BeginHorizontal();
                for (int x = 0; x < map.Width; x++)
                {
                    MiniBomberCellType cell = map.GetCell(x, z);
                    Color previous = GUI.backgroundColor;
                    GUI.backgroundColor = ResolveColor(cell);
                    if (GUILayout.Button(ResolveLabel(cell), GUILayout.Width(buttonSize), GUILayout.Height(buttonSize)))
                    {
                        Undo.RecordObject(map, "Paint MiniBomber Map Cell");
                        map.SetCell(x, z, paintType);
                        EditorUtility.SetDirty(map);
                    }

                    GUI.backgroundColor = previous;
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 获取地图格按钮颜色。
        /// </summary>
        /// <param name="type">格子类型。</param>
        /// <returns>按钮背景色。</returns>
        private static Color ResolveColor(MiniBomberCellType type)
        {
            switch (type)
            {
                case MiniBomberCellType.Solid:
                    return new Color(0.35f, 0.4f, 0.48f);
                case MiniBomberCellType.Breakable:
                    return new Color(0.75f, 0.48f, 0.2f);
                default:
                    return new Color(0.25f, 0.65f, 0.35f);
            }
        }

        /// <summary>
        /// 获取地图格紧凑按钮文本。
        /// </summary>
        /// <param name="type">格子类型。</param>
        /// <returns>单字符标签。</returns>
        private static string ResolveLabel(MiniBomberCellType type)
        {
            return type == MiniBomberCellType.Solid ? "墙" : type == MiniBomberCellType.Breakable ? "箱" : "路";
        }

        #endregion
    }
}
