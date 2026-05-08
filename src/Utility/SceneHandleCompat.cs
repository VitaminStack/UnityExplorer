using System;
using System.Linq;
using System.Reflection;
using UnityEngine.SceneManagement;

namespace UnityExplorer.Utility;

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

    private static readonly MethodInfo? IntToSceneHandleOperator =
        HandleIsInt
            ? null
            : HandleType
                .GetMethods(Flags)
                .FirstOrDefault(m =>
                    (m.Name == "op_Implicit" || m.Name == "op_Explicit")
                    && m.ReturnType == HandleType
                    && m.GetParameters().Length == 1
                    && m.GetParameters()[0].ParameterType == typeof(int));

    private static readonly MethodInfo? GetNameInternalMethod =
        typeof(Scene)
            .GetMethods(Flags)
            .FirstOrDefault(m =>
                m.Name == "GetNameInternal"
                && m.IsStatic
                && m.ReturnType == typeof(string)
                && m.GetParameters().Length == 1);

    private static readonly Type GetNameInternalParameterType =
        GetNameInternalMethod?.GetParameters()[0].ParameterType ?? typeof(int);

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

    internal static Scene CreateSceneFromIntHandle(int handle)
    {
        Scene scene = default;
        object boxedScene = scene;

        object boxedHandle = BoxIntHandle(handle);

        if (HandleField == null)
            throw new MissingFieldException("UnityEngine.SceneManagement.Scene.m_Handle");

        HandleField.SetValue(boxedScene, boxedHandle);
        return (Scene)boxedScene;
    }

    internal static string? GetNameFromIntHandle(int handle)
    {
        if (GetNameInternalMethod == null)
            throw new MissingMethodException("UnityEngine.SceneManagement.Scene.GetNameInternal");

        object arg =
            GetNameInternalParameterType == typeof(int)
                ? handle
                : BoxIntHandle(handle);

        return (string?)GetNameInternalMethod.Invoke(null, new[] { arg });
    }

    private static object BoxIntHandle(int handle)
    {
        if (HandleIsInt)
            return handle;

        if (IntToSceneHandleOperator == null)
            throw new MissingMethodException($"Cannot convert int to {HandleType.FullName}.");

        return IntToSceneHandleOperator.Invoke(null, new object[] { handle })!;
    }
}