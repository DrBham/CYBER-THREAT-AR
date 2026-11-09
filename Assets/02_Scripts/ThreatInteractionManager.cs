using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Handles tap selection, highlight, pinch scaling, and two-finger twist rotation.
/// Added to XR Origin at runtime by ThreatImageTracker.
/// </summary>
public class ThreatInteractionManager : MonoBehaviour
{
    [Header("Raycast")]
    [SerializeField] private Camera interactionCamera;
    [SerializeField, Min(0.1f)] private float maximumRayDistance = 20f;
    [SerializeField, Min(0f)] private float tapMovementThreshold = 24f;

    [Header("Gesture")]
    [SerializeField, Min(0.01f)] private float pinchSensitivity = 1f;
    [SerializeField, Min(0.01f)] private float twistSensitivity = 1f;

    [Header("Editor Mouse Fallback")]
    [SerializeField, Min(0.01f)] private float mouseScaleStep = 0.12f;
    [SerializeField, Min(0.01f)] private float mouseRotationSensitivity = 2f;

    private readonly List<ThreatInteractable> registeredTargets
        = new List<ThreatInteractable>();
    private readonly RaycastHit[] raycastHits = new RaycastHit[32];

    private ThreatInteractable selectedTarget;
    private int tapFingerId = -1;
    private Vector2 tapStartPosition;
    private bool tapMoved;
    private bool twoFingerGestureActive;

    public static ThreatInteractionManager Instance { get; private set; }
    public ThreatInteractable SelectedTarget => selectedTarget;
    public int RegisteredTargetCount => registeredTargets.Count;

    private void Awake()
    {
        if (Instance == null || Instance == this)
            Instance = this;

        ResolveCamera();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (selectedTarget != null && !selectedTarget.gameObject.activeInHierarchy)
            DeselectCurrent();

        HandleTouchInput();
        HandleMouseInput();
    }

    public void SetInteractionCamera(Camera targetCamera)
    {
        interactionCamera = targetCamera;
    }

    public void Register(ThreatInteractable target)
    {
        if (target != null && !registeredTargets.Contains(target))
            registeredTargets.Add(target);
    }

    public void Unregister(ThreatInteractable target)
    {
        if (target == null)
            return;

        if (selectedTarget == target)
            DeselectCurrent();

        registeredTargets.Remove(target);
    }

    public void NotifyUnavailable(ThreatInteractable target)
    {
        if (selectedTarget == target)
            DeselectCurrent();
    }

    public void SelectInteractable(ThreatInteractable target, bool notifyTap)
    {
        if (target == null || !target.gameObject.activeInHierarchy)
        {
            DeselectCurrent();
            return;
        }

        if (selectedTarget != target)
        {
            if (selectedTarget != null)
            {
                ThreatInteractable previous = selectedTarget;
                previous.SetSelected(false);
                ThreatInteractionEvents.RaiseDeselected(previous);
            }

            selectedTarget = target;
            selectedTarget.SetSelected(true);
            ThreatInteractionEvents.RaiseSelected(selectedTarget);
        }

        if (notifyTap)
            ThreatInteractionEvents.RaiseTapped(selectedTarget);
    }

    public void DeselectCurrent()
    {
        if (selectedTarget == null)
            return;

        ThreatInteractable previous = selectedTarget;
        selectedTarget = null;
        previous.SetSelected(false);
        ThreatInteractionEvents.RaiseDeselected(previous);
    }

    /// <summary>
    /// Intended for Triyanti's Reset/Pindai Ulang button integration.
    /// </summary>
    public void ResetInteractions()
    {
        for (int i = registeredTargets.Count - 1; i >= 0; i--)
        {
            ThreatInteractable target = registeredTargets[i];
            if (target == null)
            {
                registeredTargets.RemoveAt(i);
                continue;
            }

            target.ResetInteractionTransform();
        }

        DeselectCurrent();
    }

    public bool SelectAtScreenPosition(Vector2 screenPosition, bool notifyTap = true)
    {
        ResolveCamera();
        if (interactionCamera == null)
            return false;

        Ray ray = interactionCamera.ScreenPointToRay(screenPosition);
        int hitCount = Physics.RaycastNonAlloc(
            ray,
            raycastHits,
            maximumRayDistance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);

        ThreatInteractable nearestTarget = null;
        float nearestDistance = float.PositiveInfinity;

        for (int i = 0; i < hitCount; i++)
        {
            ThreatInteractable target = raycastHits[i].collider
                .GetComponentInParent<ThreatInteractable>();
            if (target == null || raycastHits[i].distance >= nearestDistance)
                continue;

            nearestTarget = target;
            nearestDistance = raycastHits[i].distance;
        }

        if (nearestTarget == null)
        {
            DeselectCurrent();
            return false;
        }

        SelectInteractable(nearestTarget, notifyTap);
        return true;
    }

    private void HandleTouchInput()
    {
        if (Input.touchCount >= 2)
        {
            twoFingerGestureActive = true;
            tapFingerId = -1;
            HandleTwoFingerGesture(Input.GetTouch(0), Input.GetTouch(1));
            return;
        }

        if (Input.touchCount == 0)
        {
            twoFingerGestureActive = false;
            tapFingerId = -1;
            return;
        }

        if (twoFingerGestureActive)
            return;

        Touch touch = Input.GetTouch(0);
        if (touch.phase == TouchPhase.Began)
        {
            tapFingerId = touch.fingerId;
            tapStartPosition = touch.position;
            tapMoved = false;
        }
        else if (touch.fingerId == tapFingerId
            && (touch.position - tapStartPosition).sqrMagnitude
                > tapMovementThreshold * tapMovementThreshold)
        {
            tapMoved = true;
        }

        if (touch.phase != TouchPhase.Ended || touch.fingerId != tapFingerId)
            return;

        bool overUi = EventSystem.current != null
            && EventSystem.current.IsPointerOverGameObject(touch.fingerId);

        if (!tapMoved && !overUi)
            SelectAtScreenPosition(touch.position);

        tapFingerId = -1;
    }

    private void HandleTwoFingerGesture(Touch first, Touch second)
    {
        if (selectedTarget == null)
            return;

        Vector2 previousFirst = first.position - first.deltaPosition;
        Vector2 previousSecond = second.position - second.deltaPosition;
        Vector2 previousVector = previousSecond - previousFirst;
        Vector2 currentVector = second.position - first.position;

        if (previousVector.sqrMagnitude < 1f || currentVector.sqrMagnitude < 1f)
            return;

        float rawScaleFactor = currentVector.magnitude / previousVector.magnitude;
        float scaleFactor = Mathf.Pow(rawScaleFactor, pinchSensitivity);
        selectedTarget.ApplyScaleFactor(scaleFactor);

        float twistAngle = Vector2.SignedAngle(previousVector, currentVector);
        selectedTarget.RotateAroundMarkerNormal(-twistAngle * twistSensitivity);
    }

    private void HandleMouseInput()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        bool overUi = EventSystem.current != null
            && EventSystem.current.IsPointerOverGameObject();

        if (Input.GetMouseButtonDown(0) && !overUi)
            SelectAtScreenPosition(Input.mousePosition);

        if (selectedTarget == null)
            return;

        float scroll = Input.mouseScrollDelta.y;
        if (!Mathf.Approximately(scroll, 0f))
        {
            float factor = 1f + scroll * mouseScaleStep;
            selectedTarget.ApplyScaleFactor(Mathf.Max(0.1f, factor));
        }

        if (Input.GetMouseButton(1))
        {
            float rotation = -Input.GetAxis("Mouse X") * mouseRotationSensitivity;
            selectedTarget.RotateAroundMarkerNormal(rotation);
        }
#endif
    }

    private void ResolveCamera()
    {
        if (interactionCamera == null)
            interactionCamera = Camera.main;
    }
}
