#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

using Object = UnityEngine.Object;

[CustomPropertyDrawer(typeof(ObjectPathFromEditor<>), true)]
public class ObjectPathFromEditorDrawer : PropertyDrawer
{
    private Type SearchBaseClasses(Type current, Type targetType)
    {
        while (
           current.BaseType != null
        && current.IsGenericType
        && current.GetGenericTypeDefinition() != targetType
        )
        {
            current = current.BaseType;
        }

        if (current.IsGenericType && targetType == current.GetGenericTypeDefinition())
        {
            return current;
        }

        return null;
    }

    private Type GetRealPropertyType()
    {
        if(fieldInfo.FieldType.IsArray) return fieldInfo.FieldType.GetElementType();

        var targetType = typeof(ObjectPathFromEditor<>);

        var result = SearchBaseClasses(fieldInfo.FieldType, targetType);
        if (result != null) return result;

        foreach (var genericArgument in fieldInfo.FieldType.GetGenericArguments())
        {
            result = SearchBaseClasses(genericArgument, targetType);
            if (result != null)
                return result;
        }

        return null;
    }

    private Dictionary<string, Object> loadedObject = new Dictionary<string, Object> ();
    private Type catchedRealType;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        var objectGuidProperty = property.FindPropertyRelative("objectGuid");
        var objectPathProperty = property.FindPropertyRelative("objectPath");
        var objectTypeProperty = property.FindPropertyRelative("objectType");

        if (string.IsNullOrEmpty(objectPathProperty.stringValue) && !string.IsNullOrEmpty(objectGuidProperty.stringValue))
        {
            objectPathProperty.stringValue = AssetDatabase.GUIDToAssetPath(objectPathProperty.stringValue);
        }

        if(catchedRealType == null) catchedRealType = GetRealPropertyType();
        var objectType = catchedRealType.GetGenericArguments()[0];

        var asset = TryGetObject(objectGuidProperty, objectPathProperty, objectType);

        var choosedObject = EditorGUI.ObjectField(position, label, asset, objectType, false);
        var choosedObjectType = choosedObject?.GetType();

        if (choosedObject != asset || objectTypeProperty.stringValue != choosedObjectType?.FullName)
        {
            objectPathProperty.stringValue = AssetDatabase.GetAssetPath(choosedObject);
            objectGuidProperty.stringValue = AssetDatabase.AssetPathToGUID(objectPathProperty.stringValue);
            objectTypeProperty.stringValue = choosedObjectType?.FullName;
        }

        EditorGUI.EndProperty();
    }

    private Object TryGetObject(SerializedProperty objectGuid, SerializedProperty objectPath, Type objectType)
    {
        var path = objectPath.stringValue;
        var guid = objectGuid.stringValue;

        if (loadedObject.ContainsKey(path)) return loadedObject[path];

        Object asset = null;

        if (!string.IsNullOrEmpty(path))
        {
            asset = AssetDatabase.LoadAssetAtPath(path, objectType);
            if (asset == null)
            {
                objectPath.stringValue = AssetDatabase.GUIDToAssetPath(guid);
                asset = AssetDatabase.LoadAssetAtPath(path, objectType);
            }
        }

        loadedObject[path] = asset;
        return asset;
    }
}

#endif