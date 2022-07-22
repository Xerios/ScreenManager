#if UNITY_EDITOR

using System;
using UnityEngine;
using UnityEditor;
using System.Linq;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEditor.Experimental.SceneManagement;

namespace ScreenMgr
{
    /// <summary>
    /// Editor for ScreenManager
    /// </summary>
    [CustomEditor(typeof(ScreenManager))]
    public class ScreenManagerEditor : Editor
    {
        private ScreenManager ScreenManager { get { return target as ScreenManager; } }

        private double clickTime;
        private string searchString = "";
        private int selectedId = -99;
        private double doubleClickTime = 0.3;
        private bool isDuplicated;

        private BaseScreen[] TestingScreens
        {
            get
            {
                return ScreenManager.GetComponentsInChildren<BaseScreen>(true);
            }
        }

        [MenuItem("Window/Select ScreenManager %&q")]
        static void ValidateLogSelectedTransformName()
        {
            Selection.activeTransform = GameObject.FindObjectOfType<ScreenManager>().transform;
        }

        private void OnSavingScene(Scene scene, string path) => UndoSceneChanges();
        private void OnEnteringPlayMode(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                UndoSceneChanges();
            }
        }

        private void UndoSceneChanges()
        {
            EditorApplication.playModeStateChanged -= OnEnteringPlayMode;
            EditorSceneManager.sceneSaving -= OnSavingScene;

            //Turn Off SpreadMode
            SpreadMode(false);
            Repaint();
        }

        private void Awake()
        {
            EditorApplication.playModeStateChanged += OnEnteringPlayMode;
            EditorSceneManager.sceneSaving += OnSavingScene;
        }

        private BaseScreen[] TestAllScreens()
        {
            return ScreenManager.allScreens.Select(o => TestScreen(o)).ToArray();
        }

        public BaseScreen VisualizeScreen(ObjectResourcesLoader<BaseScreen> screenLoader)
        {
            var screen = PrefabUtility.InstantiatePrefab(screenLoader.LoadObjectFromResources(), ScreenManager.transform) as BaseScreen;
            var screenRectTransform = screen.GetComponent<RectTransform>();

            screenRectTransform.localScale = Vector3.one;
            screenRectTransform.anchorMin = Vector2.zero;
            screenRectTransform.anchorMax = Vector2.one;
            screenRectTransform.offsetMin = Vector2.zero;
            screenRectTransform.offsetMax = Vector2.zero;

            screen.gameObject.SetActive(true);
            return screen;
        }

        private BaseScreen TestScreen(ObjectResourcesLoader<BaseScreen> screen)
        {
            if (string.IsNullOrEmpty(screen.ObjectPath)) throw new NullReferenceException("Screen Can Not Be Null");

            var screenInstance = VisualizeScreen(screen);
            Selection.activeGameObject = screenInstance.gameObject;
            return screenInstance;
        }

        private BaseScreen TestScreen(string screenName)
        {
            var screen = ScreenManager.allScreens.FirstOrDefault(o => o.GetFileName() == screenName);
            return TestScreen(screen);
        }

        private void ScreenTestFinished(string screenName)
        {
            var screen = GetTestScreen(screenName);
            DestroyImmediate(screen.gameObject);
        }

        private void AllScreenTestsFinished()
        {
            //Remove Testing Screens
            foreach (var testingScreen in TestingScreens)
            {
                DestroyImmediate(testingScreen.gameObject);
            }
        }

        private void MarkDirty()
        {
            EditorUtility.SetDirty(target);
        }

        private bool IsTestingScreen(string screenName) => TestingScreens.Any(o => screenName == o.name);

        private BaseScreen GetTestScreen(string screenName)
        {
            return TestingScreens.FirstOrDefault(o => o.name == screenName);
        }

        private bool DrawContentFittedButton(string text)
        {
            var buttonContent = new GUIContent(text);
            return GUILayout.Button(text, GUILayout.MaxWidth(GUI.skin.button.CalcSize(buttonContent).x));
        }

        private bool DrawScreenRow(SerializedProperty allScreenElement, ObjectResourcesLoader<BaseScreen> refrenceLoader)
        {
            var isDirty = false;
            var beforeColor = GUI.color;
            var screenName = refrenceLoader.GetFileName();
            var isTesting = IsTestingScreen(screenName);
            isDuplicated = ScreenManager.allScreens.Count(o => o.GetFileName() == screenName) > 1;

            GUI.color = isDuplicated ? Color.red : GUI.color;

            GUILayout.BeginHorizontal();
            var fixedLabel = new GUIContent("ScreenName : ", "ScreenManager Finds Your Screen With This Name");
            EditorGUILayout.LabelField(fixedLabel, GUILayout.MaxWidth(GUI.skin.label.CalcSize(fixedLabel).x));
            GUILayout.Space(20);
            var screenNameLabel = new GUIContent(screenName, "ScreenManager Finds Your Screen With This Name");
            EditorGUILayout.LabelField(screenNameLabel);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (EditorGUILayout.Toggle(isTesting, GUILayout.MaxWidth(20)) != isTesting)
            {
                isTesting = !isTesting;
                if (isTesting)
                {
                    var screenInstance = TestScreen(screenName);
                    if (!EditorApplication.isPlaying)
                    {
                        foreach (var other in ScreenManager.GetComponentsInChildren<BaseScreen>(true))
                        {
                            if (other == screenInstance) continue;
                            ScreenTestFinished(other.name);
                        }
                    }
                }
                else
                {
                    ScreenTestFinished(screenName);
                }
            }
            EditorGUILayout.PropertyField(allScreenElement, new GUIContent(), true);
            allScreenElement.serializedObject.ApplyModifiedProperties();

            //GUILayout.FlexibleSpace();

            if (DrawContentFittedButton("Remove"))
            {
                ScreenManager.allScreens.RemoveAll(o => o.GetFileName() == screenName);
                Repaint();
                return true;
            }

            if (string.IsNullOrEmpty(refrenceLoader.ObjectPath)) goto EndOfRow;

            if (DrawContentFittedButton("Edit"))
            {
                AssetDatabase.OpenAsset(refrenceLoader.LoadObjectFromResources().GetInstanceID());
            }

            if (ScreenManager.defaultScreen == screenName && DrawContentFittedButton("Clear Default"))
            {
                ScreenManager.defaultScreen = "";
                isDirty = true;
            }
            else if (ScreenManager.defaultScreen != screenName && DrawContentFittedButton("Set Default"))
            {
                ScreenManager.defaultScreen = screenName;
                isDirty = true;
            }


        EndOfRow:
            GUILayout.EndHorizontal();
            GUI.color = beforeColor;

            return isDirty;
        }

        public override void OnInspectorGUI()
        {
            var isDirty = false;

            // Get the transform of the object and use that to get all children in alphabetical order
            Color slightGray = new Color(1f, 1f, 1f, 0.4f);

            GUILayout.Space(10);
            GUI.color = EditorGUIUtility.isProSkin ? Color.white : Color.black;
            GUILayout.Toggle(false, "Screen Manager", GUI.skin.FindStyle("LODLevelNotifyText"));
            GUILayout.Space(15);
            GUI.color = Color.white;


            if (!EditorApplication.isPlaying)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Spread"))
                {
                    SpreadMode(true);
                }

                if (GUILayout.Button("UnSpread"))
                {
                    SpreadMode(false);
                }

                GUILayout.EndHorizontal();
                GUILayout.Space(15);
            }

            // Default Screen
            if (ScreenManager.defaultScreen != null)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(ScreenManager.dontDestroyOnLoad)));

                GUILayout.Space(20);
                GUILayout.Label("Default Screen :");
                bool isEnabled = IsTestingScreen(ScreenManager.defaultScreen);
                GUI.color = isEnabled ? Color.white : slightGray;

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Toggle(isEnabled, "");//, GUI.skin.FindStyle("VisibilityToggle"));

                    if (GUILayout.Button(ScreenManager.defaultScreen, EditorStyles.label))
                    {
                        if (selectedId == -1 && (EditorApplication.timeSinceStartup - clickTime) < doubleClickTime)
                        {
                            Selection.activeGameObject = GetTestScreen(ScreenManager.defaultScreen)?.gameObject;
                        }
                        clickTime = EditorApplication.timeSinceStartup;
                        selectedId = -1;
                    }

                    // Check if class exists
                    if (selectedId == -1)
                    {
                        GUI.color = Color.white;
                        if (!EditorApplication.isPlaying && GUILayout.Button("Clear Default", EditorStyles.miniButton, GUILayout.Width(100)))
                        {
                            ScreenManager.defaultScreen = null;
                            Repaint();
                            MarkDirty();
                            return;
                        }
                    }
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(10);

            GUI.color = Color.white;

            // Screen Management with that fancy list and all
            GUILayout.Label("Screens :");

            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                GUILayout.BeginHorizontal(GUI.skin.FindStyle("Toolbar"));
                //GUILayout.FlexibleSpace();
                searchString = GUILayout.TextField(searchString, GUI.skin.FindStyle("ToolbarSeachTextField")/*, GUILayout.MaxWidth(300)*/);
                if (GUILayout.Button("", GUI.skin.FindStyle("ToolbarSeachCancelButton")))
                {
                    // Remove focus if cleared
                    searchString = "";
                    GUI.FocusControl(null);
                }

                GUILayout.EndHorizontal();
            }
            GUILayout.EndHorizontal();

            EditorGUILayout.Space(15);

            GUILayout.BeginVertical();

            //Drawing Object Resources Loader Of Screens
            var allScreensProperty = serializedObject.FindProperty(nameof(ScreenManager.allScreens));
            for (int i = 0; i < allScreensProperty.arraySize; i++)
            {
                if (i >= ScreenManager.allScreens.Count) break;

                if (!ScreenManager.allScreens[i].GetFileName().ToLower().Contains(searchString.ToLower())) continue;
                isDirty = DrawScreenRow(allScreensProperty.GetArrayElementAtIndex(i), ScreenManager.allScreens[i]) || isDirty;
                GUILayout.Space(15);
            }

            GUILayout.EndVertical();

            GUILayout.Space(20);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Add Screen", GUILayout.MaxWidth(150)))
            {
                ScreenManager.allScreens.Add(default);
                Repaint();
                isDirty = true;
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(30);

            if (EditorApplication.isPlaying)
            {
                Repaint();
            }

            if (isDirty)
            {
                MarkDirty();
            }

        }

        private void SpreadMode(bool active)
        {
            if (ScreenManager == null) return;

            RectTransform rectTScreenMgr = ScreenManager.GetComponent<RectTransform>();

            if (active)
            {
                var screenList = TestAllScreens();
                // Spread
                float screenCountX = 0, screenCountY = 0;
                for (int i = 0; i < screenList.Length; i++)
                {
                    BaseScreen screen = screenList[i];
                    screen.gameObject.SetActive(true);
                    RectTransform rectT = screen.GetComponent<RectTransform>();
                    if (rectT != null)
                    {
                        screenCountX += (rectTScreenMgr.rect.xMin - rectT.rect.xMin);
                        screenCountY += (rectTScreenMgr.rect.yMin - rectT.rect.yMin);

                        rectT.anchoredPosition = new Vector2(screenCountX, screenCountY);

                        screenCountY -= (rectTScreenMgr.rect.yMax - rectT.rect.yMax);

                        screenCountX += rectTScreenMgr.rect.width - (rectTScreenMgr.rect.xMax - rectT.rect.xMax);

                        if (i != 0 && (i % 4) == 0)
                        {
                            screenCountX = 0;
                            screenCountY += rectT.rect.height;
                        }
                    }
                }
            }
            else
                AllScreenTestsFinished();
        }
    }
}
#endif