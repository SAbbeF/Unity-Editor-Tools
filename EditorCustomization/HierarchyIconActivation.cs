using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace OptionalTools.Editor
{
    [InitializeOnLoad]
    public static class HierarchyIconActivation
    {
        public static void OnHierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
        {
            GameObject obj = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            if (obj == null)
                return;

            Rect rect = new Rect(selectionRect.x, selectionRect.y, 15f, selectionRect.height);
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 &&
                rect.Contains(Event.current.mousePosition))
            {
                if (!Application.isPlaying)
                    Undo.RecordObject(obj, "Changing active state of object");

                obj.SetActive(!obj.activeSelf);

                if (!Application.isPlaying)
                    EditorSceneManager.MarkSceneDirty(obj.scene);

                Event.current.Use();
            }
        }

        static HierarchyIconActivation()
        {
            if (EditorPrefs.GetBool("HierarchyIconActivation_Enabled", false))
            {
                EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyWindowItemOnGUI;
            }
        }
    }
}