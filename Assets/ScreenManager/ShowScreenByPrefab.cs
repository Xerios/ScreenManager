using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace ScreenMgr
{
    public class ShowScreenByPrefab : MonoBehaviour, IPointerClickHandler
    {
        public List<ObjectPathFromEditor<BaseScreen>> screens;

        public void OnPointerClick(PointerEventData eventData)
        {
            foreach(var screen in screens)
            {
                ScreenManager.Instance.Show(screen.GetFileName());
            }
        }
    }
}