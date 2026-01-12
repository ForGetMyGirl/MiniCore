using MiniCore.Model;
using UnityEditor;
namespace MiniCore.EditorTools
{
    [CustomEditor(typeof(TouchInput))]
    public class TouchInputEditor : Editor
    {

        TouchInput touchInput;
        private void OnEnable()
        {
            touchInput = target as TouchInput;
        }

        public override void OnInspectorGUI()
        {

            EditorGUILayout.BeginVertical();

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            //绘制基本信息

            touchInput.touchMoveEnable = EditorGUILayout.Toggle("开启单指移动检测", touchInput.touchMoveEnable);
            if (touchInput.touchMoveEnable) { 
                //移动灵敏度
                touchInput.touchMoveSensitive = EditorGUILayout.FloatField("    移动灵敏度", touchInput.touchMoveSensitive);
                touchInput.moveLerpDamp = EditorGUILayout.Slider("    移动速度下降缓动率", touchInput.moveLerpDamp, 0f, 1f);
                touchInput.moveReachedValue = EditorGUILayout.FloatField("    移动临界值", touchInput.moveReachedValue);
            }

            EditorGUILayout.LabelField("----------------------------------------");

            touchInput.touchZoomEnable = EditorGUILayout.Toggle("开启双指缩放检测",touchInput.touchZoomEnable);
            if(touchInput.touchZoomEnable)
            {
                touchInput.touchZoomSensitive = EditorGUILayout.FloatField("    缩放灵敏度", touchInput.touchZoomSensitive);
                touchInput.touchZoomDamp = EditorGUILayout.Slider("    缩放速度下降缓动率", touchInput.touchZoomDamp, 0f, 1f);  
                touchInput.touchZoomReachedValue =  EditorGUILayout.FloatField("    缩放临界值", touchInput.touchZoomReachedValue);
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("以上信息修改完成后会自动保存数据，如果是在预制体中修改，需要对预制体进行重新保存才会生效", MessageType.Info);
            EditorGUILayout.EndVertical();
        }
    }

}