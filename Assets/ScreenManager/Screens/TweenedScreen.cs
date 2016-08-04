using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

namespace ScreenMgr {

    /// <summary>
    /// A simple class to demonstrate custom tweened screen
    /// </summary>
    public class TweenedScreen : BaseScreen {
        
        public override void OnAnimationIn() {
            canvasGroup.alpha = 1f;

            Transform[] allChildren = GetComponentsInChildren<RectTransform>();

            foreach (var item in allChildren) {
                if (item.transform == this.transform) continue;
                item.localScale = new Vector3(0f, 0f, 0f);
                LeanTween.scale(item.gameObject, Vector3.one, Random.Range(0.2f,0.7f)).setDelay(Random.Range(0f, 0.6f)).setEase(LeanTweenType.easeOutBack);
            }

            LeanTween.delayedCall(0.5f, OnAnimationInEnd);
        }

        public override void OnAnimationOut() {


            LeanTween.value(this.gameObject,(x)=> canvasGroup.alpha = x, 1f, 0f, 0.5f).setEase(LeanTweenType.easeOutCubic).setOnComplete(OnAnimationOutEnd);
        }
    }

}