using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ScreenMgr
{
    public class HideScreenOfType : MonoBehaviour, IPointerClickHandler
    {
        [TypeFilter(typeof(BaseScreen))]
        public TypeCatcher[] screens;

        public void OnPointerClick(PointerEventData eventData)
        {
            foreach (var screen in screens)
                HideScreen(screen.Type);
        }

        private void HideScreen(Type screenType)
        {
            ScreenManager.Instance.Hide(screenType);
        }
    }
}