using System;
using System.Collections.Generic;

namespace IHaveTooMuchItemBloat;

internal static class Failsafe
{
    private static readonly HashSet<string> ReportedFailures = [];

    public static void Run(string task, Action action)
    {
        try
        {
            action();
        }
        catch (Exception exception)
        {
            Report(task, exception);
        }
    }

    public static T Run<T>(string task, Func<T> action, T fallback)
    {
        try
        {
            return action();
        }
        catch (Exception exception)
        {
            Report(task, exception);
            return fallback;
        }
    }

    public static Action<T> Handler<T>(string task, Action<T> handler) => argument => Run(task, () => handler(argument));

    private static void Report(string task, Exception exception)
    {
        if (ReportedFailures.Add(task + exception.GetType().FullName))
            IHaveTooMuchItemBloatPlugin.Log.LogError($"Failed {task}: {exception}");
    }
}
