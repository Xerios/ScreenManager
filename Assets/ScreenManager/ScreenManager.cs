using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using ScreenMgr.Tasks;
using System.Collections;
using System.Collections.Generic;

namespace ScreenMgr
{
    [RequireComponent(typeof(Canvas), typeof(GraphicRaycaster), typeof(CanvasScaler))]
    public class ScreenManager : MonoBehaviour
    {
        public static ScreenManager Instance { get; private set; }
        public static event Action<BaseScreen> onScreenShow, onScreenHide;

        private List<BaseScreen> showingScreens = new List<BaseScreen>();
        private Dictionary<string, ObjectResourcesLoader<BaseScreen>> screensDict;

        public bool dontDestroyOnLoad;
        public string defaultScreen = null;
        public List<ObjectResourcesLoader<BaseScreen>> allScreens = new List<ObjectResourcesLoader<BaseScreen>>();

        public BaseScreen Current
        {
            get
            {
                return showingScreens.OrderBy(o => o.transform.GetSiblingIndex())
                    .Where(o => o.IsShowing)
                    .LastOrDefault();
            }
        }

        public Canvas Canvas => GetComponent<Canvas>() ?? GetComponentInParent<Canvas>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                if (dontDestroyOnLoad)
                {
                    DontDestroyOnLoad(gameObject);
                }
            }
            else Destroy(gameObject);

            Initialize();
        }

        private void OnDestroy()
        {
            Instance = null;
        }

        private void Initialize()
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                Destroy(transform.GetChild(i).gameObject);
            }

            screensDict = new Dictionary<string, ObjectResourcesLoader<BaseScreen>>();
            foreach (var screen in allScreens)
            {
                screensDict[screen.GetFileName()] = screen;
            }
            ShowDefault();
        }
        public void ShowDefault()
        {
            if (!string.IsNullOrEmpty(defaultScreen))
                Show(defaultScreen);
        }

        public bool IsShowingScreen<T>() where T : BaseScreen
        {
            return IsShowingScreen(GetScreenByType(typeof(T)));
        }

        public bool IsShowingScreen(Type type)
        {
            return IsShowingScreen(GetScreenByType(type));
        }

        public bool IsShowingScreen(string screenName)
        {
            return GetScreenByName(screenName) != null;
        }

        public bool IsShowingScreen(Type type, out BaseScreen screen)
        {
            var screenName = GetScreenByType(type);
            screen = GetScreenByName(screenName);
            return IsShowingScreen(screenName);
        }

        public bool IsShowingScreen(string screenName, out BaseScreen screen)
        {
            screen = GetScreenByName(screenName);
            return IsShowingScreen(screenName);
        }

        public bool IsShowingScreen<T>(out T screen) where T : BaseScreen
        {
            var screenName = GetScreenByType(typeof(T));
            screen = (T)GetScreenByName(screenName);
            return IsShowingScreen(screenName);
        }

        private BaseScreen GetScreenByName(string screenName)
        {
            return showingScreens.FirstOrDefault(o => o.name == screenName);
        }

        private string GetScreenByType(Type type)
        {
            if (type.Equals(typeof(BaseScreen)))
                throw new Exception(
                    $"[{nameof(ScreenManager)}] You Can't Get Screen With Type Of {nameof(BaseScreen)}");


            foreach (var iter in screensDict.Values)
            {
                if (type.IsAssignableFrom(iter.ObjectType))
                    return iter.GetFileName();
            }

            throw new KeyNotFoundException(
                $"[{nameof(ScreenManager)}] Screen With Type Of {type.Name} Could Not Found");
        }

        #region HideScreens

        public void HideAll()
        {
            foreach (var iter in new List<BaseScreen>(showingScreens))
            {
                Hide(iter);
            }
        }

        public void HideCurrent()
        {
            Hide(Current);
        }

        public void Hide(string screenName)
        {
            if (!screensDict.ContainsKey(screenName))
            {
                throw new KeyNotFoundException(
                    $"[{nameof(ScreenManager)}] Could Not Find Screen With Key {screenName}");
            }

            Hide(GetScreenByName(screenName));
        }

        public void Hide(Type type)
        {
            var screen = GetScreenByType(type);
            Hide(screen);
        }

        public void Hide<T>(object _ = null)
        {
            Hide(GetScreenByType(typeof(T)));
        }

        public void Hide(BaseScreen screen)
        {
            _coroutineQueue.Enqueue(HideScreen(screen));
        }

        private IEnumerator HideScreen(BaseScreen screen)
        {
            if (screen == null || !IsShowingScreen(screen.name)) yield break;

            while (screen.IsTransitioning)
                yield return null;

            showingScreens.Remove(screen);
            screen.DeActiveScreen();
            Resources.UnloadUnusedAssets();
            onScreenHide?.Invoke(screen);
        }

        #endregion

        #region ShowScreens

        public T Show<T>(object data = null) where T : BaseScreen
        {
            return (T)Show(typeof(T), data);
        }

        public BaseScreen Show(Type type, object data = null)
        {
            var screen = GetScreenByType(type);
            return Show(screen, data);
        }

        public BaseScreen Show(string screenName, object data = null)
        {
            if (!screensDict.ContainsKey(screenName))
            {
                throw new KeyNotFoundException(
                    $"[{nameof(ScreenManager)}] Could Not Find Screen With Key {screenName}");
            }

            var showCoroutine = ShowScreen(screenName, data);
            if (!showCoroutine.MoveNext()) return null;
            var screen = (BaseScreen)showCoroutine.Current;
            _coroutineQueue.Enqueue(showCoroutine);
            return screen;
        }

        public void ShowScreen(string screenName)
        {
            Show(screenName, null);
        }

        public BaseScreen VisualizeScreen(ObjectResourcesLoader<BaseScreen> screenLoader)
        {
            var screen = Instantiate(screenLoader.LoadObjectFromResources(), transform);
            var screenRectTransform = screen.GetComponent<RectTransform>();
            screenRectTransform.localScale = Vector3.one;

            screenRectTransform.anchorMin = Vector2.zero;
            screenRectTransform.anchorMax = Vector2.one;
            screenRectTransform.offsetMin = Vector2.zero;
            screenRectTransform.offsetMax = Vector2.zero;

            screen.gameObject.SetActive(true);
            screen.name = screenLoader.GetFileName();
            screen.Initialize(this);
            return screen;
        }

        private IEnumerator ShowScreen(string screenName, object data)
        {
            if (string.IsNullOrEmpty(screenName)) yield break;

            var screen = VisualizeScreen(screensDict[screenName]);

            yield return screen;

            var hideIfExist = IsShowingScreen(screenName, out var oldScreen) && !oldScreen.isPopup;
            if (hideIfExist)
                yield return HideScreen(oldScreen);

            while (screen.IsTransitioning)
                yield return null;

            if (screen.hideCurrent) yield return HideScreen(Current);

            if (screen.showAfterBeforeScreensDone)
            {
                var timeToCheckAnimationsAreFinished = new WaitForSecondsRealtime(1);

                while (true)
                {
                    var shouldBreak = true;

                    foreach (var iter in showingScreens)
                    {
                        if (iter == screen) break;
                        if (iter.IsTransitioning)
                        {
                            shouldBreak = false;
                            break;
                        }
                    }

                    if (shouldBreak) break;
                    yield return timeToCheckAnimationsAreFinished;
                }
            }


            showingScreens.Add(screen);
            screen.transitionData = data;
            screen.ActiveScreen();
            SortScreens();
            onScreenShow?.Invoke(screen);
        }

        #endregion


        private Queue<IEnumerator> _coroutineQueue = new Queue<IEnumerator>();
        private Task _nowRunningCoroutine;

        private void Update()
        {
            if (_coroutineQueue.Count <= 0) return;
            if(_nowRunningCoroutine == null || !_nowRunningCoroutine.Running)
            {
                _nowRunningCoroutine = new Task(_coroutineQueue.Dequeue());
            }
        }

        public void SortScreens()
        {
            List<BaseScreen> screens;

            foreach (var _ in Enumerable.Range(0, showingScreens.Count))
            {
                foreach (var screenIndex in Enumerable.Range(0, showingScreens.Count - 1))
                {
                    screens = showingScreens.OrderBy(o => o.transform.GetSiblingIndex()).ToList();
                    var first = screens[screenIndex];

                    if (!first.IsShowing) continue;

                    var second = screens[screenIndex + 1];
                    if (first.SortingOrder > second.SortingOrder)
                        first.transform.SetSiblingIndex(first.transform.GetSiblingIndex() + 1);
                }
            }
        }
    }
}
