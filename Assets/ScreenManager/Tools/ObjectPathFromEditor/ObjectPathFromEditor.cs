using System;
using UnityEngine;
using System.Text.RegularExpressions;

using Object = UnityEngine.Object;

[Serializable]
public class ObjectPathFromEditor<TObject> where TObject : Object
{
    //Do Not Rename
    [SerializeField] private string objectGuid;
    [SerializeField] private string objectPath;
    [SerializeField] private string objectType;


    public Type ObjectType => Type.GetType(objectType);
    public string GUID => objectGuid;
    public string ObjectPath => objectPath;

    //Do Not Rename
    public string GetFileName(bool withExtention = false)
    {
        if (string.IsNullOrEmpty(objectPath)) return string.Empty;

        var extentionPart = withExtention ? "" : @"\.";
        var regexResult = new Regex(@".+\/(.+)" + extentionPart).Match(objectPath).Groups;
        if (regexResult.Count > 1)
            return regexResult[1].Value;
        return null;
    }
}