using MiniCore.Model;
using UnityEditor;

namespace MiniCore.EditorTools
{
    [CustomEditor(typeof(MouseInput))]
    public class MouseInputEditor : Editor
    {
        MouseInput mouseInput;

        void OnEnable()
        {
            mouseInput = target as MouseInput;
        }


        public override void OnInspectorGUI()
        {
            //设置垂直方向布局
            EditorGUILayout.BeginVertical();

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            //绘制MouseInput的基本信息
            //EditorGUILayout.LabelField("拖拽检测");
            mouseInput.mouseMoveEnable = EditorGUILayout.Toggle("开启拖拽检测", mouseInput.mouseMoveEnable);
            if (mouseInput.mouseMoveEnable)
            {
                //开启了移动检测才显示移动检测的内容
                //鼠标移动灵敏度
                mouseInput.mouseMoveSensitive = EditorGUILayout.FloatField("    移动灵敏度", mouseInput.mouseMoveSensitive);
                mouseInput.moveLerpDamp = EditorGUILayout.Slider("    移动速度下降缓动率", mouseInput.moveLerpDamp, 0, 1);
                mouseInput.moveReachedValue = EditorGUILayout.FloatField("    移动临界值", mouseInput.moveReachedValue);
            }

            EditorGUILayout.LabelField("----------------------------------------");
            //EditorGUILayout.LabelField("滚轮检测");
            mouseInput.mouseScrollEnable = EditorGUILayout.Toggle("开启滚轮检测", mouseInput.mouseScrollEnable);
            if (mouseInput.mouseScrollEnable)
            {
                //开启了移动检测才显示移动检测的内容
                //鼠标移动灵敏度
                mouseInput.scrollSensitive = EditorGUILayout.FloatField("    滚轮灵敏度", mouseInput.scrollSensitive);
                mouseInput.scrollLerpDamp = EditorGUILayout.Slider("    滚动速度下降缓动率", mouseInput.scrollLerpDamp, 0, 1);
                mouseInput.scrollReachedValue = EditorGUILayout.FloatField("    滚动临界值", mouseInput.scrollReachedValue);
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("以上信息修改完成后会自动保存数据，如果是在预制体中修改，需要对预制体进行重新保存才会生效", MessageType.Info);
            EditorGUILayout.EndVertical();

        }


    }

}