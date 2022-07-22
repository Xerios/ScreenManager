using System;
using UnityEngine;
using System.Text.RegularExpressions;

using Object = UnityEngine.Object;

[Serializable]
public class ObjectResourcesLoader<TObject> : ObjectPathFromEditor<TObject> where TObject : Object
{
    private readonly Regex _resourcePathRegex = new Regex(@".+\/Resources\/(.+)\.", RegexOptions.IgnoreCase);

    public string ResourcesPath
    {
        get
        {
            return _resourcePathRegex.Match(ObjectPath).Groups[1].Value;
        }
    }

    public TObject Asset { get; private set; }

    public TObject LoadObjectFromResources()
    {
        Asset = Resources.Load<TObject>(ResourcesPath);
        if (Asset == null)
        {
            throw new Exception($"Failed To Load Asset {GetFileName()}, Make Sure Asset Is Located At <color=green>Assets/Resources</color>");
        }

        return Asset;
    }

    public void LoadObjectFromResourcesAsync(Action<TObject> onComplete)
    {
        var request = Resources.LoadAsync<TObject>(ResourcesPath);
        request.completed += res =>
        {
            if (request.asset == null)
            {
                throw new Exception($"Failed To Load Asset {GetFileName()}, Make Sure Asset Is Located At <color=green>Assets/Resources</color>");
            }

            Asset = (TObject)request.asset;
            onComplete?.Invoke(Asset);
        };
    }

    public void UnloadObject()
    {
        Resources.UnloadAsset(Asset);
        Asset = null;
    }
}