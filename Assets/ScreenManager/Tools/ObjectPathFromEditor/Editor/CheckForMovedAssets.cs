#if UNITY_EDITOR

using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class CheckForMovedAssets : AssetPostprocessor
{

    private static Scene ActiveScene => EditorSceneManager.GetActiveScene();

    static Type GetSerializedPropertyType(SerializedProperty property)
    {
        string[] slices = property.propertyPath.Split('.');
        Type type = property.serializedObject.targetObject.GetType();

        for (int i = 0; i < slices.Length; i++)
        {
            if (slices[i] == "Array")
            {
                i++; //skips "data[x]"
                type = type?.GetElementType() ?? (type != null && type.IsGenericType ? type.GetGenericArguments()[0] : null); //gets info on array elements
            }

            //gets info on field and its type
            else type = type?.GetRuntimeField(slices[i])?.FieldType;
        }

        if (type != null && type.IsGenericType)
            type = type.GetGenericTypeDefinition();

        return type;
    }

    static bool IsSubclassOfRawGeneric(Type parent, Type child)
    {
        while (child != null && child != typeof(object))
        {
            var cur = child.IsGenericType ? child.GetGenericTypeDefinition() : child;
            if (parent == cur)
            {
                return true;
            }
            child = child.BaseType;
        }
        return false;
    }

    static async void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        var wasSceneDirty = ActiveScene.isDirty;

        foreach (var file in AssetDatabase.FindAssets("t:prefab t:scene t:scriptableobject").Select(o => AssetDatabase.GUIDToAssetPath(o)))
        {
            var isDirty = false;

            if (file == ActiveScene.path)
            {
                DoSceneChanges(movedAssets, movedFromAssetPaths, ref isDirty);

                if (isDirty)
                {
                    if (wasSceneDirty)
                    {
                        var userChoosed = EditorUtility.DisplayDialogComplex(
                                      "ObjectPathFromEditor Properties Are Updated",
                                      "ObjectPathFromEditor Properties Are Update Due To Moving Some Assets, You Better To Save Current Scene",
                                      "Save Scene", "Cancel", "Discard Changes");

                        if (userChoosed == 1)
                        {
                            continue;
                        }
                        else if (userChoosed == 2)
                        {
                            EditorSceneManager.OpenScene(ActiveScene.path);
                            await Task.Yield();
                            DoSceneChanges(movedAssets, movedFromAssetPaths, ref isDirty);
                        }
                    }

                    await Task.Yield();
                    EditorSceneManager.SaveScene(ActiveScene);
                    await Task.Yield();
                    EditorSceneManager.OpenScene(ActiveScene.path);
                    continue;
                }
            }

            try
            {
                var fileContent = File.ReadAllText(file);
                for (int i = 0; i < movedFromAssetPaths.Length; i++)
                {
                    if (fileContent.Contains(movedFromAssetPaths[i]))
                    {
                        fileContent = fileContent.Replace(movedFromAssetPaths[i], movedAssets[i]);
                        isDirty = true;
                    }
                }

                if (isDirty)
                {
                    File.WriteAllText(file, fileContent);
                }
            }
            catch (Exception) { }
        }
    }

    private static void DoSceneChanges(string[] movedAssets, string[] movedFromAssetPaths, ref bool isDirty)
    {
        //Do Scene Changes
        var components = ActiveScene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<MonoBehaviour>());
        var serializedObjects = components.Where(o => o != null).Select(o => new SerializedObject(o)).ToList();

        foreach (var serializedObject in serializedObjects)
        {
            var iterator = serializedObject.GetIterator();
            while (iterator.Next(true))
            {
                if (IsSubclassOfRawGeneric(typeof(ObjectPathFromEditor<>), GetSerializedPropertyType(iterator)))
                {
                    var objectPath = iterator.FindPropertyRelative("objectPath");
                    var index = movedFromAssetPaths.ToList().IndexOf(objectPath?.stringValue);
                    if (objectPath != null && index != -1)
                    {
                        objectPath.stringValue = movedAssets[index];
                        serializedObject.ApplyModifiedProperties();
                        isDirty = true;
                    }
                }
            }

            serializedObject.Dispose();
        }
    }
}

#endif