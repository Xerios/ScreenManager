using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ScreenMgr
{
    public class ShowScreenOfType : MonoBehaviour, IPointerClickHandler
    {
        [TypeFilter(typeof(BaseScreen))]
        public TypeCatcher[] screens;

        public void OnPointerClick(PointerEventData eventData)
        {
            foreach (var screen in screens)
                ShowScreen(screen.Type);
        }

        private void ShowScreen(Type type)
        {
            ScreenManager.Instance.Show(type);
        }

    }
}