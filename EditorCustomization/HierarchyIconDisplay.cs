using System;
using System.Linq;
using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public class HierarchyIconDisplay
{
    static bool _hierarchyHasFocus = false;
    static EditorWindow _hierarchyEditorWindow;

    public static void OnEditorUpdate()
    {
        if (_hierarchyEditorWindow == null)
            _hierarchyEditorWindow = EditorWindow.GetWindow(System.Type.GetType("UnityEditor.SceneHierarchyWindow,UnityEditor"));

        _hierarchyHasFocus = EditorWindow.focusedWindow != null &&
            EditorWindow.focusedWindow == _hierarchyEditorWindow;
    }

    public static void OnHierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
    {
        GameObject obj = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
        if (obj == null)
            return;

        Component[] components = obj.GetComponents<Component>();
        if (components == null || components.Length == 0)
            return;

        if (PrefabUtility.GetCorrespondingObjectFromSource(obj) != null)
            return;

        // Choose the component to use, second after transform if it has more than one
        Component component = components.Length > 1 ? components[1] : components[0];
        Type type = component.GetType();

        GUIContent content = EditorGUIUtility.ObjectContent(component, type);
        content.text = null;
        content.tooltip = type.Name;

        if (content.image == null)
            return;

        bool isSelected = Selection.instanceIDs.Contains(instanceID);
        bool isHovering = selectionRect.Contains(Event.current.mousePosition);

        Color color = UnityEditorBackgroundColor.Get(isSelected, isHovering, _hierarchyHasFocus);

        Rect backgroundRect = selectionRect;
        backgroundRect.width = 18.5f;

        EditorGUI.DrawRect(backgroundRect, color);
        EditorGUI.LabelField(selectionRect, content);
    }


    static HierarchyIconDisplay()
    {
        if (EditorPrefs.GetBool("HierarchyIconDisplay_Enabled", false))
        {
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyWindowItemOnGUI;
            EditorApplication.update += OnEditorUpdate;
        }
    }
}