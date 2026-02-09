using System;
using Microsoft.UI.Xaml.Controls;

namespace KeyPocket.UI.Helpers;

public static class NavigationHelper
{
    public static void RemovePageEntries(Frame? frame, Type pageType, Func<object?, bool> parameterMatch)
    {
        if (frame == null) return;

        for (var i = frame.BackStack.Count - 1; i >= 0; i--)
        {
            var entry = frame.BackStack[i];
            if (entry.SourcePageType == pageType && parameterMatch(entry.Parameter))
                frame.BackStack.RemoveAt(i);
        }

        for (var i = frame.ForwardStack.Count - 1; i >= 0; i--)
        {
            var entry = frame.ForwardStack[i];
            if (entry.SourcePageType == pageType && parameterMatch(entry.Parameter))
                frame.ForwardStack.RemoveAt(i);
        }
    }
}
