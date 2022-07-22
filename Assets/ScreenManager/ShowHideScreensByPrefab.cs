using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

#if UNITY_EDITOR

using UnityEditor;
namespace ScreenMgr.Editors
{
    [CustomEditor(typeof(ShowHideScreensByPrefab))]
    public class ShowHideScreensByPrefabEditor : Editor
    {

        private void Awake()
        {
            Undo.RegisterCompleteObjectUndo(target, nameof(ShowHideScreensByPrefab) + " Undo");
        }

        public override void OnInspectorGUI()
        {
            var targetCasted = (ShowHideScreensByPrefab)target;

            EditorGUI.BeginChangeCheck();

            GUILayout.Space(10);

            targetCasted.hideAll = EditorGUILayout.Toggle("Hide All", targetCasted.hideAll);
            GUILayout.Space(10);

            if (!targetCasted.hideAll)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(targetCasted.hideScreens)));
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(targetCasted.showScreens)));

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(targetCasted);
                Undo.RecordObject(target, nameof(ShowHideScreensByPrefab) + " Undo");
            }
        }
    }
}

#endif

namespace ScreenMgr
{
    public class ShowHideScreensByPrefab : MonoBehaviour, IPointerClickHandler
    {
        public bool hideAll;
        public List<ObjectPathFromEditor<BaseScreen>> hideScreens;
        public List<ObjectPathFromEditor<BaseScreen>> showScreens;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (hideAll) ScreenManager.Instance.HideAll();
            else
            {
                foreach (var screen in hideScreens)
                {
                    ScreenManager.Instance.Hide(screen.GetFileName());
                }
            }

            foreach (var screen in showScreens)
            {
                ScreenManager.Instance.Show(screen.GetFileName());
            }
        }
    }
}