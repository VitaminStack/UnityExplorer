using System;
using System.Linq;
using System.Reflection;
using UnityEngine.SceneManagement;

namespace UniverseLib;

internal static class SceneHandleCompat
{
    private const BindingFlags Flags =
        BindingFlags.Public |
        BindingFlags.NonPublic |
        BindingFlags.Static |
        BindingFlags.Instance;

    private static readonly PropertyInfo? HandleProperty =
        typeof(Scene).GetProperty("handle", Flags);

    private static readonly FieldInfo? HandleField =
        typeof(Scene).GetField("m_Handle", Flags);

    private static readonly Type HandleType =
        HandleProperty?.PropertyType
        ?? HandleField?.FieldType
        ?? typeof(int);

    private static readonly bool HandleIsInt = HandleType == typeof(int);

    private static readonly MethodInfo? SceneHandleToIntOperator =
        HandleIsInt
            ? null
            : HandleType
                .GetMethods(Flags)
                .FirstOrDefault(m =>
                    (m.Name == "op_Implicit" || m.Name == "op_Explicit")
                    && m.ReturnType == typeof(int)
                    && m.GetParameters().Length == 1
                    && m.GetParameters()[0].ParameterType == HandleType);

    internal static int GetIntHandle(this Scene scene)
    {
        object? handle =
            HandleProperty?.GetValue(scene, null)
            ?? HandleField?.GetValue(scene);

        if (handle == null)
            return 0;

        if (HandleIsInt)
            return (int)handle;

        if (SceneHandleToIntOperator == null)
            throw new MissingMethodException($"Cannot convert {HandleType.FullName} to int.");

        return (int)SceneHandleToIntOperator.Invoke(null, new[] { handle })!;
    }
}