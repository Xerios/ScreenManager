using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace ScreenMgr
{
    /// <summary>
    /// Editor for ScreenManager
    /// </summary>
    [CustomEditor(typeof(ScreenManager))]
    public class ScreenManagerEditor : Editor
    {
        private double clickTime;
        private bool filterScreens,filterPopups;
        private string searchString = "";
        private int selectedId = -99;
        private double doubleClickTime = 0.3;

        private bool spreadMode = false;

        private bool showScreenManagerSettings,showNavigation;

        [MenuItem("Window/Select ScreenManager %&t")]
        static void ValidateLogSelectedTransformName() {
            Selection.activeTransform = GameObject.FindObjectOfType<ScreenManager>().transform;
        }

        public void OnPlayModeEnter(PlayModeStateChange state) {
            ScreenManager screenMgr = (target as ScreenManager);
            spreadMode = false;
            SpreadMode(false, screenMgr);
            EditorApplication.playModeStateChanged -= OnPlayModeEnter;
            this.Repaint();
        }

        public override void OnInspectorGUI()
        {
            // Get the transform of the object and use that to get all children in alphabetical order
            ScreenManager screenMgr = (target as ScreenManager);
            var transform = screenMgr.transform;
            var children = (transform as IEnumerable).Cast<Transform>().OrderBy(t => t.gameObject.name).ToArray();
            
            Color slightGray = new Color(1f, 1f, 1f, 0.4f);

            GUILayout.Space(10);
            GUI.color = EditorGUIUtility.isProSkin ? Color.white : Color.black;
            GUILayout.Toggle(false,"Screen Manager", GUI.skin.FindStyle("LODLevelNotifyText"));
            GUI.color = Color.white;


            if (!EditorApplication.isPlaying) {
                GUILayout.BeginVertical();
                bool newToggle = GUILayout.Toggle(spreadMode,"Spread", EditorStyles.miniButton);

                if (newToggle != spreadMode) {
                    spreadMode = newToggle;
                    SpreadMode(spreadMode, screenMgr);
                    if (spreadMode) {
                        EditorApplication.playModeStateChanged += OnPlayModeEnter;
                    }
                }
                GUILayout.Space(10);
            }

            showScreenManagerSettings = EditorGUILayout.Foldout(showScreenManagerSettings, "Main Settings");
            if (showScreenManagerSettings) {
                screenMgr.touchMode = GUILayout.Toggle(screenMgr.touchMode, "Touch Mode ( disable auto-selection )");
                if (!screenMgr.touchMode){
                    screenMgr.alwaysOnSelection = GUILayout.Toggle(screenMgr.alwaysOnSelection, "Always-on Selection");
                    screenMgr.instantCancelButton = GUILayout.Toggle(screenMgr.instantCancelButton, "Instant Cancel Button");
                }
                GUILayout.Space(10);
            }

            if (!EditorApplication.isPlaying) {

                showNavigation = EditorGUILayout.Foldout(showNavigation, "Navigation Tools");

                if (showNavigation) {
                    GUILayout.BeginHorizontal();
                    {
                        if (GUILayout.Button("Clear Navigation", EditorStyles.miniButtonLeft)) ClearNavigation(screenMgr, children);
                        if (GUILayout.Button("Update Navigation", EditorStyles.miniButtonRight)) UpdateNavigation(screenMgr, children);
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.Space(10);
                }
                GUILayout.EndVertical();
            }

            GUILayout.Space(20);

            // Show screen stack during Play mode
            if (EditorApplication.isPlaying) {
                GUILayout.Label("Stack :");
                var e = screenMgr.Breadcrumbs;
                GUILayout.BeginHorizontal();
                {
                    while (e.MoveNext()) {
                        if (GUILayout.Button(e.Current.name, GUI.skin.FindStyle("Tooltip"))) Selection.activeGameObject = e.Current.gameObject;
                        GUILayout.Space(10);
                    }
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(10);
            }

            // Default Screen
            if (screenMgr.defaultScreen!=null) {
                GUILayout.Label("Default Screen :");
                bool isEnabled = screenMgr.defaultScreen.gameObject.activeSelf;
                GUI.color = isEnabled ? Color.white : slightGray;

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Toggle(isEnabled, "");//, GUI.skin.FindStyle("VisibilityToggle"));

                    if (GUILayout.Button(screenMgr.defaultScreen.name, EditorStyles.label)) {
                        if (selectedId == -1 && (EditorApplication.timeSinceStartup - clickTime) < doubleClickTime) {
                            Selection.activeGameObject = screenMgr.defaultScreen.gameObject;
                        }
                        clickTime = EditorApplication.timeSinceStartup;
                        selectedId = -1;

                        if (!EditorApplication.isPlaying) {
                            foreach (var other in children) {
                                other.gameObject.SetActive(screenMgr.defaultScreen.transform == other);
                            }
                        }
                    }

                    // Check if class exists
                    if (selectedId == -1) {
                        GUI.color = Color.white;
                        if (!EditorApplication.isPlaying && GUILayout.Button("Clear Default", EditorStyles.miniButton, GUILayout.Width(100))) {
                            screenMgr.defaultScreen = null;
                            Repaint();
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
                var allValue = GUILayout.Toggle((filterScreens && filterPopups) || (!filterScreens && !filterPopups), "All", EditorStyles.toolbarButton, GUILayout.Width(30));

                if (allValue) filterScreens = filterPopups = true;

                filterScreens = GUILayout.Toggle(filterScreens, "Screens", EditorStyles.toolbarButton, GUILayout.Width(70));
                filterPopups = GUILayout.Toggle(filterPopups, "Popups", EditorStyles.toolbarButton, GUILayout.Width(70));

                if (filterScreens && filterPopups) filterScreens = filterPopups = false;

                GUILayout.BeginHorizontal(GUI.skin.FindStyle("Toolbar"));
                GUILayout.FlexibleSpace();
                searchString = GUILayout.TextField(searchString, GUI.skin.FindStyle("ToolbarSeachTextField"), GUILayout.MaxWidth(300));
                if (GUILayout.Button("", GUI.skin.FindStyle("ToolbarSeachCancelButton"))) {
                    // Remove focus if cleared
                    searchString = "";
                    GUI.FocusControl(null);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginVertical();

            // Make a button for each child. Pressing the button enables that child and disables all others
            for (int i = 0; i < children.Length; i++) {
                var child = children[i];
                bool isEnabled = child.gameObject.activeSelf;
                var screen = child.GetComponent<BaseScreen>();

                if (screenMgr.defaultScreen!=null && screenMgr.defaultScreen.transform==child) continue; // Ignore default screen since it's already shown up there
                if (filterPopups && screen.layerPriority != ScreenManager.LayerPriority.Normal) continue;
                if (filterScreens && screen.layerPriority == ScreenManager.LayerPriority.Normal) continue;
                if (!string.IsNullOrEmpty(searchString) && !child.gameObject.name.ToLowerInvariant().Contains(searchString.ToLowerInvariant())) continue;

                GUI.color = isEnabled ? Color.white : slightGray;

                GUILayout.BeginHorizontal(EditorStyles.toolbar);// GUI.skin.FindStyle("TL tab mid"));
                GUILayout.Toggle(isEnabled, "");//, GUI.skin.FindStyle("VisibilityToggle"));

                string label = child.gameObject.name;
                if (screenMgr.Current == screen) label = "CURRENT: " + label;

                if (GUILayout.Button(label, EditorStyles.label)) {
                    if (selectedId==i && (EditorApplication.timeSinceStartup - clickTime) < doubleClickTime){
                        Selection.activeGameObject = child.gameObject;
                    }
                    clickTime = EditorApplication.timeSinceStartup;
                    selectedId = i;

                    if (!EditorApplication.isPlaying) {
                        foreach (var other in children) {
                            other.gameObject.SetActive(child == other); 
                        }
                    }
                    break;
                }

                // Check if class exists
                if (screen != null) {
                    if (selectedId == i) {
                        GUI.color = Color.white;
                        if (!EditorApplication.isPlaying && GUILayout.Button("Set Default", EditorStyles.miniButton, GUILayout.Width(100))) {
                            screenMgr.defaultScreen = screen;
                            selectedId = -1;
                            break;
                        }
                    }
                } else {
                    GUILayout.Button("MISSING CLASS", GUI.skin.FindStyle("ChannelStripAttenuationMarkerSquare"), GUILayout.Width(150));
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();

            GUILayout.Space(10);

            if (EditorApplication.isPlaying)
            {
                this.Repaint();
            }
        }

        private void SpreadMode(bool active, ScreenManager screenMgr) {

            RectTransform rectTScreenMgr = screenMgr.GetComponent<RectTransform>();

            var screenList = screenMgr.GetComponentsInChildren<BaseScreen>(true);

            if (active) {
                // Spread
                float screenCountX = 0, screenCountY = 0;
                for (int i = 0; i < screenList.Length; i++) {
                    BaseScreen screen = screenList[i];
                    screen.gameObject.SetActive(true);
                    RectTransform rectT = screen.GetComponent<RectTransform>();
                    if (rectT != null) {
                        screenCountX += (rectTScreenMgr.rect.xMin - rectT.rect.xMin);
                        screenCountY += (rectTScreenMgr.rect.yMin - rectT.rect.yMin);

                        rectT.anchoredPosition = new Vector2(screenCountX, screenCountY);

                        screenCountY -= (rectTScreenMgr.rect.yMax - rectT.rect.yMax);
                        
                        screenCountX += rectTScreenMgr.rect.width - (rectTScreenMgr.rect.xMax - rectT.rect.xMax);

                        if (i != 0 && (i % 4) == 0) {
                            screenCountX = 0;
                            screenCountY += rectT.rect.height;
                        }
                    }
                }
            } else {
                // Unspread
                for (int i = 0; i < screenList.Length; i++) {
                    BaseScreen screen = screenList[i];
                    screen.gameObject.SetActive(false);
                    RectTransform rectT = screen.GetComponent<RectTransform>();
                    if (rectT != null) {
                        rectT.anchoredPosition = Vector2.zero;
                    }
                }
                if (screenMgr.defaultScreen != null) screenMgr.defaultScreen.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Clears navigation settings and removes CancelTrigger class from all selectables
        /// </summary>
        /// <param name="ScreenMgr"></param>
        /// <param name="children"></param>
        private void ClearNavigation(ScreenManager ScreenMgr, Transform[] children) {
            for (int i = 0; i < children.Length; i++) children[i].gameObject.SetActive(false);

            List<Selectable> selectables = new List<Selectable>();

            for (int i = 0; i < children.Length; i++) {
                Transform parent = children[i];
                BaseScreen screen = parent.GetComponent<BaseScreen>();
                if (!screen.generateNavigation) continue;

                parent.gameObject.SetActive(true);

                selectables.Clear();
                parent.GetComponentsInChildren<Selectable>(selectables);
                foreach (Selectable selectableUI in selectables) {
                    selectableUI.navigation = new Navigation() { mode = Navigation.Mode.Automatic };
                    CancelTrigger sctrigger = selectableUI.gameObject.GetComponent<CancelTrigger>();

                    if (sctrigger != null) DestroyImmediate(sctrigger);
                }
            }
            if (ScreenMgr.defaultScreen != null) ScreenMgr.defaultScreen.gameObject.SetActive(true);
        }

        /// <summary>
        /// My black vodoo script, do not use outside
        /// </summary>
        /// <param name="ScreenMgr"></param>
        /// <param name="children"></param>
        private void UpdateNavigation(ScreenManager ScreenMgr, Transform[] children) {
            for (int i = 0; i < children.Length; i++) children[i].gameObject.SetActive(false);

            List<Selectable> selectables = new List<Selectable>();

            for (int i = 0; i < children.Length; i++) {
                Transform parent = children[i];
                BaseScreen screen = parent.GetComponent<BaseScreen>();

                if (!screen.generateNavigation) continue;

                parent.gameObject.SetActive(true);

                selectables.Clear();
                parent.GetComponentsInChildren<Selectable>(selectables);

                Selectable[] directions = new Selectable[4];
                foreach (Selectable selectableUI in selectables) {
                    //Debug.Log("<b>" + parent.name + "</b>." + selectableUI.name, selectableUI);

                    Transform t = selectableUI.transform;

                    directions[0] = FindSelectable(parent, t, selectables, t.rotation * Vector3.up);
                    directions[1] = FindSelectable(parent, t, selectables, t.rotation * Vector3.right);
                    directions[2] = FindSelectable(parent, t, selectables, t.rotation * Vector3.down);
                    directions[3] = FindSelectable(parent, t, selectables, t.rotation * Vector3.left);

                    if (selectableUI is Slider) {
                        Slider.Direction dir = (selectableUI as Slider).direction;
                        if (dir == Slider.Direction.TopToBottom || dir == Slider.Direction.BottomToTop) {
                            directions[0] = directions[2] = null;
                        } else {
                            directions[1] = directions[3] = null;
                        }
                    }
                    
                    selectableUI.navigation = new Navigation() {
                        mode = Navigation.Mode.Explicit,
                        selectOnUp = directions[0],
                        selectOnRight = directions[1],
                        selectOnDown = directions[2],
                        selectOnLeft = directions[3],
                    };


                    CancelTrigger sctrigger = selectableUI.gameObject.GetComponent<CancelTrigger>();

                    if (sctrigger == null) {
                        var cancelHandlerAvailable = selectableUI.GetComponent<ICancelHandler>();
                        if (cancelHandlerAvailable == null) {
                            sctrigger = selectableUI.gameObject.AddComponent<CancelTrigger>();
                            sctrigger.disableCancelHandler = cancelHandlerAvailable != null;
                        }
                    }

                }
                parent.gameObject.SetActive(false);
            }
            if (ScreenMgr.defaultScreen != null) ScreenMgr.defaultScreen.gameObject.SetActive(true);
        }


        //----------------------- EVERTHING BELOW THIS LINE IS TAKEN FROM OFFICIAL UNITY UI SOURCES


        // Find the next selectable object in the specified world-space direction.
        private Selectable FindSelectable(Transform parent, Transform transform, List<Selectable> list, Vector3 dir) {
            dir = dir.normalized;
            Vector3 localDir = Quaternion.Inverse(transform.rotation) * dir;
            Vector3 pos = transform.TransformPoint(GetPointOnRectEdge(transform as RectTransform, localDir));
            float maxScore = Mathf.NegativeInfinity;
            Selectable bestPick = null;
            for (int i = 0; i < list.Count; ++i) {
                Selectable sel = list[i];

                if (sel == this || sel == null)
                    continue;

                if (!sel.transform.IsChildOf(parent) || sel.navigation.mode == Navigation.Mode.None)
                    continue;

                var selRect = sel.transform as RectTransform;
                Vector3 selCenter = selRect != null ? (Vector3)selRect.rect.center : Vector3.zero;
                Vector3 myVector = sel.transform.TransformPoint(selCenter) - pos;

                // Value that is the distance out along the direction.
                float dot = Vector3.Dot(dir, myVector);

                // Skip elements that are in the wrong direction or which have zero distance.
                // This also ensures that the scoring formula below will not have a division by zero error.
                if (dot <= 0)
                    continue;

                // This scoring function has two priorities:
                // - Score higher for positions that are closer.
                // - Score higher for positions that are located in the right direction.
                // This scoring function combines both of these criteria.
                // It can be seen as this:
                //   Dot (dir, myVector.normalized) / myVector.magnitude
                // The first part equals 1 if the direction of myVector is the same as dir, and 0 if it's orthogonal.
                // The second part scores lower the greater the distance is by dividing by the distance.
                // The formula below is equivalent but more optimized.
                //
                // If a given score is chosen, the positions that evaluate to that score will form a circle
                // that touches pos and whose center is located along dir. A way to visualize the resulting functionality is this:
                // From the position pos, blow up a circular balloon so it grows in the direction of dir.
                // The first Selectable whose center the circular balloon touches is the one that's chosen.
                float score = dot / myVector.sqrMagnitude;

                if (score > maxScore) {
                    maxScore = score;
                    bestPick = sel;
                }
            }
            return bestPick;
        }


        private static Vector3 GetPointOnRectEdge(RectTransform rect, Vector2 dir) {
            if (rect == null)
                return Vector3.zero;
            if (dir != Vector2.zero)
                dir /= Mathf.Max(Mathf.Abs(dir.x), Mathf.Abs(dir.y));
            dir = rect.rect.center + Vector2.Scale(rect.rect.size, dir * 0.5f);
            return dir;
        }
    }
}
