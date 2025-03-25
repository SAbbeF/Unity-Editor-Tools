using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEditor.SceneManagement;

[Overlay(typeof(SceneView), "Scene Selection")]
[Icon("BuildSettings.Editor")]
public class SceneSelectionOverlay : ToolbarOverlay
{
    public SceneSelectionOverlay() : base(SceneDropdownToggle.Id, DefaultStartSceneToggle.Id) { }

    [EditorToolbarElement(Id, typeof(SceneView))]
    class SceneDropdownToggle : EditorToolbarDropdownToggle, IAccessContainerWindow
    {
        public const string Id = "SceneSelectionOverlay/SceneDropdownToggle";
        public EditorWindow containerWindow { get; set; }

        SceneDropdownToggle()
        {
            text = "Scenes";
            tooltip = "Select a scene to load";
            icon = EditorGUIUtility.FindTexture("BuildSettings.Editor");
            dropdownClicked += ShowSceneMenu;
        }

        private void ShowSceneMenu()
        {
            // Retrieve all scene paths with filtering
            string[] sceneGuids = AssetDatabase.FindAssets("t:scene", new[] { "Assets" });

            // Filter and process scene paths
            var validScenes = sceneGuids
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .Where(IsValidScene)
                .ToArray();

            string[] scenePaths = validScenes.ToArray();
            string[] sceneNames = scenePaths
                .Select(path => Path.GetFileNameWithoutExtension(path))
                .ToArray();

            // Sort scenes alphabetically
            System.Array.Sort(sceneNames, scenePaths);

            // Create custom scene selection popup
            ShowSceneSelectionPopup(scenePaths, sceneNames);
        }

        // Validate scene for selection
        private bool IsValidScene(string scenePath)
        {
            // Exclude scenes in read-only packages or with specific prefixes
            if (scenePath.Contains("/Packages/") ||
                scenePath.Contains("/PackageCache/") ||
                Path.GetFileNameWithoutExtension(scenePath).StartsWith("_") ||
                Path.GetFileNameWithoutExtension(scenePath).Contains("SerializationTests") ||
                Path.GetFileNameWithoutExtension(scenePath).Contains("Template"))
            {
                return false;
            }

            return true;
        }

        public static void ShowSceneSelectionPopup(string[] scenePaths, string[] sceneNames)
        {
            SceneSelectionPopup window = ScriptableObject.CreateInstance<SceneSelectionPopup>();
            window._originalScenePaths = scenePaths;
            window._originalSceneNames = sceneNames;
            window._currentScenePaths = scenePaths;
            window._currentSceneNames = sceneNames;

            // Create the popup window
            window.ShowAsDropDown(
                new Rect(Event.current.mousePosition.x, Event.current.mousePosition.y, 300, 0),
                new Vector2(300, 350)
            );
        }
    }

    [EditorToolbarElement(Id, typeof(SceneView))]
    class DefaultStartSceneToggle : EditorToolbarToggle, IAccessContainerWindow
    {
        public const string Id = "SceneSelectionOverlay/DefaultStartSceneToggle";
        public EditorWindow containerWindow { get; set; }

        DefaultStartSceneToggle()
        {
            text = "Start Scene";
            tooltip = "Enable/Disable Default Starting Scene";
            icon = EditorGUIUtility.FindTexture("PlayButton");

            // Setup change handler
            this.RegisterValueChangedCallback(evt => OnToggleChanged(evt.newValue));
        }

        private void OnToggleChanged(bool newValue)
        {
            // If disabled, clear the start scene
            if (!newValue)
            {
                EditorSceneManager.playModeStartScene = null;
            }
            else
            {
                // Open scene selection if enabling
                ShowStartSceneSelection();
            }
        }

        private void ShowStartSceneSelection()
        {
            // Retrieve all scene paths with filtering
            string[] sceneGuids = AssetDatabase.FindAssets("t:scene", new[] { "Assets" });

            // Filter and process scene paths
            var validScenes = sceneGuids
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .Where(scenePath =>
                    !scenePath.Contains("/Packages/") &&
                    !scenePath.Contains("/PackageCache/") &&
                    !Path.GetFileNameWithoutExtension(scenePath).StartsWith("_") &&
                    !Path.GetFileNameWithoutExtension(scenePath).Contains("SerializationTests") &&
                    !Path.GetFileNameWithoutExtension(scenePath).Contains("Template"))
                .ToArray();

            string[] scenePaths = validScenes.ToArray();
            string[] sceneNames = scenePaths
                .Select(path => Path.GetFileNameWithoutExtension(path))
                .ToArray();

            // Sort scenes alphabetically
            System.Array.Sort(sceneNames, scenePaths);

            // Create a popup to select the start scene
            SceneDropdownToggle.ShowSceneSelectionPopup(scenePaths, sceneNames);
        }
    }

    // Popup for selecting scenes
    class SceneSelectionPopup : EditorWindow
    {
        internal string[] _originalScenePaths;
        internal string[] _originalSceneNames;
        internal string[] _currentScenePaths;
        internal string[] _currentSceneNames;
        private Vector2 _scrollPosition;
        private string _searchText = "";

        private void OnGUI()
        {
            // Search bar
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // Search text field
            EditorGUI.BeginChangeCheck();
            _searchText = EditorGUILayout.TextField(_searchText, EditorStyles.toolbarSearchField);
            if (EditorGUI.EndChangeCheck())
            {
                // Filter scenes based on search text
                FilterScenes();
            }

            EditorGUILayout.EndHorizontal();

            // Scenes list
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Width(290), GUILayout.Height(280));

            Scene currentScene = EditorSceneManager.GetActiveScene();
            SceneAsset currentStartScene = EditorSceneManager.playModeStartScene;

            for (int i = 0; i < _currentSceneNames.Length; i++)
            {
                bool isCurrentScene = currentScene.name == _currentSceneNames[i];
                bool isCurrentStartScene = currentStartScene != null && currentStartScene.name == _currentSceneNames[i];

                EditorGUI.BeginDisabledGroup(isCurrentScene || isCurrentStartScene);

                GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
                buttonStyle.alignment = TextAnchor.MiddleLeft;

                if (GUILayout.Button(_currentSceneNames[i], buttonStyle, GUILayout.Width(270)))
                {
                    // Determine the context of the popup
                    bool isStartScenePopup = currentStartScene == null;

                    if (!isStartScenePopup)
                    {
                        // Open scene for SceneDropdownToggle
                        OpenSceneWithSaveCheck(currentScene, _currentScenePaths[i]);
                    }
                    else
                    {
                        // Set as start scene for DefaultStartSceneToggle
                        SceneAsset selectedScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(_currentScenePaths[i]);
                        EditorSceneManager.playModeStartScene = selectedScene;
                    }

                    Close(); // Close the popup after selection
                }

                EditorGUI.EndDisabledGroup();
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        // Static method to handle scene opening
        private static void OpenSceneWithSaveCheck(Scene currentScene, string path)
        {
            if (currentScene.isDirty)
            {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    EditorSceneManager.OpenScene(path);
            }
            else
            {
                EditorSceneManager.OpenScene(path);
            }
        }

        private void FilterScenes()
        {
            if (string.IsNullOrWhiteSpace(_searchText))
            {
                // If search is empty, show all scenes
                _currentScenePaths = _originalScenePaths;
                _currentSceneNames = _originalSceneNames;
            }
            else
            {
                // Filter scenes that contain the search text (case-insensitive)
                var filteredScenes = _originalSceneNames
                    .Select((name, index) => new { Name = name, Path = _originalScenePaths[index] })
                    .Where(scene => scene.Name.IndexOf(_searchText, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToArray();

                _currentSceneNames = filteredScenes.Select(s => s.Name).ToArray();
                _currentScenePaths = filteredScenes.Select(s => s.Path).ToArray();
            }
        }
    }
}