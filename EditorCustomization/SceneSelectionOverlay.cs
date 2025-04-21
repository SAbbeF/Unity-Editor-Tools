using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

namespace OptionalTools.Editor
{
    [Overlay(typeof(SceneView), "Scene Selection")]
    [Icon("BuildSettings.Editor")]
    public class SceneSelectionOverlay : ToolbarOverlay
    {
        public SceneSelectionOverlay() : base(SceneDropdownToggle.Id, DefaultStartSceneToggle.Id) { }

        [EditorToolbarElement(Id, typeof(SceneView))]
        class SceneDropdownToggle : EditorToolbarDropdown, IAccessContainerWindow
        {
            public const string Id = "SceneSelectionOverlay/SceneDropdownToggle";
            public EditorWindow containerWindow { get; set; }

            SceneDropdownToggle()
            {
                text = "Scenes";
                tooltip = "Select a scene to load";
                icon = EditorGUIUtility.FindTexture("BuildSettings.Editor");
                clicked += ShowSceneMenu;
            }

            private void ShowSceneMenu()
            {
                string[] sceneGuids = AssetDatabase.FindAssets("t:scene", new[] { "Assets" });

                string[] validScenes = sceneGuids
                    .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                    .Where(IsValidScene)
                    .ToArray();

                string[] scenePaths = validScenes;
                string[] sceneNames = scenePaths
                    .Select(path => Path.GetFileNameWithoutExtension(path))
                    .ToArray();

                System.Array.Sort(sceneNames, scenePaths);

                ShowSceneSelectionWindow(scenePaths, sceneNames, false);
            }

            private bool IsValidScene(string scenePath)
            {
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

            public static void ShowSceneSelectionWindow(string[] scenePaths, string[] sceneNames, bool isStartSceneMode)
            {
                if (scenePaths == null || sceneNames == null)
                {
                    Debug.LogError("Scene paths or names are null!");
                    return;
                }

                SceneSelectionWindow window = EditorWindow.GetWindow<SceneSelectionWindow>(true,
                    isStartSceneMode ? "Select Start Scene" : "Select Scene");
                window._originalScenePaths = scenePaths;
                window._originalSceneNames = sceneNames;
                window._currentScenePaths = scenePaths;
                window._currentSceneNames = sceneNames;
                window._isStartSceneMode = isStartSceneMode;

                window.minSize = new Vector2(300, 350);
                window.maxSize = new Vector2(800, 600);
                window.Show();
                window.Focus();
            }
        }

        [EditorToolbarElement(Id, typeof(SceneView))]
        class DefaultStartSceneToggle : EditorToolbarButton, IAccessContainerWindow
        {
            public const string Id = "SceneSelectionOverlay/DefaultStartSceneToggle";
            public EditorWindow containerWindow { get; set; }

            private bool _hasStartScene = false;
            public static event System.Action OnStartSceneChanged;

            SceneAsset _currentStartScene;

            DefaultStartSceneToggle()
            {
                UpdateButtonContent();
                clicked += OnButtonClicked;
                EditorApplication.playModeStateChanged += _ => UpdateButtonContent();
                OnStartSceneChanged += UpdateButtonContent;
                EditorApplication.update += CheckForStartSceneChanges;
            }

            private void CheckForStartSceneChanges()
            {
                SceneAsset startScene = EditorSceneManager.playModeStartScene;
                if ((_currentStartScene == null && startScene != null) ||
                    (_currentStartScene != null && startScene == null) ||
                    (_currentStartScene != null && startScene != null && _currentStartScene.name != startScene.name))
                {
                    _currentStartScene = startScene;
                    UpdateButtonContent();
                }
            }

            public void UpdateButtonContent()
            {
                SceneAsset startScene = EditorSceneManager.playModeStartScene;
                _currentStartScene = startScene;
                _hasStartScene = startScene != null;

                if (_hasStartScene)
                {
                    text = $"Start: {startScene.name}";
                    tooltip = $"Default Starting Scene: {startScene.name}";
                }
                else
                {
                    text = "Set Start Scene";
                    tooltip = "Set Default Starting Scene";
                }

                icon = EditorGUIUtility.FindTexture("PlayButton");

                containerWindow?.Repaint();
            }

            private void OnButtonClicked()
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Set Start Scene..."), false, () => {
                    EditorApplication.delayCall += ShowStartSceneSelection;
                });

                if (_hasStartScene)
                {
                    menu.AddItem(new GUIContent("Clear Start Scene"), false, () => {
                        EditorSceneManager.playModeStartScene = null;
                        UpdateButtonContent();
                        BroadcastStartSceneChange();
                    });
                }
                else
                {
                    menu.AddDisabledItem(new GUIContent("Clear Start Scene"));
                }

                menu.ShowAsContext();
            }

            public static void BroadcastStartSceneChange()
            {
                OnStartSceneChanged?.Invoke();
            }

            private void ShowStartSceneSelection()
            {
                try
                {
                    string[] sceneGuids = AssetDatabase.FindAssets("t:scene", new[] { "Assets" });
                    if (sceneGuids == null || sceneGuids.Length == 0)
                    {
                        Debug.LogWarning("No scenes found in Assets folder");
                        return;
                    }

                    string[] validScenes = sceneGuids
                        .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                        .Where(scenePath =>
                            !string.IsNullOrEmpty(scenePath) &&
                            !scenePath.Contains("/Packages/") &&
                            !scenePath.Contains("/PackageCache/") &&
                            !Path.GetFileNameWithoutExtension(scenePath).StartsWith("_") &&
                            !Path.GetFileNameWithoutExtension(scenePath).Contains("SerializationTests") &&
                            !Path.GetFileNameWithoutExtension(scenePath).Contains("Template"))
                        .ToArray();

                    if (validScenes.Length == 0)
                    {
                        Debug.LogWarning("No valid scenes found after filtering");
                        return;
                    }

                    string[] scenePaths = validScenes;
                    string[] sceneNames = scenePaths.Select(path => Path.GetFileNameWithoutExtension(path)).ToArray();
                    System.Array.Sort(sceneNames, scenePaths);

                    EditorApplication.delayCall += () => {
                        SceneDropdownToggle.ShowSceneSelectionWindow(scenePaths, sceneNames, true);
                    };
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error in ShowStartSceneSelection: {e.Message}\n{e.StackTrace}");
                }
            }
        }

        class SceneSelectionWindow : EditorWindow
        {
            internal string[] _originalScenePaths;
            internal string[] _originalSceneNames;
            internal string[] _currentScenePaths;
            internal string[] _currentSceneNames;
            internal bool _isStartSceneMode = false;

            private Vector2 _scrollPosition;
            private string _searchText = "";
            private Dictionary<string, bool> _folderFoldouts = new Dictionary<string, bool>();

            private void OnEnable()
            {
                titleContent = new GUIContent(_isStartSceneMode ? "Select Start Scene" : "Select Scene");
            }

            private void OnGUI()
            {
                if (_currentScenePaths == null || _currentSceneNames == null)
                {
                    EditorGUILayout.HelpBox("No scenes available.", MessageType.Warning);
                    return;
                }

                EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                EditorGUILayout.LabelField(_isStartSceneMode ? "Select Start Scene" : "Open Scene", EditorStyles.boldLabel, GUILayout.Width(120));

                EditorGUI.BeginChangeCheck();
                _searchText = EditorGUILayout.TextField(_searchText, EditorStyles.toolbarSearchField);
                if (EditorGUI.EndChangeCheck())
                {
                    FilterScenes();
                }
                EditorGUILayout.EndHorizontal();

                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
                Scene currentScene = EditorSceneManager.GetActiveScene();
                SceneAsset currentStartScene = EditorSceneManager.playModeStartScene;

                if (!string.IsNullOrWhiteSpace(_searchText))
                {
                    for (int i = 0; i < _currentSceneNames.Length; i++)
                    {
                        DrawSceneButton(_currentSceneNames[i], _currentScenePaths[i], currentScene, currentStartScene);
                    }
                }
                else
                {
                    Dictionary<string, List<(string Name, string Path)>> groupedScenes = GetGroupedScenes();
                    foreach (KeyValuePair<string, List<(string Name, string Path)>> group in groupedScenes)
                    {
                        if (!_folderFoldouts.ContainsKey(group.Key))
                            _folderFoldouts[group.Key] = group.Key == "Assets/Scenes";

                        _folderFoldouts[group.Key] = EditorGUILayout.Foldout(_folderFoldouts[group.Key], group.Key, true);

                        if (_folderFoldouts[group.Key])
                        {
                            foreach ((string Name, string Path) scene in group.Value)
                            {
                                DrawSceneButton(scene.Name, scene.Path, currentScene, currentStartScene);
                            }
                        }
                    }
                }

                EditorGUILayout.EndScrollView();
            }

            private void DrawSceneButton(string sceneName, string scenePath, Scene currentScene, SceneAsset currentStartScene)
            {
                bool isCurrentScene = currentScene.name == sceneName;
                bool isCurrentStartScene = currentStartScene != null && currentStartScene.name == sceneName;
                bool disableButton = !_isStartSceneMode && isCurrentScene;

                EditorGUI.BeginDisabledGroup(disableButton);

                GUIStyle buttonStyle = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleLeft };
                string buttonLabel = sceneName;
                if (isCurrentScene && isCurrentStartScene) buttonLabel += " [Start Scene] (Currently Open)";
                else if (isCurrentScene) buttonLabel += " (Currently Open)";
                else if (isCurrentStartScene) buttonLabel += " [Start Scene]";

                if (GUILayout.Button(buttonLabel, buttonStyle))
                {
                    if (_isStartSceneMode)
                    {
                        SceneAsset selectedScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
                        EditorSceneManager.playModeStartScene = selectedScene;
                        DefaultStartSceneToggle.BroadcastStartSceneChange();
                        SceneView.RepaintAll();
                    }
                    else
                    {
                        OpenSceneWithSaveCheck(currentScene, scenePath);
                    }

                    Close();
                }

                EditorGUI.EndDisabledGroup();
            }

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
                    _currentScenePaths = _originalScenePaths;
                    _currentSceneNames = _originalSceneNames;
                }
                else
                {
                    var filtered = _originalSceneNames
                        .Select((name, index) => new { Name = name, Path = _originalScenePaths[index] })
                        .Where(scene => scene.Name.IndexOf(_searchText, System.StringComparison.OrdinalIgnoreCase) >= 0)
                        .OrderBy(scene => scene.Name)
                        .ToList();

                    _currentSceneNames = filtered.Select(s => s.Name).ToArray();
                    _currentScenePaths = filtered.Select(s => s.Path).ToArray();
                }
            }

            private Dictionary<string, List<(string Name, string Path)>> GetGroupedScenes()
            {
                return _originalScenePaths
                    .Select((path, index) => new { Path = path, Name = _originalSceneNames[index] })
                    .GroupBy(s => Path.GetDirectoryName(s.Path).Replace("\\", "/"))
                    .OrderByDescending(g => g.Key == "Assets/Scenes")
                    .ThenBy(g => g.Key)
                    .ToDictionary(
                        g => g.Key,
                        g => g.OrderBy(s => s.Name).Select(s => (s.Name, s.Path)).ToList()
                    );
            }

        }
    }
}
