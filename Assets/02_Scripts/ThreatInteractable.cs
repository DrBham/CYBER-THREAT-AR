using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime interaction state for one spawned threat model.
/// </summary>
public class ThreatInteractable : MonoBehaviour
{
    [Header("Highlight")]
    [SerializeField] private Color highlightColor = new Color(0.1f, 0.9f, 1f, 1f);
    [SerializeField, Range(0f, 1f)] private float highlightBlend = 0.45f;

    [Header("Scale Limits")]
    [SerializeField, Min(0.1f)] private float minimumScaleMultiplier = 0.6f;
    [SerializeField, Min(0.1f)] private float maximumScaleMultiplier = 1.8f;

    [Header("Tap Target")]
    [SerializeField, Min(0f)] private float colliderPadding = 0.005f;

    private readonly List<Renderer> renderers = new List<Renderer>();
    private ThreatInteractionManager manager;
    private Vector3 initialLocalPosition;
    private Quaternion initialLocalRotation;
    private Vector3 initialLocalScale;
    private float scaleMultiplier = 1f;
    private BoxCollider interactionCollider;
    private bool initialized;
    private bool selected;

    public string ThreatId { get; private set; }
    public bool IsSelected => selected;
    public BoxCollider InteractionCollider => interactionCollider;
    public float ScaleMultiplier => scaleMultiplier;

    public void Initialize(string threatId, ThreatInteractionManager interactionManager)
    {
        ThreatId = threatId;
        manager = interactionManager;

        initialLocalPosition = transform.localPosition;
        initialLocalRotation = transform.localRotation;
        initialLocalScale = transform.localScale;
        scaleMultiplier = 1f;

        CacheRenderers();
        ConfigureInteractionCollider();

        initialized = true;
        if (manager != null)
            manager.Register(this);
    }

    public void SetSelected(bool value)
    {
        if (selected == value)
            return;

        selected = value;
        ApplyHighlight(value);
    }

    public void ApplyScaleFactor(float factor)
    {
        if (!initialized || factor <= 0f || float.IsNaN(factor) || float.IsInfinity(factor))
            return;

        scaleMultiplier = Mathf.Clamp(
            scaleMultiplier * factor,
            minimumScaleMultiplier,
            maximumScaleMultiplier);

        transform.localScale = Vector3.Scale(
            initialLocalScale,
            Vector3.one * scaleMultiplier);
    }

    public void RotateAroundMarkerNormal(float degrees)
    {
        if (!initialized || Mathf.Approximately(degrees, 0f))
            return;

        transform.localRotation = Quaternion.AngleAxis(degrees, Vector3.up)
            * transform.localRotation;
    }

    public void ResetInteractionTransform()
    {
        if (!initialized)
            return;

        transform.localPosition = initialLocalPosition;
        transform.localRotation = initialLocalRotation;
        transform.localScale = initialLocalScale;
        scaleMultiplier = 1f;
        SetSelected(false);
    }

    private void CacheRenderers()
    {
        renderers.Clear();
        Renderer[] children = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] != null)
                renderers.Add(children[i]);
        }
    }

    private void ConfigureInteractionCollider()
    {
        interactionCollider = GetComponent<BoxCollider>();
        if (interactionCollider == null)
            interactionCollider = gameObject.AddComponent<BoxCollider>();

        bool hasBounds = false;
        Bounds localBounds = default;

        for (int i = 0; i < renderers.Count; i++)
        {
            Renderer targetRenderer = renderers[i];
            if (!targetRenderer.enabled || !targetRenderer.gameObject.activeInHierarchy)
                continue;

            Bounds worldBounds = targetRenderer.bounds;
            Vector3 min = worldBounds.min;
            Vector3 max = worldBounds.max;

            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 worldCorner = new Vector3(
                    (corner & 1) == 0 ? min.x : max.x,
                    (corner & 2) == 0 ? min.y : max.y,
                    (corner & 4) == 0 ? min.z : max.z);
                Vector3 localCorner = transform.InverseTransformPoint(worldCorner);

                if (!hasBounds)
                {
                    localBounds = new Bounds(localCorner, Vector3.zero);
                    hasBounds = true;
                }
                else
                {
                    localBounds.Encapsulate(localCorner);
                }
            }
        }

        if (!hasBounds)
            localBounds = new Bounds(Vector3.zero, Vector3.one * 0.02f);

        Vector3 paddedSize = localBounds.size + Vector3.one * (colliderPadding * 2f);
        paddedSize.x = Mathf.Max(paddedSize.x, 0.02f);
        paddedSize.y = Mathf.Max(paddedSize.y, 0.02f);
        paddedSize.z = Mathf.Max(paddedSize.z, 0.02f);

        interactionCollider.center = localBounds.center;
        interactionCollider.size = paddedSize;
        interactionCollider.isTrigger = false;
    }

    private void ApplyHighlight(bool enabled)
    {
        MaterialPropertyBlock block = new MaterialPropertyBlock();

        for (int rendererIndex = 0; rendererIndex < renderers.Count; rendererIndex++)
        {
            Renderer targetRenderer = renderers[rendererIndex];
            if (targetRenderer == null)
                continue;

            Material[] materials = targetRenderer.sharedMaterials;
            for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
            {
                if (!enabled)
                {
                    targetRenderer.SetPropertyBlock(null, materialIndex);
                    continue;
                }

                Material material = materials[materialIndex];
                if (material == null)
                    continue;

                block.Clear();
                targetRenderer.GetPropertyBlock(block, materialIndex);

                Color baseColor = Color.white;
                if (material.HasProperty("_BaseColor"))
                    baseColor = material.GetColor("_BaseColor");
                else if (material.HasProperty("_Color"))
                    baseColor = material.GetColor("_Color");

                Color tintedColor = Color.Lerp(baseColor, highlightColor, highlightBlend);
                if (material.HasProperty("_BaseColor"))
                    block.SetColor("_BaseColor", tintedColor);
                if (material.HasProperty("_Color"))
                    block.SetColor("_Color", tintedColor);
                if (material.HasProperty("_EmissionColor"))
                    block.SetColor("_EmissionColor", highlightColor * 0.65f);

                targetRenderer.SetPropertyBlock(block, materialIndex);
            }
        }
    }

    private void OnDisable()
    {
        if (initialized && manager != null)
            manager.NotifyUnavailable(this);
    }

    private void OnDestroy()
    {
        if (manager != null)
            manager.Unregister(this);
    }
}
