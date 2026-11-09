using System;

public static class ThreatQuizEvents
{
    public static event Action<string> QuizStarted;
    public static event Action<string, int, bool> AnswerSubmitted;
    public static event Action<string, int, int> QuizCompleted;
    public static event Action<string> QuizClosed;

    internal static void RaiseQuizStarted(string threatId)
    {
        QuizStarted?.Invoke(threatId);
    }

    internal static void RaiseAnswerSubmitted(string threatId, int questionIndex, bool correct)
    {
        AnswerSubmitted?.Invoke(threatId, questionIndex, correct);
    }

    internal static void RaiseQuizCompleted(string threatId, int score, int total)
    {
        QuizCompleted?.Invoke(threatId, score, total);
    }

    internal static void RaiseQuizClosed(string threatId)
    {
        QuizClosed?.Invoke(threatId);
    }
}
