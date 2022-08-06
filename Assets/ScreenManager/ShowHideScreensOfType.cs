using UnityEngine;
using UnityEngine.EventSystems;

#if UNITY_EDITOR

using UnityEditor;
namespace ScreenMgr.Editors
{
    [CustomEditor(typeof(ShowHideScreensOfType))]
    public class ShowHideScreensOfTypeEditor : Editor
    {

        private void Awake()
        {
            Undo.RegisterCompleteObjectUndo(target, nameof(ShowHideScreensOfType) + " Undo");
        }

        public override void OnInspectorGUI()
        {
            var targetCasted = (ShowHideScreensOfType)target;

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
                Undo.RecordObject(target, nameof(ShowHideScreensOfType) + " Undo");
            }
        }
    }
}

#endif

namespace ScreenMgr
{
    public class ShowHideScreensOfType : MonoBehaviour, IPointerClickHandler
    {
        public bool hideAll;

        [TypeFilter(typeof(BaseScreen))]
        public TypeCatcher[] hideScreens;

        [TypeFilter(typeof(BaseScreen))]
        public TypeCatcher[] showScreens;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (hideAll) ScreenManager.Instance.HideAll();
            else
            {
                foreach (var screen in hideScreens)
                    ScreenManager.Instance.Hide(screen.Type);
            }

            foreach (var screen in showScreens)
                ScreenManager.Instance.Show(screen.Type);

        }

    }
}