// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;

namespace KeyPocket.UI.Controls;

public sealed partial class CopyButton : Button
{
    public static readonly DependencyProperty CopiedMessageProperty =
        DependencyProperty.Register("CopiedMessage", typeof(string), typeof(CopyButton),
            new PropertyMetadata("Copied to clipboard"));

    public CopyButton()
    {
        DefaultStyleKey = typeof(CopyButton);
    }

    public string CopiedMessage
    {
        get => (string)GetValue(CopiedMessageProperty);
        set => SetValue(CopiedMessageProperty, value);
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        if (GetTemplateChild("CopyToClipboardSuccessAnimation") is Storyboard _storyBoard)
        {
            _storyBoard.Begin();
            AnnounceActionForAccessibility(this, CopiedMessage, "CopiedToClipboardActivityId");
        }
    }

    // Confirmation of Action
    public static void AnnounceActionForAccessibility(UIElement ue, string announcement, string activityID)
    {
        if (FrameworkElementAutomationPeer.FromElement(ue) is AutomationPeer peer)
            peer.RaiseNotificationEvent(AutomationNotificationKind.ActionCompleted,
                AutomationNotificationProcessing.ImportantMostRecent, announcement, activityID);
    }

    protected override void OnApplyTemplate()
    {
        Click -= CopyButton_Click;
        base.OnApplyTemplate();
        Click += CopyButton_Click;
    }
}