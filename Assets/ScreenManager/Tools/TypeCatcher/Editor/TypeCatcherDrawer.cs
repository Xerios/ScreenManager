#if UNITY_EDITOR

using System;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

[CustomPropertyDrawer(typeof(TypeCatcher), true)]
[CustomPropertyDrawer(typeof(TypeFilterAttribute))]
public class TypeCatcherDrawer : PropertyDrawer
{
    private static List<Type> loadedTypes = new List<Type>();
    private List<Type> loadingTypes = new List<Type>();
    private IEnumerator getTypesCoroutine;


    public TypeCatcherDrawer() : base()
    {
        EditorApplication.update += Update;
        getTypesCoroutine = LoadAssemblies(loadedTypes.Count > 0);
    }

    ~TypeCatcherDrawer()
    {
        EditorApplication.update -= Update;
    }

    private void Update()
    {
        if (getTypesCoroutine == null && (EditorApplication.isCompiling || EditorApplication.isUpdating))
            getTypesCoroutine = LoadAssemblies();

        if (getTypesCoroutine != null && !getTypesCoroutine.MoveNext()) getTypesCoroutine = null;
    }

    IEnumerator LoadAssemblies(bool async = true)
    {
        while (EditorApplication.isCompiling || EditorApplication.isUpdating)
            if (async)
                yield return null;

        loadingTypes.Clear();

        foreach (var _iter in AppDomain.CurrentDomain.GetAssemblies())
        {
            loadingTypes.AddRange(_iter.GetTypes());
            if (async)
                yield return null;
        }

        loadedTypes.Clear();
        loadedTypes.AddRange(loadingTypes);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.type != nameof(TypeCatcher)) return;

        EditorGUI.BeginProperty(position, label, property);

        var typePath = property.FindPropertyRelative("typePath");
        var typeSelf = Type.GetType(typePath.stringValue);
        var filterTypes = (attribute as TypeFilterAttribute)?.types;
        var finalTypes = new List<Type>();

        if (filterTypes != null)
        {
            foreach (var filterType in filterTypes)
            {
                finalTypes.AddRange(loadedTypes.Where(o => filterType.IsAssignableFrom(o)));
            }
        }
        else finalTypes.AddRange(loadedTypes);

        finalTypes = finalTypes.OrderBy(o => o.Name, StringComparer.CurrentCultureIgnoreCase).ToList();

        var typeIndex = finalTypes.IndexOf(typeSelf);
        var typesNames = finalTypes.Select(o => new GUIContent(o.Name)).ToArray();

        var newTypeIndex = EditorGUI.Popup(position, label, typeIndex, typesNames);

        if(newTypeIndex != typeIndex)
        {
            typePath.stringValue = finalTypes[newTypeIndex].FullName;
        }

        EditorGUI.EndProperty();
    }
}

#endif
