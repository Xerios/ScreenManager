using System;
using UnityEngine;

[Serializable]
public class TypeCatcher
{
    [SerializeField] protected string typePath;

    public Type Type
    {
        get => Type.GetType(typePath);
    }
}