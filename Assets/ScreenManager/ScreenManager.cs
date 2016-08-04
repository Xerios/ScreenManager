//#define DEBUG_ScreenManager
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.EventSystems;
using System.Linq;

namespace ScreenMgr {

    /// <summary>
    /// Manages screens and their transitions
    /// </summary>
    public class ScreenManager: MonoBehaviour {
        

        public enum LayerPriority: int {
            Low = -100,
            Normal = 0,
            Popup = 100,
            High = 200,
            Alert = 300,
        };

        /// <summary>
        /// First/default screen
        /// </summary>
        public BaseScreen defaultScreen;

        /// <summary>
        /// Always have a button selected
        /// </summary>
        public bool alwaysOnSelection = false;

        /// <summary>
        /// Instead of selecting cancel button, execute it directly
        /// </summary>
        public bool instantCancelButton = false;

        /// <summary>
        /// Current active screen ( returns null when empty )
        /// </summary>
        public BaseScreen Current = null;

        /// <summary>
        /// Current active stack ( excluding delayed screen popup queues )
        /// </summary>
        public List<BaseScreen>.Enumerator Breadcrumbs {
            get {
                return screenQueue.GetEnumerator();
            }
        }

        /// <summary>
        /// ScreenHide and Show events
        /// </summary>
        public Action<BaseScreen> onScreenShow,onScreenHide;

        // Internal stuff, don't touch
        private Dictionary<string, BaseScreen> screensList;

        private List<BaseScreen> screenQueue;

        private GameObject lastSelection;

        private bool screenQueueDirty = false;
        private BaseScreen screenToKill = null;
        private BaseScreen screenToKeepOnTop = null;

        private void Awake() {
            Initialize();
        }


        /// <summary>
        /// Initializes the class and fills the screen list with children ( executed automatically on Awake() )
        /// </summary>
        private void Initialize() {
            ServiceLocator.Register<ScreenManager>(this);
            
            screenQueue = new List<BaseScreen>(50);

            screensList = new Dictionary<string, BaseScreen>();

            foreach (BaseScreen screen in GetComponentsInChildren<BaseScreen>(true)) {
                screen.Initialize(this, false);
                screensList[screen.name] = screen;
            }

            ShowDefault();

            StartCoroutine(CoroutineUpdate());
        }

        public void OnDestroy() {
            StopAllCoroutines();
        }


        IEnumerator CoroutineUpdate() {
            var waitTime = new WaitForSeconds(0.1f);

            while (true) {
                if (screenQueueDirty) {
                    if (screenToKill != null && screenToKill == Current) {
                        Debug("KILL: " + screenToKill);
                        if (onScreenHide != null) onScreenHide.Invoke(Current);

                        screenQueue.Remove(screenToKill);

                        EventSystem.current.SetSelectedGameObject(null);
                        screenToKill.selectedObject = null;
                        screenToKill.OnDeactivated(true, true);
                        if (screenToKill.keepOnTopWhenHiding) screenToKeepOnTop = screenToKill;
                        screenToKill = null;
                        Current = null;
                    }

                    BaseScreen maxPriorityScreen = screenQueue.LastOrDefault();

                    // Is highest-score screen different from current shown one? Then show highest-score screen and hide current
                    if (Current != maxPriorityScreen) {
                        Debug("Different --> " + Current + " != " + maxPriorityScreen);

                        BaseScreen previousScreen = Current;

                        if (previousScreen != null) {
                            previousScreen.selectedObject = EventSystem.current.currentSelectedGameObject;
                            EventSystem.current.SetSelectedGameObject(null);
                        }

                        if (maxPriorityScreen.Transition) { // Wait for transition
                            Debug("Transition is busy?");
                            Current = null;
                            screenQueueDirty = true;
                            yield return waitTime;
                            continue;
                        } else {
                            Current = maxPriorityScreen;

                            if (Current == null && defaultScreen != null) Current = defaultScreen;

                            if (Current != null) {
                                if (onScreenShow != null) onScreenShow.Invoke(Current);
                                Current.OnActivated();
                            }

                            if (previousScreen != null) {
                                previousScreen.OnDeactivated(Current.hideCurrent);
                            } else if (Current.hideCurrent && screenQueue.Count > 1) {
                                for (int i = screenQueue.Count - 2; i >= 0; i--) {
                                    screenQueue[i].OnDeactivated(true);
                                }
                            }

                            if (screenToKeepOnTop != null && screenToKeepOnTop.isActiveAndEnabled) {
                                screenToKeepOnTop.transform.SetAsLastSibling();
                                screenToKeepOnTop = null;
                            }
                        }
                    }

                    screenQueueDirty = false;
                }

                // Make sure we're always selecting something when always-on is enabled
                if (alwaysOnSelection) {
                    if (Current != null && !Current.Transition) {
                        GameObject selectedGameObject = EventSystem.current.currentSelectedGameObject;
                        bool isCurrentActive = (selectedGameObject != null && selectedGameObject.activeInHierarchy);

                        if (!isCurrentActive) {
                            if (lastSelection!=null && lastSelection.activeInHierarchy && lastSelection.transform.IsChildOf(Current.transform)) {
                                EventSystem.current.SetSelectedGameObject(lastSelection);
                            } else if (Current.defaultSelection != null && Current.defaultSelection.gameObject.activeInHierarchy) {
                                EventSystem.current.SetSelectedGameObject(Current.defaultSelection.gameObject);
                                lastSelection = Current.defaultSelection.gameObject;
                            }
                        } else {
                            //Save last selection when everything is fine
                            lastSelection = selectedGameObject;
                        }
                    }
                }

                yield return waitTime;
            }
        }

        /// <summary>
        /// Duplicates specified screen and shows it
        /// Good use for messageboxes, notification boxes or anything that can or will be shown multiple times
        /// ( Use a custom class inheriting BaseScreen with OnShow(...), see Popup for example )
        /// </summary>
        /// <param name="screenName"></param>
        /// <param name="data"></param>
        public T ShowPopup<T>(string screenName) where T : BaseScreen {
            if (!screensList.ContainsKey(screenName)) {
                throw new KeyNotFoundException("ScreenManager: Show failed. Screen with name '" + screenName + "' does not exist.");
            }

            GameObject newDupeScreen = GameObject.Instantiate(screensList[screenName].gameObject);
            newDupeScreen.transform.SetParent(transform, false);
            BaseScreen baseScreen = newDupeScreen.GetComponent<BaseScreen>();
            baseScreen.Initialize(this, true);

            newDupeScreen.name = screenName + " (" + (baseScreen.ID) + ")";


            return ShowScreen(baseScreen,true) as T;
        }

        /// <summary>
        /// Shows the default screen ( if set )
        /// </summary>
        public void ShowDefault(bool force = false) {
            if (defaultScreen != null) ShowScreen(defaultScreen, force);
        }


        /// <summary>
        /// Shows specified screen (with no return)
        /// </summary>
        /// <param name="screen"></param>
        public void Show(string screenName) {
            ShowScreen(screenName);
        }

        /// <summary>
        /// Shows specified screen (with no return)
        /// </summary>
        /// <param name="screen"></param>
        public void Show(BaseScreen screen) {
            ShowScreen(screen);
        }

        /// <summary>
        /// Shows specified screen
        /// </summary>
        /// <param name="screenName"></param>
        public BaseScreen ShowScreen(string screenName, bool force = false) {
            if (!screensList.ContainsKey(screenName)) {
                throw new KeyNotFoundException("ScreenManager: Show failed. Screen with name '" + screenName + "' does not exist.");
            }
            return ShowScreen(screensList[screenName], force);
        }
        /// <summary>
        /// Shows specified screen ( Use Show("MyScreenName"); instead )
        /// </summary>
        /// <param name="screen"></param>
        public BaseScreen ShowScreen(BaseScreen screen, bool force = false) {
            if (screen == null) {
                throw new KeyNotFoundException("ScreenManager: Show(BaseScreen) failed, screen is Null.");
            }

            Debug("SHOW:" + screen.name);

            //Force screen open or wait until screens are properly removed and added
            if (!force && (screenQueueDirty || screenQueue.LastOrDefault() == screen)) {
                return screen;
            }

            screenQueue.Add(screen);
            InsertionSort(screenQueue);

            // Is screen a higher priority and should be show this instead of current one?
            if (Current == null || (int)Current.layerPriority <= (int)screen.layerPriority) {
                if (Current!=null) Current.OnDeactivated(false);
                screenQueueDirty = true;
            }

            return screen;
        }


        /// <summary>
        /// Hides ( or removes if it's a copy ) the current screen
        /// </summary>
        public void Hide() {
            if (!screenQueueDirty && Current != null && Current.Transition) return;

            screenToKill = Current;
            screenQueueDirty = true;
        }

        /// <summary>
        /// Hides all screns of specific type ( or removes them when they're a copy )
        /// </summary>
        public void HideAll() {

            Debug("HIDE ALL");

            foreach (var item in screenQueue) {
                if (item == Current) continue;
                item.selectedObject = null;
                item.OnDeactivated(true, true);
            }

            screenToKill = Current;
            screenQueueDirty = true;
        }

        private static void InsertionSort(IList<BaseScreen> list) {
            if (list == null) throw new ArgumentNullException("list");

            int count = list.Count;
            for (int j = 1; j < count; j++) {
                BaseScreen key = list[j];

                int i = j - 1;
                for (; i >= 0 && CompareBaseScreens(list[i], key) > 0; i--) {
                    list[i + 1] = list[i];
                }
                list[i + 1] = key;
            }
        }

        private static int CompareBaseScreens(BaseScreen x, BaseScreen y) {
            int result = 1;
            if (x != null && x is BaseScreen &&
                y != null && y is BaseScreen) {
                BaseScreen screenX = (BaseScreen)x;
                BaseScreen screenY = (BaseScreen)y;
                result = screenX.CompareTo(screenY);
            }
            return result;
        }

        [System.Diagnostics.Conditional("DEBUG_ScreenManager")]
        private void Debug(string str) {
            UnityEngine.Debug.Log("ScreenManager: "+ str, this);
        }
    }

}