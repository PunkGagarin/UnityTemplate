using UnityEditor;
using UnityEngine;

namespace _Project.Scripts.Utils.Editor
{
    [CustomEditor(typeof(ContentUi), true)]
    public class ContentUiEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // Рисуем стандартный инспектор
            DrawDefaultInspector();

            ContentUi ui = (ContentUi)target;

            GUILayout.Space(10);

            var content = new GUIContent(
                "Find Content",
                "Найти дочерний объект с именем 'Content' и присвоить его"
            );

            if (GUILayout.Button(content, GUILayout.Width(200),
                    GUILayout.Height(32)))
                FindAndAssignContent(ui);
        }

        private void FindAndAssignContent(ContentUi ui)
        {
            // Ищем дочерний объект с именем "Content"
            Transform contentTransform = ui.transform.Find("Content");

            if (contentTransform == null)
            {
                Debug.LogWarning($"[ContentUi] Child with name 'Content' not found on {ui.name}");
                return;
            }

            // Записываем изменение корректно для Undo
            Undo.RecordObject(ui, "Assign Content");

            SerializedObject so = new SerializedObject(ui);
            SerializedProperty contentProp = so.FindProperty("content");
            contentProp.objectReferenceValue = contentTransform.gameObject;
            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(ui);
        }
    }
}