using System;
using System.Collections.Generic;
using System.Reflection;
using MonoMod.RuntimeDetour;

namespace IHaveTooMuchItemBloat;

internal static class Hooks
{
    private const BindingFlags AnyMethod = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    private static readonly List<Hook> Installed = [];

    public static bool Install(Type type, string methodName, Type[] parameterTypes, Delegate hook)
    {
        var target = $"{type.FullName}.{methodName}({string.Join(", ", (object[])parameterTypes)})";
        var method = type.GetMethod(methodName, AnyMethod, null, parameterTypes, null);

        if (method == null)
        {
            IHaveTooMuchItemBloatPlugin.Log.LogError($"Hook target not found: {target}.");
            return false;
        }

        try
        {
            Installed.Add(new Hook(method, hook));
            return true;
        }
        catch (Exception exception)
        {
            IHaveTooMuchItemBloatPlugin.Log.LogError($"Failed to hook {target}: {exception}");
            return false;
        }
    }
}
