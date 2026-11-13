using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Responsive runtime uGUI panel for threat education content.
/// It remains hidden until a ThreatInteractionEvents.ThreatTapped event is received.
/// </summary>
public class ThreatEducationPanelController : MonoBehaviour
{
    private static readonly Color BackgroundColor = new Color32(8, 15, 28, 255);
    private static readonly Color SurfaceColor = new Color32(17, 29, 46, 255);
    private static readonly Color PrimaryTextColor = new Color32(242, 249, 255, 255);
    private static readonly Color SecondaryTextColor = new Color32(168, 189, 208, 255);

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

    private Rect lastSafeArea;
    private Vector2Int lastScreenSize;
    private string currentThreatId;
    private bool closing;
    private bool expanded;
    private UnityEngine.UI.Button exampleToggle, preventionToggle;
    private bool exampleOpen, preventionOpen;
    private GameObject scannerHintRoot;
    private string fullDefinition, shortDefinition;
    private bool layoutPending;
    private RectTransform sheet;
    private UnityEngine.UI.Button expandButton;
    private UnityEngine.UI.ScrollRect educationScroll;
    private UnityEngine.UI.Text scannerStatus;
    private UnityEngine.UI.Text scannerHint;
    private GameObject scannerHud;
    private UnityEngine.UI.Image scannerIndicator;
    private static readonly Color Cyan = new Color32(34, 211, 238, 255);

    public static ThreatEducationPanelController Instance { get; private set; }
    public bool IsOpen => modalRoot != null && modalRoot.activeSelf;
    public string CurrentThreatId => currentThreatId;
    public UnityEngine.UI.Button QuizButton => quizButton;
    // Compatibility: reset is now a method, not a visible education control.
    public UnityEngine.UI.Button ResetButton => null;

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
        if (IsOpen && layoutPending) FitSheet();
        bool quizOpen = ThreatQuizController.Instance != null && ThreatQuizController.Instance.IsOpen;
        if (scannerHud != null) scannerHud.SetActive(!quizOpen);
        if (scannerHintRoot != null) scannerHintRoot.SetActive(!IsOpen);
        var manager = ThreatInteractionManager.Instance;
        var visible = manager != null ? manager.VisibleTarget : null;
        if (scannerStatus != null)
        {
            scannerStatus.text = visible != null ? "TERDETEKSI / " + visible.ThreatId.ToUpperInvariant() : "MEMINDAI / ARAHKAN KE KARTU";
            scannerIndicator.color = visible != null ? GetAccentColor(visible.ThreatId) : Cyan;
            scannerHint.text = visible != null ? "Geser: rotasi  |  Cubit: zoom  |  Tap: informasi" : "Arahkan kamera ke salah satu kartu ancaman";
        }
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
        fullDefinition = content.Definition;
        shortDefinition = content.ThreatId == "Phishing" ? "Penipuan digital yang menyamar sebagai pihak tepercaya untuk mencuri data sensitif." : content.ThreatId == "Malware" ? "Perangkat lunak berbahaya yang dapat merusak sistem, mencuri data, atau mengambil alih perangkat." : "Malware yang mengunci perangkat atau mengenkripsi data dan meminta tebusan.";
        definitionText.text = shortDefinition;
        exampleText.text = content.RealWorldExample;
        preventionText.text = content.Prevention;

        Color accent = GetAccentColor(content.ThreatId);
        accentBar.color = accent;
        panelImage.color = SurfaceColor;
        exampleOpen = preventionOpen = false;
        SetButtonColor(quizButton, Cyan);
        SetExpanded(false);

        modalRoot.SetActive(true);
        modalRoot.transform.SetAsLastSibling();
        FitSheet();
        Canvas.ForceUpdateCanvases();
        if (educationScroll != null) educationScroll.verticalNormalizedPosition = 1f;
        ThreatEducationEvents.RaisePanelOpened(content);
        return true;
    }

    public void ClosePanel()
    {
        ClosePanel(false);
    }

    public void RequestQuiz()
    {
        if (string.IsNullOrEmpty(currentThreatId))
            return;

        string threatId = currentThreatId;
        ThreatEducationEvents.RaiseQuizRequested(threatId);
        if (ThreatQuizController.Instance != null && ThreatQuizController.Instance.IsOpen)
            ClosePanel(false);
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

        BuildScannerHud();

        modalRoot = new GameObject("EducationModal", typeof(RectTransform));
        modalRoot.transform.SetParent(safeAreaRect, false);
        RectTransform modalRect = modalRoot.GetComponent<RectTransform>();
        Stretch(modalRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        UnityEngine.UI.Image overlay = CreateImage(
            "DimmedOverlay",
            modalRoot.transform,
            new Color(0f, 0f, 0f, 0.12f));
        Stretch(overlay.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        UnityEngine.UI.Button overlayButton = overlay.gameObject.AddComponent<UnityEngine.UI.Button>();
        overlayButton.transition = UnityEngine.UI.Selectable.Transition.None;
        overlayButton.onClick.AddListener(ClosePanel);

        panelImage = CreateImage("Panel", modalRoot.transform, SurfaceColor);
        RectTransform panelRect = panelImage.rectTransform;
        Stretch(
            panelRect,
            new Vector2(0.025f, 0.02f),
            new Vector2(0.975f, 0.57f),
            Vector2.zero,
            Vector2.zero);
        sheet = panelRect;
        UnityEngine.UI.Outline outline = panelImage.gameObject.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor = new Color32(41, 65, 88, 255);
        outline.effectDistance = new Vector2(2f, -2f);

        RectTransform header = CreateRect("Header", panelRect);
        Stretch(header, new Vector2(0f, 1f), Vector2.one, new Vector2(28f, -144f), new Vector2(-28f, -16f));

        accentBar = CreateImage("AccentBar", header, new Color32(22, 199, 255, 255));
        SetAnchoredRect(accentBar.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(6f, 0f), new Vector2(0f, 0.5f));

        UnityEngine.UI.Text eyebrow = CreateText(
            "Eyebrow",
            header,
            "CYBER THREAT AR  /  THREAT INTEL",
            22,
            SecondaryTextColor,
            TextAnchor.UpperLeft,
            FontStyle.Bold);
        Stretch(eyebrow.rectTransform, new Vector2(0f, 0.55f), new Vector2(1f, 1f), new Vector2(24f, 0f), new Vector2(-230f, 0f));

        titleText = CreateText(
            "ThreatTitle",
            header,
            "ANCAMAN",
            43,
            PrimaryTextColor,
            TextAnchor.LowerLeft,
            FontStyle.Bold);
        Stretch(titleText.rectTransform, Vector2.zero, new Vector2(1f, 0.65f), new Vector2(24f, 0f), new Vector2(-230f, 0f));
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
        expandButton = CreateButton("ExpandButton", header, "DETAIL", new Color32(25, 43, 64, 255), Cyan);
        SetAnchoredRect(expandButton.GetComponent<RectTransform>(), Vector2.one, Vector2.one,
            new Vector2(144f, 72f), new Vector2(-160f, -36f));
        expandButton.onClick.AddListener(() => SetExpanded(!expanded));

        RectTransform footer = CreateRect("Footer", panelRect);
        Stretch(footer, Vector2.zero, new Vector2(1f, 0f), new Vector2(32f, 22f), new Vector2(-32f, 112f));
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
            Cyan,
            BackgroundColor);
        quizButton.onClick.AddListener(RequestQuiz);

        RectTransform scrollView = CreateRect("ScrollView", panelRect);
        Stretch(scrollView, Vector2.zero, Vector2.one, new Vector2(28f, 124f), new Vector2(-28f, -154f));
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
        educationScroll = scrollRect;

        definitionText = CreateSectionCard(content, "01 / PENGERTIAN", "DefinitionText");
        exampleText = CreateAccordion(content, "02 / CONTOH KASUS", "ExampleText", out exampleToggle);
        preventionText = CreateAccordion(content, "03 / LANGKAH PENCEGAHAN", "PreventionText", out preventionToggle);
        exampleToggle.onClick.AddListener(() => { exampleOpen = !exampleOpen; RefreshSections(); });
        preventionToggle.onClick.AddListener(() => { preventionOpen = !preventionOpen; RefreshSections(); });

        modalRoot.SetActive(false);
    }

    private void SetExpanded(bool value)
    {
        expanded = value;
        exampleOpen = preventionOpen = value;
        RefreshSections();
    }

    private void RefreshSections()
    {
        if (sheet == null || exampleText == null) return;
        bool tall = expanded || exampleOpen || preventionOpen;
        sheet.anchorMax = new Vector2(0.975f, Screen.width > Screen.height ? 0.91f : tall ? 0.85f : 0.48f);
        definitionText.text = expanded ? fullDefinition : shortDefinition;
        exampleText.gameObject.SetActive(exampleOpen);
        preventionText.gameObject.SetActive(preventionOpen);
        exampleToggle.GetComponentInChildren<UnityEngine.UI.Text>().text = "02 / CONTOH KASUS     " + (exampleOpen ? "−" : "+");
        preventionToggle.GetComponentInChildren<UnityEngine.UI.Text>().text = "03 / LANGKAH PENCEGAHAN     " + (preventionOpen ? "−" : "+");
        expandButton.GetComponentInChildren<UnityEngine.UI.Text>().text = expanded ? "RINGKAS" : "DETAIL";
        UnityEngine.UI.LayoutRebuilder.MarkLayoutForRebuild(educationScroll.content);
        layoutPending = true;
    }

    private void FitSheet()
    {
        if (safeAreaRect.rect.height <= 1f) return;
        Canvas.ForceUpdateCanvases();
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(educationScroll.content);
        float needed = UnityEngine.UI.LayoutUtility.GetPreferredHeight(educationScroll.content) + 290f;
        float max = Screen.width > Screen.height ? .94f : .88f;
        float top = Mathf.Clamp(.02f + needed / safeAreaRect.rect.height, .34f, max);
        sheet.anchorMax = new Vector2(.975f, top);
        layoutPending = false;
    }

    private UnityEngine.UI.Text CreateAccordion(Transform parent, string heading, string name, out UnityEngine.UI.Button toggle)
    {
        var card = CreateImage(name + "Card", parent, new Color32(17, 29, 46, 255));
        var layout = card.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 4, 4);
        layout.spacing = 6;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        toggle = CreateButton(name + "ToggleButton", card.transform, heading, new Color32(25, 43, 64, 255), Cyan);
        var element = toggle.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
        element.preferredHeight = 68;
        var label = toggle.GetComponentInChildren<UnityEngine.UI.Text>();
        label.alignment = TextAnchor.MiddleLeft;
        return CreateBodyText(card.transform, name);
    }

    private UnityEngine.UI.Text CreateSectionCard(Transform parent, string heading, string name)
    {
        var card = CreateImage(name + "Card", parent, new Color32(25, 43, 64, 255));
        var layout = card.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        layout.padding = new RectOffset(20, 20, 14, 20);
        layout.spacing = 8;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        CreateSectionHeading(card.transform, heading);
        return CreateBodyText(card.transform, name);
    }

    private void BuildScannerHud()
    {
        var hud = CreateRect("ScannerHUD", safeAreaRect);
        Stretch(hud, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        scannerHud = hud.gameObject;
        var header = CreateImage("ScannerHeader", hud, new Color32(8, 15, 28, 238));
        Stretch(header.rectTransform, new Vector2(0.025f, 1f), new Vector2(0.975f, 1f),
            new Vector2(0, -116), new Vector2(0, -12));
        header.raycastTarget = false;
        var brand = CreateText("Brand", header.transform, "CYBER THREAT AR  /  SCANNER", 27, Cyan, TextAnchor.MiddleLeft, FontStyle.Bold);
        Stretch(brand.rectTransform, new Vector2(0, 0.48f), Vector2.one, new Vector2(24, 0), new Vector2(-24, 0));
        brand.resizeTextForBestFit = true;
        brand.resizeTextMinSize = 17;
        brand.resizeTextMaxSize = 27;
        scannerIndicator = CreateImage("StatusIndicator", header.transform, Cyan);
        SetAnchoredRect(scannerIndicator.rectTransform, new Vector2(0, 0.25f), new Vector2(0, 0.25f), new Vector2(12, 12), new Vector2(28, 0));
        scannerIndicator.raycastTarget = false;
        scannerStatus = CreateText("ScannerStatus", header.transform, "MEMINDAI", 22, PrimaryTextColor, TextAnchor.MiddleLeft, FontStyle.Bold);
        Stretch(scannerStatus.rectTransform, Vector2.zero, new Vector2(1, 0.5f), new Vector2(48, 0), new Vector2(-24, 0));
        scannerStatus.resizeTextForBestFit = true;
        scannerStatus.resizeTextMinSize = 16;
        scannerStatus.resizeTextMaxSize = 22;
        var hint = CreateImage("ScannerHint", hud, new Color32(8, 15, 28, 220));
        Stretch(hint.rectTransform, new Vector2(0.05f, 0), new Vector2(0.95f, 0), new Vector2(0, 16), new Vector2(0, 86));
        hint.raycastTarget = false;
        scannerHintRoot = hint.gameObject;
        scannerHint = CreateText("HintText", hint.transform, "Arahkan kamera ke kartu", 24, SecondaryTextColor, TextAnchor.MiddleCenter, FontStyle.Normal);
        Stretch(scannerHint.rectTransform, Vector2.zero, Vector2.one, new Vector2(12, 0), new Vector2(-12, 0));
        scannerHint.resizeTextForBestFit = true;
        scannerHint.resizeTextMinSize = 18;
        scannerHint.resizeTextMaxSize = 24;
        // Non-interactive corner markers: the live AR camera remains visible.
        var frame = CreateRect("ScanFrame", hud);
        Stretch(frame, new Vector2(0.15f, 0.22f), new Vector2(0.85f, 0.76f), Vector2.zero, Vector2.zero);
        for (int x = 0; x <= 1; x++)
        for (int y = 0; y <= 1; y++)
        {
            Vector2 corner = new Vector2(x, y);
            float dx = x == 0 ? 1 : -1;
            float dy = y == 0 ? 1 : -1;
            var horizontal = CreateImage("FrameH", frame, new Color32(34, 211, 238, 160));
            SetAnchoredRect(horizontal.rectTransform, corner, corner, new Vector2(52, 4), new Vector2(dx * 26, dy * 2));
            horizontal.raycastTarget = false;
            var vertical = CreateImage("FrameV", frame, new Color32(34, 211, 238, 160));
            SetAnchoredRect(vertical.rectTransform, corner, corner, new Vector2(4, 52), new Vector2(dx * 2, dy * 26));
            vertical.raycastTarget = false;
        }
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
        if (sheet != null) RefreshSections();

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
        CyberUiStyle.ApplySurface(image, objectName);
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
            return new Color32(255, 77, 94, 255);
        if (string.Equals(threatId, "Ransomware"))
            return new Color32(251, 191, 36, 255);
        return new Color32(59, 130, 246, 255);
    }

    private static void SetButtonColor(UnityEngine.UI.Button button, Color color)
    {
        if (button != null && button.targetGraphic != null)
            button.targetGraphic.color = color;
    }
}

// Shared runtime visual asset. No external texture/font or scene changes required.
internal static class CyberUiStyle
{
    private static Sprite rounded;
    internal static void ApplySurface(UnityEngine.UI.Image image, string name)
    {
        if (!(name == "Panel" || name.EndsWith("Card") || name.EndsWith("Button")
            || name.StartsWith("Answer") || name == "FeedbackPanel" || name == "ResultGroup"
            || name == "ScannerHeader" || name == "ScannerHint" || name == "ScannerReset")) return;
        if (rounded == null)
        {
            const int size = 64;
            const int radius = 16;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "CyberUI_RoundedSurface";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x + 0.5f - Mathf.Clamp(x + 0.5f, radius, size - radius);
                float dy = y + 0.5f - Mathf.Clamp(y + 0.5f, radius, size - radius);
                float alpha = Mathf.Clamp01(radius + 0.5f - Mathf.Sqrt(dx * dx + dy * dy));
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255));
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            rounded = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
                100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
            rounded.name = "CyberUI_RoundedSurface";
        }
        image.sprite = rounded;
        image.type = UnityEngine.UI.Image.Type.Sliced;
    }
}
