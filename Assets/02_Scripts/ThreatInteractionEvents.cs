using System;
using UnityEngine;

/// <summary>
/// Integration point for education UI and other features that react to threat interaction.
/// UI code can subscribe without depending on the image-tracking implementation.
/// </summary>
public static class ThreatInteractionEvents
{
    public static event Action<string, GameObject> ThreatTapped;
    public static event Action<string, GameObject> ThreatSelected;
    public static event Action<string, GameObject> ThreatDeselected;

    internal static void RaiseTapped(ThreatInteractable target)
    {
        ThreatTapped?.Invoke(target.ThreatId, target.gameObject);
    }

    internal static void RaiseSelected(ThreatInteractable target)
    {
        ThreatSelected?.Invoke(target.ThreatId, target.gameObject);
    }

    internal static void RaiseDeselected(ThreatInteractable target)
    {
        ThreatDeselected?.Invoke(target.ThreatId, target.gameObject);
    }
}
