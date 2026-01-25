using CommunityToolkit.Mvvm.Messaging.Messages;

namespace KeyPocket.UI.Messages;

/// <summary>
/// Message sent when the application theme changes
/// </summary>
public class ThemeChangedMessage : ValueChangedMessage<bool>
{
    // value: true = Dark, false = Light
    public ThemeChangedMessage(bool isDark) : base(isDark)
    {
    }
}
