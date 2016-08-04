using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using System;
using UnityEngine.Events;

namespace ScreenMgr {

    public class CancelTrigger: MonoBehaviour, ICancelHandler {

        [Header("Screen Manager Settings")]
        public bool disableCancelHandler;

        private Action<BaseEventData> cancel;

        public void SetCancelAction(Action<BaseEventData> _cancel) {
            cancel = _cancel;
        }

        public void OnCancel(BaseEventData eventData) {
            //Debug.Log("OnCancel - : " + this.name + " - " + cancel, this.gameObject);
            if (!disableCancelHandler && cancel != null) cancel.Invoke(eventData);
        }
    }
}