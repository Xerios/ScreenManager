using UnityEngine;
using UnityEngine.UI;

namespace ScreenMgr {

    /// <summary>
    /// A simple class to demonstrate how to make popups and alertboxes with custom data and animation
    /// </summary>
    public class Popup : BaseScreen {

        public string Message;

#pragma warning disable 0649
        [SerializeField]
        private Text textMessage;
#pragma warning restore 0649

        protected override void OnShow() {
            if (textMessage!=null) textMessage.text = Message;
        }

        private void Reset()
        {
            isPopup = true;
        }

        protected override void OnAnimationIn() {
            this.transform.GetChild(1).localScale = new Vector3(0f,0f,0f);
            LeanTween.scale(this.transform.GetChild(1).gameObject, Vector3.one, 0.5f).setEase(LeanTweenType.easeOutBack).setOnComplete(base.OnAnimationIn);
        }

        protected override void OnAnimationOut() {
            LeanTween.scale(this.transform.GetChild(1).gameObject, Vector3.zero, 0.3f).setEase(LeanTweenType.easeOutCubic).setOnComplete(base.OnAnimationOut);
        }
    }
}