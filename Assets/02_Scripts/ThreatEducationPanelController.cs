using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Responsive runtime uGUI panel for threat education content.
/// It remains hidden until a ThreatInteractionEvents.ThreatTapped event is received.
/// </summary>
public class ThreatEducationPanelController : MonoBehaviour
{
    private static readonly Color BackgroundColor = new Color32(8, 18, 34, 246);
    private static readonly Color SurfaceColor = new Color32(18, 35, 58, 255);
    private static readonly Color PrimaryTextColor = new Color32(242, 249, 255, 255);
    private static readonly Color SecondaryTextColor = new Color32(183, 207, 226, 255);

    private Font runtimeFont;
    private RectTransform safeAreaRect;
    private GameObject modalRoot;
    private UnityEngine.UI.Image panelImage;
    private UnityEngine.UI.Image accentBar;
    private UnityEngine.UI.Text titleText;
    private UnityEngine.UI.Text definitionText;
    private UnityEngine.UI.Text exampleText;
    private UnityEngine.UI.Text preventionText;
    private UnityEngine.UI.Button closeButton;
    private UnityEngine.UI.Button quizButton;
    private UnityEngine.UI.Button resetButton;

    private Rect lastSafeArea;
    private Vector2Int lastScreenSize;
    private string currentThreatId;
    private bool closing;

    public static ThreatEducationPanelController Instance { get; private set; }
    public bool IsOpen => modalRoot != null && modalRoot.activeSelf;
    public string CurrentThreatId => currentThreatId;
    public UnityEngine.UI.Button QuizButton => quizButton;
    public UnityEngine.UI.Button ResetButton => resetButton;

    private void Awake()
    {
        if (Instance == null || Instance == this)
            Instance = this;

        runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        BuildInterface();
        ApplySafeArea(true);
    }

    private void OnEnable()
    {
        ThreatInteractionEvents.ThreatTapped += HandleThreatTapped;
        ThreatInteractionEvents.ThreatDeselected += HandleThreatDeselected;
    }

    private void OnDisable()
    {
        ThreatInteractionEvents.ThreatTapped -= HandleThreatTapped;
        ThreatInteractionEvents.ThreatDeselected -= HandleThreatDeselected;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        ApplySafeArea(false);
    }

    public bool OpenForThreat(string threatId)
    {
        ThreatEducationContent content;
        if (!ThreatEducationCatalog.TryGet(threatId, out content))
        {
            Debug.LogWarning("No education content for threat: " + threatId);
            return false;
        }

        currentThreatId = content.ThreatId;
        titleText.text = content.DisplayName;
        definitionText.text = content.Definition;
        exampleText.text = content.RealWorldExample;
        preventionText.text = content.Prevention;

        Color accent = GetAccentColor(content.ThreatId);
        accentBar.color = accent;
        panelImage.color = Color.Lerp(SurfaceColor, accent, 0.08f);
        SetButtonColor(quizButton, accent);

        modalRoot.SetActive(true);
        modalRoot.transform.SetAsLastSibling();
        ThreatEducationEvents.RaisePanelOpened(content);
        return true;
    }

    public void ClosePanel()
    {
        ClosePanel(true);
    }

    public void RequestQuiz()
    {
        if (!string.IsNullOrEmpty(currentThreatId))
            ThreatEducationEvents.RaiseQuizRequested(currentThreatId);
    }

    public void ResetAndRescan()
    {
        ClosePanel(false);

        if (ThreatInteractionManager.Instance != null)
            ThreatInteractionManager.Instance.ResetInteractions();

        ThreatEducationEvents.RaiseResetRequested();
    }

    private void HandleThreatTapped(string threatId, GameObject target)
    {
        OpenForThreat(threatId);
    }

    private void HandleThreatDeselected(string threatId, GameObject target)
    {
        if (!closing && IsOpen && string.Equals(currentThreatId, threatId))
            ClosePanel(false);
    }

    private void ClosePanel(bool deselectObject)
    {
        if (!IsOpen)
            return;

        closing = true;
        modalRoot.SetActive(false);
        currentThreatId = null;

        if (deselectObject && ThreatInteractionManager.Instance != null)
            ThreatInteractionManager.Instance.DeselectCurrent();

        ThreatEducationEvents.RaisePanelClosed();
        closing = false;
    }

    private void BuildInterface()
    {
        EnsureEventSystem();

        GameObject canvasObject = new GameObject(
            "ThreatEducationCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(UnityEngine.UI.CanvasScaler),
            typeof(UnityEngine.UI.GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        UnityEngine.UI.CanvasScaler scaler
            = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        safeAreaRect = CreateRect("SafeArea", canvasObject.transform);
        Stretch(safeAreaRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        modalRoot = new GameObject("EducationModal", typeof(RectTransform));
        modalRoot.transform.SetParent(safeAreaRect, false);
        RectTransform modalRect = modalRoot.GetComponent<RectTransform>();
        Stretch(modalRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        UnityEngine.UI.Image overlay = CreateImage(
            "DimmedOverlay",
            modalRoot.transform,
            new Color(0f, 0f, 0f, 0.7f));
        Stretch(overlay.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        UnityEngine.UI.Button overlayButton = overlay.gameObject.AddComponent<UnityEngine.UI.Button>();
        overlayButton.transition = UnityEngine.UI.Selectable.Transition.None;
        overlayButton.onClick.AddListener(ClosePanel);

        panelImage = CreateImage("Panel", modalRoot.transform, SurfaceColor);
        RectTransform panelRect = panelImage.rectTransform;
        Stretch(
            panelRect,
            new Vector2(0.055f, 0.055f),
            new Vector2(0.945f, 0.945f),
            Vector2.zero,
            Vector2.zero);
        UnityEngine.UI.Outline outline = panelImage.gameObject.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor = new Color(0.12f, 0.75f, 1f, 0.75f);
        outline.effectDistance = new Vector2(2f, -2f);

        RectTransform header = CreateRect("Header", panelRect);
        Stretch(header, new Vector2(0f, 1f), Vector2.one, new Vector2(32f, -116f), new Vector2(-32f, -18f));

        accentBar = CreateImage("AccentBar", header, new Color32(22, 199, 255, 255));
        SetAnchoredRect(accentBar.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(6f, 0f), new Vector2(0f, 0.5f));

        UnityEngine.UI.Text eyebrow = CreateText(
            "Eyebrow",
            header,
            "KENALI ANCAMAN DIGITAL",
            22,
            SecondaryTextColor,
            TextAnchor.UpperLeft,
            FontStyle.Bold);
        Stretch(eyebrow.rectTransform, new Vector2(0f, 0.55f), new Vector2(1f, 1f), new Vector2(24f, 0f), new Vector2(-96f, 0f));

        titleText = CreateText(
            "ThreatTitle",
            header,
            "ANCAMAN",
            43,
            PrimaryTextColor,
            TextAnchor.LowerLeft,
            FontStyle.Bold);
        Stretch(titleText.rectTransform, Vector2.zero, new Vector2(1f, 0.65f), new Vector2(24f, 0f), new Vector2(-96f, 0f));
        titleText.resizeTextForBestFit = true;
        titleText.resizeTextMinSize = 28;
        titleText.resizeTextMaxSize = 43;

        closeButton = CreateButton(
            "CloseButton",
            header,
            "×",
            new Color32(42, 66, 88, 255),
            new Color32(232, 246, 255, 255));
        SetAnchoredRect(closeButton.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(72f, 72f), new Vector2(-36f, -36f));
        closeButton.onClick.AddListener(ClosePanel);

        RectTransform footer = CreateRect("Footer", panelRect);
        Stretch(footer, Vector2.zero, new Vector2(1f, 0f), new Vector2(32f, 22f), new Vector2(-32f, 142f));
        UnityEngine.UI.HorizontalLayoutGroup footerLayout
            = footer.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
        footerLayout.spacing = 18f;
        footerLayout.childAlignment = TextAnchor.MiddleCenter;
        footerLayout.childControlWidth = true;
        footerLayout.childControlHeight = true;
        footerLayout.childForceExpandWidth = true;
        footerLayout.childForceExpandHeight = true;

        quizButton = CreateButton(
            "QuizButton",
            footer,
            "UJI PEMAHAMAN",
            new Color32(22, 167, 217, 255),
            Color.white);
        quizButton.onClick.AddListener(RequestQuiz);

        resetButton = CreateButton(
            "ResetButton",
            footer,
            "RESET / PINDAI ULANG",
            new Color32(52, 73, 94, 255),
            Color.white);
        resetButton.onClick.AddListener(ResetAndRescan);

        RectTransform scrollView = CreateRect("ScrollView", panelRect);
        Stretch(scrollView, Vector2.zero, Vector2.one, new Vector2(32f, 154f), new Vector2(-32f, -128f));
        UnityEngine.UI.ScrollRect scrollRect
            = scrollView.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 36f;

        UnityEngine.UI.Image viewportImage = CreateImage(
            "Viewport",
            scrollView,
            new Color(1f, 1f, 1f, 0.012f));
        Stretch(viewportImage.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        UnityEngine.UI.Mask mask = viewportImage.gameObject.AddComponent<UnityEngine.UI.Mask>();
        mask.showMaskGraphic = false;

        RectTransform content = CreateRect("Content", viewportImage.transform);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = Vector2.zero;

        UnityEngine.UI.VerticalLayoutGroup contentLayout
            = content.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        contentLayout.padding = new RectOffset(8, 18, 8, 24);
        contentLayout.spacing = 12f;
        contentLayout.childAlignment = TextAnchor.UpperLeft;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;

        UnityEngine.UI.ContentSizeFitter fitter
            = content.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();
        fitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = viewportImage.rectTransform;
        scrollRect.content = content;

        CreateSectionHeading(content, "PENGERTIAN");
        definitionText = CreateBodyText(content, "DefinitionText");
        CreateDivider(content);
        CreateSectionHeading(content, "CONTOH KASUS");
        exampleText = CreateBodyText(content, "ExampleText");
        CreateDivider(content);
        CreateSectionHeading(content, "LANGKAH PENCEGAHAN");
        preventionText = CreateBodyText(content, "PreventionText");

        modalRoot.SetActive(false);
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null)
            return;

        GameObject eventSystemObject = new GameObject(
            "EventSystem",
            typeof(EventSystem),
            typeof(StandaloneInputModule));
        eventSystemObject.transform.SetParent(transform, false);
    }

    private void ApplySafeArea(bool force)
    {
        if (safeAreaRect == null)
            return;

        Rect safeArea = Screen.safeArea;
        Vector2Int screenSize = new Vector2Int(Screen.width, Screen.height);
        if (!force && safeArea == lastSafeArea && screenSize == lastScreenSize)
            return;

        lastSafeArea = safeArea;
        lastScreenSize = screenSize;

        if (Screen.width <= 0 || Screen.height <= 0)
            return;

        safeAreaRect.anchorMin = new Vector2(
            safeArea.xMin / Screen.width,
            safeArea.yMin / Screen.height);
        safeAreaRect.anchorMax = new Vector2(
            safeArea.xMax / Screen.width,
            safeArea.yMax / Screen.height);
        safeAreaRect.offsetMin = Vector2.zero;
        safeAreaRect.offsetMax = Vector2.zero;
    }

    private void CreateSectionHeading(Transform parent, string text)
    {
        UnityEngine.UI.Text heading = CreateText(
            text.Replace(" ", string.Empty) + "Heading",
            parent,
            text,
            24,
            new Color32(83, 214, 255, 255),
            TextAnchor.MiddleLeft,
            FontStyle.Bold);
        UnityEngine.UI.LayoutElement layout
            = heading.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
        layout.preferredHeight = 40f;
    }

    private UnityEngine.UI.Text CreateBodyText(Transform parent, string objectName)
    {
        UnityEngine.UI.Text body = CreateText(
            objectName,
            parent,
            string.Empty,
            27,
            PrimaryTextColor,
            TextAnchor.UpperLeft,
            FontStyle.Normal);
        body.horizontalOverflow = HorizontalWrapMode.Wrap;
        body.verticalOverflow = VerticalWrapMode.Overflow;
        body.lineSpacing = 1.15f;
        return body;
    }

    private void CreateDivider(Transform parent)
    {
        UnityEngine.UI.Image divider = CreateImage(
            "Divider",
            parent,
            new Color(0.35f, 0.55f, 0.7f, 0.35f));
        UnityEngine.UI.LayoutElement layout
            = divider.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
        layout.preferredHeight = 2f;
    }

    private UnityEngine.UI.Button CreateButton(
        string objectName,
        Transform parent,
        string label,
        Color background,
        Color foreground)
    {
        UnityEngine.UI.Image image = CreateImage(objectName, parent, background);
        UnityEngine.UI.Button button = image.gameObject.AddComponent<UnityEngine.UI.Button>();
        button.targetGraphic = image;

        UnityEngine.UI.ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
        colors.pressedColor = new Color(0.78f, 0.82f, 0.86f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        UnityEngine.UI.Text text = CreateText(
            "Label",
            button.transform,
            label,
            25,
            foreground,
            TextAnchor.MiddleCenter,
            FontStyle.Bold);
        Stretch(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(10f, 8f), new Vector2(-10f, -8f));
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 17;
        text.resizeTextMaxSize = 25;
        text.raycastTarget = false;
        return button;
    }

    private UnityEngine.UI.Text CreateText(
        string objectName,
        Transform parent,
        string text,
        int fontSize,
        Color color,
        TextAnchor alignment,
        FontStyle style)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(UnityEngine.UI.Text));
        textObject.transform.SetParent(parent, false);
        UnityEngine.UI.Text label = textObject.GetComponent<UnityEngine.UI.Text>();
        label.font = runtimeFont;
        label.text = text;
        label.fontSize = fontSize;
        label.color = color;
        label.alignment = alignment;
        label.fontStyle = style;
        label.raycastTarget = false;
        return label;
    }

    private static RectTransform CreateRect(string objectName, Transform parent)
    {
        GameObject rectObject = new GameObject(objectName, typeof(RectTransform));
        rectObject.transform.SetParent(parent, false);
        return rectObject.GetComponent<RectTransform>();
    }

    private static UnityEngine.UI.Image CreateImage(
        string objectName,
        Transform parent,
        Color color)
    {
        GameObject imageObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(UnityEngine.UI.Image));
        imageObject.transform.SetParent(parent, false);
        UnityEngine.UI.Image image = imageObject.GetComponent<UnityEngine.UI.Image>();
        image.color = color;
        return image;
    }

    private static void Stretch(
        RectTransform rect,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private static void SetAnchoredRect(
        RectTransform rect,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 size,
        Vector2 anchoredPosition)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;
    }

    private static Color GetAccentColor(string threatId)
    {
        if (string.Equals(threatId, "Malware"))
            return new Color32(255, 84, 77, 255);
        if (string.Equals(threatId, "Ransomware"))
            return new Color32(255, 190, 54, 255);
        return new Color32(22, 199, 255, 255);
    }

    private static void SetButtonColor(UnityEngine.UI.Button button, Color color)
    {
        if (button != null && button.targetGraphic != null)
            button.targetGraphic.color = color;
    }
}
