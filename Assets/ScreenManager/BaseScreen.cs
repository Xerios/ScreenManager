using System;
using UnityEngine;
using UnityEngine.Events;

namespace ScreenMgr
{
    [RequireComponent(typeof(CanvasGroup))]
    public class BaseScreen : MonoBehaviour
    {
        [Space(30)]
        public UnityEvent<BaseScreen> onShow, onHide;
        public bool hideCurrent;
        public bool showAfterBeforeScreensDone = true;
        public bool getControlOfRaycastChatching;
        public bool isPopup;

        [HideInInspector]
        public object transitionData;

        [HideInInspector]
        public CanvasGroup canvasGroup;

        [SerializeField] private int _sortingOrder;

        private ScreenManager _screenManager;
        private bool _isTransitioningIn, _isTransitioningOut;

        public bool IsTransitioning => _isTransitioningIn || _isTransitioningOut;
        public bool IsShowing { get; private set; }

        public int SortingOrder
        {
            get { return _sortingOrder; }
            set
            {
                _sortingOrder = value;
                _screenManager.SortScreens();
            }
        }

        public void Initialize(ScreenManager screenManager)
        {
            _screenManager = screenManager;
            canvasGroup = GetComponent<CanvasGroup>();

            ScreenManager.onScreenShow += OnScreensChanged;
            ScreenManager.onScreenHide += OnScreensChanged;
        }

        private void OnDestroy()
        {
            ScreenManager.onScreenShow -= OnScreensChanged;
            ScreenManager.onScreenHide -= OnScreensChanged;
        }

        private void OnScreensChanged(BaseScreen screen)
        {
            if (getControlOfRaycastChatching)
                canvasGroup.blocksRaycasts = _screenManager.Current == this;
        }

        public void ActiveScreen()
        {
            IsShowing = true;
            _isTransitioningIn = true;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            OnAnimationIn();
            OnShow();
        }

        public void DeActiveScreen()
        {
            IsShowing = false;
            _isTransitioningOut = true;
            OnAnimationOut();
            OnHide();
            Destroy(gameObject);
        }

        protected virtual void OnShow()
        {
            //Do Nothing...
        }

        protected virtual void OnHide()
        {
            //Do Nothing...
        }

        protected virtual void OnAnimationIn()
        {
            _isTransitioningIn = false;
            onShow?.Invoke(this);
        }

        protected virtual void OnAnimationOut()
        {
            _isTransitioningOut = false;
            transform.SetAsFirstSibling();
            onHide?.Invoke(this);
        }

        public void HideScreen() => _screenManager.Hide(this);
        public void HideScreen(string screenName) => _screenManager.Hide(screenName);
        public void HideScreen<T>(object data = null) where T : BaseScreen => _screenManager.Hide<T>(data);
        public void ShowSreen(string screenName) => _screenManager.Show(screenName);
        public BaseScreen ShowScreen(string screenName, object data = null) => _screenManager.Show(screenName, data);
        public BaseScreen ShowScreen(Type type, object data = null) => _screenManager.Show(type, data);
        public T ShowScreen<T>(object data = null) where T : BaseScreen => _screenManager.Show<T>(data);
        public bool IsShowingScreen(string screenName) => _screenManager.IsShowingScreen(screenName);
        public bool IsShowingScreen(string screenName, out BaseScreen screen) => _screenManager.IsShowingScreen(screenName, out screen);
        public bool IsShowingScreen<T>() where T : BaseScreen => _screenManager.IsShowingScreen<T>();
        public bool IsShowingScreen<T>(out T screen) where T : BaseScreen => _screenManager.IsShowingScreen<T>(out screen);
        public void HideAll() => _screenManager.HideAll();
        public void ShowDefault() => _screenManager.ShowDefault();
    }
}