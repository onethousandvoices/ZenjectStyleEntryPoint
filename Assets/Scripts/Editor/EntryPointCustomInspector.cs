using BaseTemplate;
using Controllers;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    /// <summary>
    /// If class ControllersQueue doesn't exist and compiler throws an error
    /// please use Tools/DIContainer/Create Queue to recreate this class.
    /// </summary>
    [CustomEditor(typeof(EntryPoint))]
    public class EntryPointCustomInspector : UnityEditor.Editor
    {
        private bool _showQueue;
        
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Show execution order"))
                _showQueue = !_showQueue;
            
            if (!_showQueue)
                return;

            GUILayout.Space(3f);
            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1f), new(1f, 0f, 0f, 1));
            GUILayout.Space(5f);
            
            var headerStyle = new GUIStyle { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.BoldAndItalic, fontSize = 17, normal = { textColor = Color.white} };
            var contentStyle = new GUIStyle { alignment = TextAnchor.MiddleLeft, normal = { textColor = Color.gray } };
            
            EditorGUILayout.LabelField("Current execution order", headerStyle);
            GUILayout.Space(3f);

            for (int i = 0; i < ControllersQueue.CurrentQueue.Length; i++)
                EditorGUILayout.LabelField($"{i + 1}. {ControllersQueue.CurrentQueue[i].Name}", contentStyle);

            GUILayout.Space(3f);
            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1f), new(1f, 0f, 0f, 1));
            GUILayout.Space(5f);
        }
    }
}