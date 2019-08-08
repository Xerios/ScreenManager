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
        public class ScreenNavigationData
        {
            public BaseScreen screen;
            public object data;
            public ScreenNavigationData(BaseScreen screen, object data = null)
            {
                this.screen = screen;
                this.data = data;
            }
        }

        public enum LayerPriority: int {
            Low = -100,
            Normal = 0,
            Popup = 100,
            High = 200,
            Alert = 300,
        };

        public Transform screensContainer;
        /// <summary>
        /// First/default screen
        /// </summary>
        public BaseScreen defaultScreen;

        /// <summary>
        /// Always have a button selected
        /// </summary>
        public bool alwaysOnSelection = false;

        /// <summary>
        /// Useful for touch only menus ( removes default selection and overrides and disables alwaysOnSelection )
        /// </summary>
        public bool touchMode = false;

        /// <summary>
        /// Instead of selecting cancel button, execute it directly
        /// </summary>
        public bool instantCancelButton = false;

        /// <summary>
        /// Current active screen ( returns null when empty )
        /// </summary>
        [NonSerialized]
        public BaseScreen Current = null;

        /// <summary>
        /// Current active stack ( excluding delayed screen popup queues )
        /// </summary>
        public List<ScreenNavigationData>.Enumerator Breadcrumbs {
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

        private List<ScreenNavigationData> screenQueue;

        private GameObject lastSelection;

        private bool screenQueueDirty = false;
        private BaseScreen screenToKill = null;
        private BaseScreen screenToKeepOnTop = null;
        private BaseScreen screenToShowInTheEnd = null;

        private void Awake() {
            Initialize();
        }
        public void OnEnable(){
            StopAllCoroutines();
            StartCoroutine(CoroutineUpdate());
        }
        public void OnDisable(){
            StopAllCoroutines();
        }

        /// <summary>
        /// Initializes the class and fills the screen list with children ( executed automatically on Awake() )
        /// </summary>
        private void Initialize() {
            ServiceLocator.Register<ScreenManager>(this, true);
            
            screenQueue = new List<ScreenNavigationData>(50);

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
            var waitTime = new WaitForSecondsRealtime(0.1f);

            while (true) {
                if (screenQueueDirty) {
                    if (screenToKill != null && screenToKill == Current) {
                        Debug("KILL: " + screenToKill);
                        if (onScreenHide != null) onScreenHide.Invoke(Current);

                        int screenToKillIndex = screenQueue.FindLastIndex(x => x.screen == screenToKill);
                        if (screenToKillIndex!=-1) screenQueue.RemoveAt(screenToKillIndex);

                        EventSystem.current.SetSelectedGameObject(null);
                        screenToKill.selectedObject = null;
                        screenToKill.OnDeactivated(true, true);
                        if (screenToKill.keepOnTopWhenHiding) screenToKeepOnTop = screenToKill;
                        screenToKill = null;
                        Current = null;
                    }
                    
                    if (screenQueue.Count==0 && screenToShowInTheEnd != null) {
                        Debug("ScreenToShowInTheEnd = " + screenToShowInTheEnd);
                        screenQueue.Add(new ScreenNavigationData(screenToShowInTheEnd));
                        screenToShowInTheEnd = null;
                    }

                    var maxPriority = screenQueue.LastOrDefault();
                    BaseScreen maxPriorityScreen = maxPriority != null ? maxPriority.screen : null;

                    // Is highest-score screen different from current shown one? Then show highest-score screen and hide current
                    if (Current != maxPriorityScreen) {
                        Debug("Different? " + Current + " != " + maxPriorityScreen);

                        BaseScreen previousScreen = Current;

                        if (previousScreen != null) {
                            previousScreen.selectedObject = EventSystem.current.currentSelectedGameObject;
                            EventSystem.current.SetSelectedGameObject(null);
                        }

                        if (maxPriorityScreen.IsTransitioning) { // Wait for transition
                            Debug("Transition is busy? " + maxPriorityScreen);
                            Current = null;
                            screenQueueDirty = true;
                            yield return waitTime;
                            continue;
                        } else {
                            Debug("Transition is over! " + maxPriorityScreen);
                            Current = maxPriorityScreen;

                            if (Current == null && defaultScreen != null) Current = defaultScreen;

                            if (Current != null) {
                                Current.SetTransitionData(maxPriority.data);
                                if (onScreenShow != null) onScreenShow.Invoke(Current);
                                Current.OnActivated();
                            }

                            if (previousScreen != null) {
                                previousScreen.OnDeactivated(Current.hideCurrent);
                            }

                            if (screenToKeepOnTop != null && screenToKeepOnTop.isActiveAndEnabled) {
                                screenToKeepOnTop.transform.SetAsLastSibling();
                                screenToKeepOnTop = null;
                            }
                        }
                    }

                    screenQueueDirty = false;
                }

                if (!touchMode && alwaysOnSelection) {
                    // Make sure we're always selecting something when always-on is enabled
                    if (Current != null && !Current.IsTransitioning) {
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
        /// Hides all screens ( clearing the stack ) and shows specified screen
        /// </summary>
        public void HideAllAndShow(string screenName) {
            if (!screensList.ContainsKey(screenName)) {
                throw new KeyNotFoundException("ScreenManager: HideAllAndShow failed. Screen with name '" + screenName + "' does not exist.");
            }
            HideAllAndShow(screensList[screenName]);
        }

        /// <summary>
        /// Hides all screens ( clearing the stack ) and shows specified screen
        /// </summary>
        public void HideAllAndShow(BaseScreen screen) {
            HideAll();
            screenToShowInTheEnd = screen;
            if (screenToKill == screenToShowInTheEnd) screenToKill = null;
        }

        public void Show(string screenName)
        {
            ShowScreen(screenName);
        }

        /// <summary>
        /// Shows specified screen (with no return)
        /// </summary>
        /// <param name="screen"></param>
        public void Show(BaseScreen screen)
        {
            ShowScreen(screen);
        }

        /// <summary>
        /// Shows specified screen (with no return)
        /// </summary>
        /// <param name="screen"></param>
        public BaseScreen Show(string screenName, object data)
        {
            return ShowScreen(screenName, data);
        }

        /// <summary>
        /// Shows specified screen (with no return)
        /// </summary>
        /// <param name="screen"></param>
        public BaseScreen Show(BaseScreen screen, object data = null)
        {
            return ShowScreen(screen, data);
        }

        /// <summary>
        /// Shows specified screen
        /// </summary>
        /// <param name="screenName"></param>
        public BaseScreen ShowScreen(string screenName, object data = null, bool force = false)
        {
            if (!screensList.ContainsKey(screenName))
            {
                throw new KeyNotFoundException("ScreenManager: Show failed. Screen with name '" + screenName + "' does not exist.");
            }
            return ShowScreen(screensList[screenName], data, force);
        }

        /// <summary>
        /// Shows specified screen ( Use Show("MyScreenName"); instead )
        /// </summary>
        /// <param name="screen"></param>
        public BaseScreen ShowScreen(BaseScreen screen, object data = null, bool force = false)
        {
            if (screen == null)
            {
                throw new KeyNotFoundException("ScreenManager: Show(BaseScreen) failed, screen is Null.");
            }

            Debug("+++++++++++++   SHOW:" + screen.name);

            var lastOrDefault = screenQueue.LastOrDefault();
            //Force screen open or wait until screens are properly removed and added
            if (!force && (screenQueueDirty || (lastOrDefault != null && lastOrDefault.screen == screen)))
            {
                return screen;
            }

            screenQueue.Add(new ScreenNavigationData(screen, data));
            InsertionSort(screenQueue);

            // Is screen a higher priority and should be show this instead of current one?
            if (Current == null || (int)Current.layerPriority <= (int)screen.layerPriority)
            {
                if (Current != null) Current.OnDeactivated(false);
                screenQueueDirty = true;
            }

            return screen;
        }


        /// <summary>
        /// Hides ( or removes if it's a copy ) the current screen
        /// </summary>
        public void Hide()
        {
            if (!screenQueueDirty && Current != null && Current.IsTransitioning) return;

            screenToKill = Current;
            screenQueueDirty = true;
        }

        /// <summary>
        /// Hides all screns of specific type ( or removes them when they're a copy )
        /// </summary>
        public void HideAll()
        {

            Debug("---------------- HIDE ALL");

            foreach (var item in screenQueue)
            {
                if (item.screen == Current) continue;
                item.screen.selectedObject = null;
                item.screen.OnDeactivated(true, true);
            }
            screenQueue.Clear();

            screenToKill = Current;
            screenQueueDirty = true;
        }

         private static void InsertionSort(IList<ScreenNavigationData> list)
        {
            if (list == null) throw new ArgumentNullException("list");

            int count = list.Count;
            for (int j = 1; j < count; j++)
            {
                BaseScreen key = list[j].screen;

                int i = j - 1;
                for (; i >= 0 && CompareBaseScreens(list[i].screen, key) > 0; i--)
                {
                    list[i + 1] = list[i];
                }
                list[i + 1] = list[j];
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
            UnityEngine.Debug.Log("ScreenManager: <b>" + str + "</b>", this);
        }
    }

}
