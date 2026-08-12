using MiniCore.Model;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace MiniCore.EditorTools
{
    [CustomEditor(typeof(ReferenceCollector))]
    public class ReferenceCollectorEditor : Editor
    {
        private ReferenceCollector referenceCollector;
        //private SerializedProperty manualInteractives;

        private SerializedProperty referenceDatas;
        private SerializedProperty referenceData;

        //private SerializedProperty manualName;
        //private SerializedProperty manualInteractive;

        private int i = 0;

        private void OnEnable()
        {
            referenceCollector = (ReferenceCollector)target;
            //manualInteractives = serializedObject.FindProperty("manualInteractives");
            referenceDatas = serializedObject.FindProperty("referenceDatas");
            referenceData = serializedObject.FindProperty("referenceData");

            //manualInteractive = serializedObject.FindProperty("manualInteractive");
            //manualName = serializedObject.FindProperty("manualName");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.BeginVertical(GUI.skin.box);

            var delList = new List<int>();
            SerializedProperty property;
            for (int i = 0; i < referenceDatas.arraySize; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("名称：", GUILayout.Width(40));
                property = referenceDatas.GetArrayElementAtIndex(i).FindPropertyRelative("key");
                property.stringValue = EditorGUILayout.TextField(/*new GUIContent("名称："),*/ property.stringValue, GUILayout.Width(120));
                EditorGUILayout.LabelField("对象：", GUILayout.Width(40));
                property = referenceDatas.GetArrayElementAtIndex(i).FindPropertyRelative("value");
                property.objectReferenceValue = EditorGUILayout.ObjectField(property.objectReferenceValue, typeof(Object), true);

                if (GUILayout.Button("X", GUILayout.Width(60)))
                {
                    //将待删除元素添加到list中
                    delList.Add(i);
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal(GUI.skin.box);

            EditorGUILayout.LabelField("名称：", GUILayout.Width(40));
            property = referenceData.FindPropertyRelative("key");
            property.stringValue = EditorGUILayout.TextField(/*new GUIContent("名称："),*/ property.stringValue, GUILayout.Width(120));

            //EditorGUILayout.Space();
            EditorGUILayout.LabelField("对象：", GUILayout.Width(40));
            property = referenceData.FindPropertyRelative("value");
            property.objectReferenceValue = EditorGUILayout.ObjectField(/*"对象：",*/property.objectReferenceValue, typeof(Object), true /*,GUILayout.Width(120)*/);

            if (GUILayout.Button("插入", GUILayout.Width(60)))
            {
                referenceCollector.Register();

            }

            EditorGUILayout.EndHorizontal();

            //删除list
            for (int i = delList.Count - 1; i >= 0; i--)
            {
                referenceCollector.Unregister(delList[i]);
                //manualInteractiveDatas.DeleteArrayElementAtIndex(delList[i]);

            }

            serializedObject.ApplyModifiedProperties();
        }
    }

}