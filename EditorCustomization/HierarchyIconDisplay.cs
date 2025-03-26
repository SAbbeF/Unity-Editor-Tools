using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[InitializeOnLoad]
public class HierarchyIconDisplay
{
    // Lists to control component selection
    private static readonly List<Type> ComponentsToIgnore = new List<Type>
    {
        typeof(CanvasRenderer),
        typeof(RectTransform)
        // Add more types you want to ignore
    };

    private static readonly List<Type> ComponentPriorityList = new List<Type>
    {
        typeof(Camera),
        typeof(Light),
        typeof(Animator),
        // Add your preferred component types in order of priority
    };

    private const string PREF_HIERARCHY_ICON_DISPLAY_PREFAB = "HierarchyIconDisplay_PrefabEnabled";
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

        // Check if prefab icon display is enabled
        bool displayPrefabIcons = EditorPrefs.GetBool(PREF_HIERARCHY_ICON_DISPLAY_PREFAB, false);

        // Determine if this is a prefab instance
        bool isPrefabInstance = PrefabUtility.GetCorrespondingObjectFromSource(obj) != null;

        // If it's a prefab instance and prefab icons are disabled, return early
        if (isPrefabInstance && !displayPrefabIcons)
            return;

        Component[] allComponents = obj.GetComponents<Component>();

        // Filter out ignored components
        Component[] filteredComponents = allComponents
        .Where(c => c != null && !ComponentsToIgnore.Contains(c.GetType()))
            .ToArray();

        // If all components are ignored, reset to full component list
        if (filteredComponents.Length == 0)
        {
            filteredComponents = allComponents;
        }

        if (filteredComponents == null || filteredComponents.Length == 0)
            return;

        // Choose component based on priority
        Component selectedComponent = ChooseComponentForIcon(filteredComponents);
        if (selectedComponent == null)
            return;

        Type type = selectedComponent.GetType();
        GUIContent content = EditorGUIUtility.ObjectContent(selectedComponent, type);
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

    private static Component ChooseComponentForIcon(Component[] components)
    {
        // First, check if any component is in the priority list
        foreach (var priorityType in ComponentPriorityList)
        {
            var matchingComponent = components.FirstOrDefault(c => c.GetType() == priorityType);
            if (matchingComponent != null)
                return matchingComponent;
        }

        // If no priority component found, use 2nd component if [length > 1] otherwise use transform
        return components.Length > 1 ? components[1] : components[0];
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