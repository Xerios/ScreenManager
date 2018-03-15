//#define DEBUG_ScreenManager
using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;

namespace ScreenMgr {

    /// <summary>
    /// Base screen class, any custom screen class should inherit from this.
    /// Handles 
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class BaseScreen : MonoBehaviour, IComparable<BaseScreen> {
        
        /// Unique ID for all BaseScreens
        [HideInInspector]
        public readonly int ID = UniqueID<BaseScreen>.NextUID;

        [Header("Generation Settings")]
        /// <summary>
        /// Should Navigation be automatically updated for this screen?
        /// </summary>
        public bool generateNavigation = true;
        
        /// <summary>
        /// Selectable to execute on cancel
        /// </summary>
        public Selectable cancelSelection;

        [Header("Settings")]
        /// <summary>
        /// Hide previous screen or overlay on top
        /// </summary>
        public bool hideCurrent = true;

        /// <summary>
        /// Keep the screen on top of all other screens when hiding
        /// </summary>
        public bool keepOnTopWhenHiding = true;

        /// <summary>
        /// Priority over normal screens ( so that they're always on top of other screens )
        /// </summary>
        public ScreenManager.LayerPriority layerPriority = ScreenManager.LayerPriority.Normal;

        /// <summary>
        /// Default selected button/selectable item
        /// </summary>
        public Selectable defaultSelection;

        /// <summary>
        /// onShow and onHide, executed when window is shown or hidden
        /// </summary>
        [HideInInspector]
        public bool isInit = false;

        /// <summary>
        /// onShow and onHide, executed when window is shown or hidden
        /// </summary>
        [HideInInspector]
        public Action<BaseScreen> onShow,onHide;

        /// <summary>
        /// Last selected selectable object
        /// </summary>
        [HideInInspector]
        public GameObject selectedObject;

        /// <summary>
        /// Is transitioning?
        /// </summary>
        public bool IsTransitioning {
            get {
                return gameObject.activeSelf && (isTransitioningIn || isTransitioningOut);
            }
        }

        // Internal functions, DO NOT TOUCH !
        private bool isDuplicate, canBeDestroyed, isTransitioningIn, isTransitioningOut, isVisible;
        private ScreenManager screenManager;

        protected CanvasGroup canvasGroup;

        /// <summary>
        /// Initialized by ScreenManager automatically, do not do it yourself unless necessary 
        /// </summary>
        /// <param name="scrnMgr">Screen Manager</param>
        /// <param name="isCopy">Is it a copy of a previous screen?</param>
        public void Initialize(ScreenManager scrnMgr, bool isCopy = false) {
            screenManager = scrnMgr;
            isDuplicate = isCopy;
            canvasGroup = gameObject.GetComponentInChildren<CanvasGroup>();
            InteractionsEnabled(false);
            gameObject.SetActive(false);
            isVisible = false;

            // Necessary for submit and cancel calls
            var list = this.GetComponentsInChildren<CancelTrigger>(true);
            foreach (var subcancelTrigger in list) {
                //Debug.Log("subcancelTrigger " + name + " + " + subcancelTrigger + " / " + list.Length + "  -- " + submitSelection + " / " + cancelSelection, this.gameObject);
                if (cancelSelection != null) subcancelTrigger.SetCancelAction((e) => SelectOrInvokeButton(cancelSelection.gameObject, e));
            }
        }
        
        /// <summary>
        /// Executed when Show is used
        /// </summary>
        public virtual void OnShow() {
            // Do nothing...
        }

        /// <summary>
        /// Executed when Hide is used
        /// </summary>
        public virtual void OnHide() {
            // Do nothing...
        }

        /// <summary>
        /// Animation In starts ( basically when screen appears )
        /// </summary>
        public virtual void OnAnimationIn() {
            OnAnimationInEnd(); // Execute this at end of this animation
        }

        /// <summary>
        /// Animation Out starts ( basically when screen disappears )
        /// </summary>
        public virtual void OnAnimationOut() {
            OnAnimationOutEnd(); // Execute this at end of this animation
        }

        //--------------------------------------

        /// <summary>
        /// This should be executed at the end of the focus ( when screen starts ) animation ( OnAnimationIn() )
        /// </summary>
        public void OnAnimationInEnd() {
            InteractionsEnabled(true);
            isTransitioningIn = false;
            Debug("OnAnimation  In  End : " + name);
        }

        /// <summary>
        /// This should be executed at the end of the blur ( when screen loses focus ) animation ( OnAnimationOut() )
        /// </summary>
        public void OnAnimationOutEnd() {
            gameObject.SetActive(false);
            isTransitioningOut = false;
            if (isDuplicate && canBeDestroyed) Destroy(gameObject);
            if (onHide != null) onHide.Invoke(this);
            Debug("OnAnimation  Out  End : " + name);
        }

        /// <summary>
        /// DO NOT USE !
        /// Internal function executed when window loses focus
        /// </summary>
        /// <param name="hide">Should hide or disable?</param>
        /// <param name="destroy">Should destroy or desactivate?</param>
        public void OnDeactivated(bool hide, bool destroy = false) {
            Debug("BLUR ( "+ hide+" ) : "+ name);
            if (!isInit) return;

            if (hide) {
                if (destroy) canBeDestroyed = destroy;

                if (isVisible) {
                    isTransitioningOut = true;
                    InteractionsEnabled(false);
                    OnAnimationOut();
                    OnHide();
                }

                isVisible = false;
            } else if (isVisible){
                screenManager.StartCoroutine(CoroutineInteractionsEnabled(false));
            }
        }

        /// <summary>
        /// DO NOT USE !
        /// Internal function executed when window gains focus
        /// </summary>
        /// <param name="show">Should show or enable?</param>
        public void OnActivated() {
            Debug("FOCUS : " + name);

            gameObject.SetActive(true);

            if (!isVisible) {
                if (!isInit) isInit = true;

                isVisible = true;
                isTransitioningIn = true;
                OnAnimationIn();
                OnShow();
                if (onShow != null) onShow.Invoke(this);
            } else {
                screenManager.StartCoroutine(CoroutineInteractionsEnabled(true));
            }
            transform.SetAsLastSibling();
        }

        /// <summary>
        /// Changes canvas group state of interactable and blockRaycasts right after transition
        /// </summary>
        /// <param name="enabled">Should enable or disable after transition?</param>
        private IEnumerator CoroutineInteractionsEnabled(bool enabled) {

            while (IsTransitioning) {
                yield return new WaitForEndOfFrame();
            }

            canvasGroup.blocksRaycasts = enabled;
            //canvasGroup.interactable = enabled;
        
            if (enabled) yield return screenManager.StartCoroutine(CoroutineInternalSelect());
        }

        /// <summary>
        /// Changes canvas group state of interactable and blockRaycasts instantly
        /// </summary>
        /// <param name="enabled">Should enable or disable after transition?</param>
        private void InteractionsEnabled(bool enabled) {
            canvasGroup.blocksRaycasts = enabled;
            //canvasGroup.interactable = enabled;

            if (enabled) screenManager.StartCoroutine(CoroutineInternalSelect());
        }

        /// <summary>
        /// Select last selected button of this screen after one frame ( necessary )
        /// </summary>
        private IEnumerator CoroutineInternalSelect() {
            yield return new WaitForEndOfFrame();
            if (!isVisible) yield break;

            GameObject go = selectedObject != null ? selectedObject : (defaultSelection != null ? defaultSelection.gameObject : FindFirstEnabledSelectable(gameObject));
            SetSelected(go);
        }
        
        /// <summary>
        /// Finds the first Selectable element in the providade hierarchy.
        /// </summary>
        /// <param name="gameObject">GameObject to search in, looks in innactive gameobjects too</param>
        private static GameObject FindFirstEnabledSelectable(GameObject gameObject) {
            GameObject go = null;
            var selectables = gameObject.GetComponentsInChildren<Selectable>(true);
            foreach (var selectable in selectables) {
                if (selectable.IsActive() && selectable.IsInteractable()) {
                    go = selectable.gameObject;
                    break;
                }
            }
            return go;
        }

        /// <summary>
        /// Make the provided GameObject selected When using the mouse/touch we actually want to set it as the previously selected and set nothing as selected for now.
        /// </summary>
        /// <param name="go">GameObject to select</param>
        private void SetSelected(GameObject go) {
            if (screenManager.touchMode) return;
            
            //Select the GameObject.
            EventSystem.current.SetSelectedGameObject(go);

            //If we are using the keyboard right now, that's all we need to do.
            var standaloneInputModule = EventSystem.current.currentInputModule as StandaloneInputModule;
            if (standaloneInputModule != null) return;

            //Since we are using a pointer device, we don't want anything selected. 
            //But if the user switches to the keyboard, we want to start the navigation from the provided game object.
            //So here we set the current Selected to null, so the provided gameObject becomes the Last Selected in the EventSystem.
            EventSystem.current.SetSelectedGameObject(null);
        }

        /// <summary>
        /// Select Submit selectable or invoke submit if already selected
        /// </summary>
        /// <param name="e"></param>
        public void SelectOrInvokeButton(GameObject go, BaseEventData e) {
            //Debug.Log("SELECT OR USE " + go, go);
            if (!screenManager.instantCancelButton && EventSystem.current.currentSelectedGameObject != go) {
                if (!screenManager.touchMode) EventSystem.current.SetSelectedGameObject(go);
            } else {
                go.GetComponent<ISubmitHandler>().OnSubmit(e);
            }
        }

        /// <summary>
        /// Compares against other BaseScreen's priority, used for sorting
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int CompareTo(BaseScreen obj) {
            int result = 0;

            if ((int)layerPriority != (int)obj.layerPriority) {
                result = ((int)layerPriority).CompareTo((int)obj.layerPriority);
            }

            return result;
        }

        [System.Diagnostics.Conditional("DEBUG_ScreenManager")]
        private void Debug(string str) {
            UnityEngine.Debug.Log("BaseScreen: <b>" + str + "</b>", this);
        }
    }


}