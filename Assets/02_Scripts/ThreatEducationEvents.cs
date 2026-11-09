using System;

/// <summary>
/// Decoupled integration contract between education UI, interaction, and quiz features.
/// </summary>
public static class ThreatEducationEvents
{
    public static event Action<ThreatEducationContent> PanelOpened;
    public static event Action PanelClosed;
    public static event Action<string> QuizRequested;
    public static event Action ResetRequested;

    internal static void RaisePanelOpened(ThreatEducationContent content)
    {
        PanelOpened?.Invoke(content);
    }

    internal static void RaisePanelClosed()
    {
        PanelClosed?.Invoke();
    }

    internal static void RaiseQuizRequested(string threatId)
    {
        QuizRequested?.Invoke(threatId);
    }

    internal static void RaiseResetRequested()
    {
        ResetRequested?.Invoke();
    }
}
