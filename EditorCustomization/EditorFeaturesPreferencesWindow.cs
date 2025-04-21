using UnityEngine;
using UnityEditor;

namespace OptionalTools.Editor
{
    public class EditorFeaturesPreferencesWindow : EditorWindow
    {
        // Preference keys
        private const string PREF_HIERARCHY_ACTIVATION = "HierarchyIconActivation_Enabled";
        private const string PREF_HIERARCHY_ICON_DISPLAY = "HierarchyIconDisplay_Enabled";
        private const string PREF_HIERARCHY_ICON_DISPLAY_PREFAB = "HierarchyIconDisplay_PrefabEnabled";

        // Feature descriptions
        private static readonly FeatureInfo[] features = new FeatureInfo[]
        {
            new FeatureInfo
            {
                Name = "Hierarchy Click Activation",
                PrefKey = PREF_HIERARCHY_ACTIVATION,
                Description = "Allows quick activation/deactivation of GameObjects by clicking their icon in the Hierarchy window. " +
                              "When enabled, you can click the small area on the left side of a GameObject to toggle its active state " +
                              "without using the checkbox or context menu."
            },
            new FeatureInfo
            {
                Name = "Hierarchy Component Icon Display",
                PrefKey = PREF_HIERARCHY_ICON_DISPLAY,
                Description = "Displays an icon in the Hierarchy window representing the first (or second) non-Transform component " +
                              "of each GameObject. This provides a quick visual reference for the types of components attached to " +
                              "each object in your scene."
            },
            new FeatureInfo
            {
                Name = "Hierarchy Prefab Icon Display",
                PrefKey = PREF_HIERARCHY_ICON_DISPLAY_PREFAB,
                Description = "Enables component icons for Prefab instances in the Hierarchy window. When disabled, icons will not " +
                              "be shown for prefab objects."
            }
        };

        // Feature information structure
        private class FeatureInfo
        {
            public string Name;
            public string PrefKey;
            public string Description;
        }

        [MenuItem("Tools/Editor Features Preferences")]
        public static void ShowWindow()
        {
            GetWindow<EditorFeaturesPreferencesWindow>("Editor Features");
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Unity Editor Features", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            foreach (var feature in features)
            {
                DrawFeatureSection(feature);
            }

            EditorGUILayout.Space(20);
            if (GUILayout.Button("Save and Apply", GUILayout.Height(30)))
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        private void DrawFeatureSection(FeatureInfo feature)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            bool isEnabled = EditorPrefs.GetBool(feature.PrefKey, false);
            bool newEnabled = EditorGUILayout.Toggle(isEnabled, GUILayout.Width(20));

            if (newEnabled != isEnabled)
            {
                EditorPrefs.SetBool(feature.PrefKey, newEnabled);

                // Dynamically enable/disable the corresponding feature
                if (feature.PrefKey == PREF_HIERARCHY_ACTIVATION)
                {
                    if (newEnabled)
                        EditorApplication.hierarchyWindowItemOnGUI += HierarchyIconActivation.OnHierarchyWindowItemOnGUI;
                    else
                        EditorApplication.hierarchyWindowItemOnGUI -= HierarchyIconActivation.OnHierarchyWindowItemOnGUI;
                }
                else if (feature.PrefKey == PREF_HIERARCHY_ICON_DISPLAY)
                {
                    if (newEnabled)
                    {
                        EditorApplication.hierarchyWindowItemOnGUI += HierarchyIconDisplay.OnHierarchyWindowItemOnGUI;
                        EditorApplication.update += HierarchyIconDisplay.OnEditorUpdate;
                    }
                    else
                    {
                        EditorApplication.hierarchyWindowItemOnGUI -= HierarchyIconDisplay.OnHierarchyWindowItemOnGUI;
                        EditorApplication.update -= HierarchyIconDisplay.OnEditorUpdate;
                    }
                }
            }

            EditorGUILayout.LabelField(feature.Name, EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            // Description
            EditorGUILayout.LabelField(feature.Description, EditorStyles.wordWrappedLabel);

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
        }
    }
}