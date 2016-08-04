using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ScreenMgr {

    /// <summary>
    /// Screen animated using animator
    /// Use "In" and "Out" as animation names or set your custom ones
    /// </summary>
    public class AnimatorScreen : BaseScreen {

        public Animator animator;
        public string animationIn = "In", animationOut = "Out";

        private IEnumerator coroutineIn = null,couroutineOut = null;

        public override void OnAnimationIn() {
            if (couroutineOut != null) {
                StopCoroutine(couroutineOut);
            }
            coroutineIn = CoroutineIn();
            StartCoroutine(coroutineIn);
        }

        public override void OnAnimationOut() {
            if (coroutineIn != null) {
                StopCoroutine(coroutineIn);
            }
            couroutineOut = CoroutineOut();
            StartCoroutine(couroutineOut);
        }

        IEnumerator CoroutineIn() {
            animator.Play(animationIn, -1, 0f); // Start anim
            yield return null; // Leave one frame to get proper animation length
            yield return new WaitForSeconds(animator.GetCurrentAnimatorStateInfo(0).length); // Wait until animation is finished...
            OnAnimationInEnd(); // Execute this at end of this animation
        }

        IEnumerator CoroutineOut() {
            animator.Play(animationOut, -1, 0f); // Start anim
            yield return null; // Leave one frame to get proper animation length
            yield return new WaitForSeconds(animator.GetCurrentAnimatorStateInfo(0).length);// Wait until animation is finished...
            OnAnimationOutEnd(); // Execute this at end of this animation
        }
    }


}