using System.Reflection;
using UnityEngine.SceneManagement;

namespace UnityExplorer.Utility;

internal static class SceneHandleCompat
{
    private static readonly PropertyInfo HandleProperty = typeof(Scene).GetProperty("handle", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly FieldInfo HandleField = typeof(Scene).GetField("m_Handle", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly Type HandleType = HandleProperty?.PropertyType ?? HandleField?.FieldType ?? typeof(int);
    private static readonly bool HandleIsInt = HandleType == typeof(int);

    private static readonly MethodInfo SceneToIntOperator = HandleIsInt
        ? null
        : HandleType.GetMethod("op_Implicit", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, null, [HandleType], null)
            ?? HandleType.GetMethod("op_Explicit", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, null, [HandleType], null);

    private static readonly MethodInfo IntToSceneOperator = HandleIsInt
        ? null
        : HandleType.GetMethod("op_Implicit", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, null, [typeof(int)], null)
            ?? HandleType.GetMethod("op_Explicit", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, null, [typeof(int)], null);

    private static readonly MethodInfo GetNameInternalMethod = typeof(Scene).GetMethod("GetNameInternal", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
    private static readonly Type GetNameInternalParameterType = GetNameInternalMethod?.GetParameters()?.FirstOrDefault()?.ParameterType ?? typeof(int);

    internal static int GetIntHandle(this Scene scene)
    {
        object handle = HandleProperty?.GetValue(scene) ?? HandleField?.GetValue(scene) ?? 0;
        if (HandleIsInt)
            return (int)handle;

        return SceneToIntOperator != null
            ? (int)SceneToIntOperator.Invoke(null, [handle])
            : (int)Convert.ChangeType(handle, typeof(int));
    }

    internal static Scene CreateSceneFromIntHandle(int handle)
    {
        Scene scene = default;
        object boxedScene = scene;

        object sceneHandle = handle;
        if (!HandleIsInt && IntToSceneOperator != null)
            sceneHandle = IntToSceneOperator.Invoke(null, [handle]);

        HandleField?.SetValue(boxedScene, sceneHandle);
        return (Scene)boxedScene;
    }

    internal static string GetNameFromIntHandle(int handle)
    {
        if (GetNameInternalMethod == null)
            throw new MissingMethodException("UnityEngine.SceneManagement.Scene.GetNameInternal");

        object arg = handle;
        if (GetNameInternalParameterType != typeof(int))
        {
            arg = IntToSceneOperator != null
                ? IntToSceneOperator.Invoke(null, [handle])
                : Convert.ChangeType(handle, GetNameInternalParameterType);
        }

        return (string)GetNameInternalMethod.Invoke(null, [arg]);
    }
}
