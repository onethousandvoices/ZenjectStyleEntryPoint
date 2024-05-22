using BaseTemplate;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomEditor(typeof(EntryPoint))]
    public class EntryPointCustomInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            GUILayout.Space(3f);
            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1f), new(1f, 0f, 0f, 1));
            GUILayout.Space(5f);
            
            var headerStyle = new GUIStyle { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.BoldAndItalic, fontSize = 17, normal = { textColor = Color.white} };
            var contentStyle = new GUIStyle { alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.gray } };
            
            EditorGUILayout.LabelField("Current execution order", headerStyle);
            GUILayout.Space(3f);

            for (int i = 0; i < ControllersQueueDisplay.CurrentQueue.Length; i++)
                EditorGUILayout.LabelField(ControllersQueueDisplay.CurrentQueue[i].Name, contentStyle);

            GUILayout.Space(3f);
            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1f), new(1f, 0f, 0f, 1));
            GUILayout.Space(5f);
            
            base.OnInspectorGUI();
        }
    }
}