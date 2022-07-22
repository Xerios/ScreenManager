using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

namespace ScreenMgr {

    /// <summary>
    /// A simple class to demonstrate how to make simple tweened screen
    /// </summary>
    public class SimpleTweenScreen: BaseScreen {

        protected override void OnAnimationIn() {
            LeanTween.scale(this.transform.gameObject, Vector3.one, 0.5f).setFrom(0f).setEase(LeanTweenType.easeOutBack).setOnComplete(base.OnAnimationIn);
        }

        protected override void OnAnimationOut() {
            LeanTween.scale(this.transform.gameObject, Vector3.zero, 0.3f).setEase(LeanTweenType.easeOutCubic).setOnComplete(base.OnAnimationOut);
        }
    }

}