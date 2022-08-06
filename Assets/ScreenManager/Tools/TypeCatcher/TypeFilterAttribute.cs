using System;
using UnityEngine;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public class TypeFilterAttribute : PropertyAttribute
{
    public Type[] types;

    public TypeFilterAttribute(params Type[] inputTypes)
    {
        types = inputTypes;
    }
}