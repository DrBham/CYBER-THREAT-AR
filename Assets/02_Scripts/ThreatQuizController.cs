using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Runtime uGUI quiz for Phishing, Malware, and Ransomware.
/// Added to XR Origin by ThreatImageTracker so the shared scene stays unchanged.
/// </summary>
public class ThreatQuizController : MonoBehaviour
{
    private static readonly Color BackgroundColor = new Color32(7, 15, 29, 248);
    private static readonly Color SurfaceColor = new Color32(18, 34, 55, 255);
    private static readonly Color OptionColor = new Color32(37, 59, 82, 255);
    private static readonly Color PrimaryTextColor = new Color32(244, 249, 255, 255);
    private static readonly Color SecondaryTextColor = new Color32(174, 199, 220, 255);
    private static readonly Color CorrectColor = new Color32(37, 166, 91, 255);
    private static readonly Color IncorrectColor = new Color32(211, 62, 69, 255);

    private readonly List<UnityEngine.UI.Button> answerButtons
        = new List<UnityEngine.UI.Button>();
    private readonly List<UnityEngine.UI.Text> answerLabels
        = new List<UnityEngine.UI.Text>();

    private Font runtimeFont;
    private RectTransform safeAreaRect;
    private GameObject modalRoot;
    private UnityEngine.UI.Image panelImage;
    private UnityEngine.UI.Image accentBar;
    private UnityEngine.UI.Text titleText;
    private UnityEngine.UI.Text progressText;
    private UnityEngine.UI.Text scoreText;
    private UnityEngine.UI.Text questionText;
    private UnityEngine.UI.Text feedbackText;
    private UnityEngine.UI.Text resultTitleText;
    private UnityEngine.UI.Text resultScoreText;
    private UnityEngine.UI.Text resultMessageText;
    private GameObject questionGroup;
    private GameObject feedbackPanel;
    private GameObject resultGroup;
    private UnityEngine.UI.Button nextButton;
    private UnityEngine.UI.Button returnButton;
    private UnityEngine.UI.Button resetButton;

    private IReadOnlyList<ThreatQuizQuestion> questions;
    private string currentThreatId;
    private int currentQuestionIndex;
    private int score;
    private bool hasAnswered;
    private bool showingResults;
    private Rect lastSafeArea;
    private Vector2Int lastScreenSize;

    public static ThreatQuizController Instance { get; private set; }
    public bool IsOpen => modalRoot != null && modalRoot.activeSelf;
    public bool IsShowingResults => showingResults;
    public bool HasAnswered => hasAnswered;
    public string CurrentThreatId => currentThreatId;
    public int CurrentQuestionIndex => currentQuestionIndex;
    public int Score => score;
    public int TotalQuestions => questions != null ? questions.Count : 0;
    public IReadOnlyList<UnityEngine.UI.Button> AnswerButtons => answerButtons;
    public UnityEngine.UI.Button NextButton => nextButton;
    public UnityEngine.UI.Button ReturnButton => returnButton;
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
        ThreatEducationEvents.QuizRequested += HandleQuizRequested;
        ThreatEducationEvents.ResetRequested += HandleResetRequested;
    }

    private void OnDisable()
    {
        ThreatEducationEvents.QuizRequested -= HandleQuizRequested;
        ThreatEducationEvents.ResetRequested -= HandleResetRequested;
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

    public bool OpenQuiz(string threatId)
    {
        IReadOnlyList<ThreatQuizQuestion> quizQuestions;
        if (!ThreatQuizCatalog.TryGet(threatId, out quizQuestions) || quizQuestions.Count == 0)
        {
            Debug.LogWarning("No quiz questions for threat: " + threatId);
            return false;
        }

        currentThreatId = threatId;
        questions = quizQuestions;
        currentQuestionIndex = 0;
        score = 0;
        hasAnswered = false;
        showingResults = false;
        nextButton.onClick.RemoveListener(RestartFromResult);
        nextButton.onClick.RemoveListener(Advance);
        nextButton.onClick.AddListener(Advance);

        Color accent = GetAccentColor(threatId);
        accentBar.color = accent;
        panelImage.color = Color.Lerp(SurfaceColor, accent, 0.07f);
        SetButtonColor(nextButton, accent);
        titleText.text = "KUIS " + threatId.ToUpperInvariant();

        modalRoot.SetActive(true);
        modalRoot.transform.SetAsLastSibling();
        ShowQuestion();
        ThreatQuizEvents.RaiseQuizStarted(threatId);
        return true;
    }

    public bool SubmitAnswer(int optionIndex)
    {
        if (!IsOpen || showingResults || hasAnswered || questions == null)
            return false;
        if (optionIndex < 0 || optionIndex >= answerButtons.Count)
            return false;

        ThreatQuizQuestion question = questions[currentQuestionIndex];
        bool correct = optionIndex == question.CorrectOptionIndex;
        hasAnswered = true;
        if (correct)
            score++;

        for (int i = 0; i < answerButtons.Count; i++)
        {
            answerButtons[i].interactable = false;
            if (i == question.CorrectOptionIndex)
                SetButtonColor(answerButtons[i], CorrectColor);
            else if (i == optionIndex)
                SetButtonColor(answerButtons[i], IncorrectColor);
        }

        feedbackPanel.SetActive(true);
        feedbackText.text = (correct ? "BENAR\n" : "BELUM TEPAT\n") + question.Explanation;
        feedbackText.color = correct
            ? new Color32(196, 255, 218, 255)
            : new Color32(255, 214, 216, 255);
        scoreText.text = "Skor: " + score;

        nextButton.gameObject.SetActive(true);
        SetButtonLabel(
            nextButton,
            currentQuestionIndex >= questions.Count - 1 ? "LIHAT HASIL" : "SOAL BERIKUTNYA");

        ThreatQuizEvents.RaiseAnswerSubmitted(currentThreatId, currentQuestionIndex, correct);
        return correct;
    }

    public void Advance()
    {
        if (!IsOpen || !hasAnswered || questions == null)
            return;

        if (currentQuestionIndex >= questions.Count - 1)
        {
            ShowResults();
            return;
        }

        currentQuestionIndex++;
        ShowQuestion();
    }

    public void RestartQuiz()
    {
        if (string.IsNullOrEmpty(currentThreatId))
            return;

        OpenQuiz(currentThreatId);
    }

    public void ReturnToEducation()
    {
        string threatId = currentThreatId;
        CloseQuiz();

        if (!string.IsNullOrEmpty(threatId) && ThreatEducationPanelController.Instance != null)
            ThreatEducationPanelController.Instance.OpenForThreat(threatId);
    }

    public void ResetAndRescan()
    {
        if (ThreatEducationPanelController.Instance != null)
        {
            ThreatEducationPanelController.Instance.ResetAndRescan();
            return;
        }

        CloseQuiz();
        if (ThreatInteractionManager.Instance != null)
            ThreatInteractionManager.Instance.ResetInteractions();
        ThreatEducationEvents.RaiseResetRequested();
    }

    public void CloseQuiz()
    {
        if (!IsOpen)
            return;

        string closedThreatId = currentThreatId;
        modalRoot.SetActive(false);
        currentThreatId = null;
        questions = null;
        currentQuestionIndex = 0;
        score = 0;
        hasAnswered = false;
        showingResults = false;
        ThreatQuizEvents.RaiseQuizClosed(closedThreatId);
    }

    private void HandleQuizRequested(string threatId)
    {
        OpenQuiz(threatId);
    }

    private void HandleResetRequested()
    {
        CloseQuiz();
    }

    private void ShowQuestion()
    {
        if (questions == null || currentQuestionIndex < 0 || currentQuestionIndex >= questions.Count)
            return;

        showingResults = false;
        hasAnswered = false;
        questionGroup.SetActive(true);
        resultGroup.SetActive(false);
        feedbackPanel.SetActive(false);
        nextButton.gameObject.SetActive(false);
        returnButton.gameObject.SetActive(true);
        SetButtonLabel(returnButton, "KEMBALI KE INFORMASI");

        ThreatQuizQuestion question = questions[currentQuestionIndex];
        progressText.text = "Soal " + (currentQuestionIndex + 1) + " dari " + questions.Count;
        scoreText.text = "Skor: " + score;
        questionText.text = question.Prompt;

        char optionLetter = 'A';
        for (int i = 0; i < answerButtons.Count; i++)
        {
            answerButtons[i].interactable = true;
            answerButtons[i].gameObject.SetActive(true);
            SetButtonColor(answerButtons[i], OptionColor);
            answerLabels[i].text = ((char)(optionLetter + i)) + ".  " + question.Options[i];
        }

        Canvas.ForceUpdateCanvases();
    }

    private void ShowResults()
    {
        showingResults = true;
        questionGroup.SetActive(false);
        feedbackPanel.SetActive(false);
        nextButton.gameObject.SetActive(true);
        returnButton.gameObject.SetActive(true);
        resultGroup.SetActive(true);

        progressText.text = "Kuis selesai";
        scoreText.text = "Skor akhir";
        resultTitleText.text = "HASIL KUIS " + currentThreatId.ToUpperInvariant();
        resultScoreText.text = score + " / " + questions.Count;
        resultMessageText.text = BuildResultMessage(score, questions.Count);
        SetButtonLabel(nextButton, "ULANGI KUIS");
        SetButtonLabel(returnButton, "KEMBALI KE INFORMASI");
        nextButton.onClick.RemoveListener(Advance);
        nextButton.onClick.AddListener(RestartFromResult);

        ThreatQuizEvents.RaiseQuizCompleted(currentThreatId, score, questions.Count);
        Canvas.ForceUpdateCanvases();
    }

    private void RestartFromResult()
    {
        nextButton.onClick.RemoveListener(RestartFromResult);
        nextButton.onClick.AddListener(Advance);
        RestartQuiz();
    }

    private void BuildInterface()
    {
        EnsureEventSystem();

        GameObject canvasObject = new GameObject(
            "ThreatQuizCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(UnityEngine.UI.CanvasScaler),
            typeof(UnityEngine.UI.GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 110;

        UnityEngine.UI.CanvasScaler scaler
            = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        safeAreaRect = CreateRect("SafeArea", canvasObject.transform);
        Stretch(safeAreaRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        modalRoot = new GameObject("QuizModal", typeof(RectTransform));
        modalRoot.transform.SetParent(safeAreaRect, false);
        Stretch(modalRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        UnityEngine.UI.Image overlay = CreateImage(
            "DimmedOverlay",
            modalRoot.transform,
            new Color(0f, 0f, 0f, 0.76f));
        Stretch(overlay.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        panelImage = CreateImage("Panel", modalRoot.transform, BackgroundColor);
        RectTransform panelRect = panelImage.rectTransform;
        Stretch(
            panelRect,
            new Vector2(0.045f, 0.035f),
            new Vector2(0.955f, 0.965f),
            Vector2.zero,
            Vector2.zero);
        UnityEngine.UI.Outline outline = panelImage.gameObject.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor = new Color(0.1f, 0.75f, 1f, 0.78f);
        outline.effectDistance = new Vector2(2f, -2f);

        RectTransform header = CreateRect("Header", panelRect);
        Stretch(header, new Vector2(0f, 1f), Vector2.one, new Vector2(30f, -120f), new Vector2(-30f, -18f));

        accentBar = CreateImage("AccentBar", header, new Color32(22, 199, 255, 255));
        SetAnchoredRect(accentBar.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(6f, 0f), new Vector2(0f, 0.5f));

        titleText = CreateText(
            "QuizTitle",
            header,
            "KUIS ANCAMAN",
            40,
            PrimaryTextColor,
            TextAnchor.UpperLeft,
            FontStyle.Bold);
        Stretch(titleText.rectTransform, new Vector2(0f, 0.42f), new Vector2(1f, 1f), new Vector2(24f, 0f), new Vector2(-88f, 0f));
        titleText.resizeTextForBestFit = true;
        titleText.resizeTextMinSize = 24;
        titleText.resizeTextMaxSize = 40;

        progressText = CreateText(
            "ProgressText",
            header,
            "Soal 1 dari 3",
            23,
            SecondaryTextColor,
            TextAnchor.LowerLeft,
            FontStyle.Normal);
        Stretch(progressText.rectTransform, Vector2.zero, new Vector2(0.65f, 0.5f), new Vector2(24f, 0f), Vector2.zero);

        scoreText = CreateText(
            "ScoreText",
            header,
            "Skor: 0/0",
            23,
            SecondaryTextColor,
            TextAnchor.LowerRight,
            FontStyle.Bold);
        Stretch(scoreText.rectTransform, new Vector2(0.6f, 0f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(-88f, 0f));

        UnityEngine.UI.Button closeButton = CreateButton(
            "CloseButton",
            header,
            "×",
            new Color32(43, 65, 86, 255),
            PrimaryTextColor,
            TextAnchor.MiddleCenter,
            27);
        SetAnchoredRect(closeButton.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(68f, 68f), new Vector2(-34f, -34f));
        closeButton.onClick.AddListener(ReturnToEducation);

        RectTransform footer = CreateRect("Footer", panelRect);
        Stretch(footer, Vector2.zero, new Vector2(1f, 0f), new Vector2(30f, 20f), new Vector2(-30f, 138f));
        UnityEngine.UI.HorizontalLayoutGroup footerLayout
            = footer.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
        footerLayout.spacing = 14f;
        footerLayout.childAlignment = TextAnchor.MiddleCenter;
        footerLayout.childControlWidth = true;
        footerLayout.childControlHeight = true;
        footerLayout.childForceExpandWidth = true;
        footerLayout.childForceExpandHeight = true;

        returnButton = CreateButton(
            "ReturnButton",
            footer,
            "KEMBALI KE INFORMASI",
            new Color32(49, 70, 92, 255),
            Color.white,
            TextAnchor.MiddleCenter,
            22);
        returnButton.onClick.AddListener(ReturnToEducation);

        resetButton = CreateButton(
            "ResetButton",
            footer,
            "RESET / PINDAI ULANG",
            new Color32(57, 72, 89, 255),
            Color.white,
            TextAnchor.MiddleCenter,
            22);
        resetButton.onClick.AddListener(ResetAndRescan);

        nextButton = CreateButton(
            "NextButton",
            footer,
            "SOAL BERIKUTNYA",
            new Color32(22, 167, 217, 255),
            Color.white,
            TextAnchor.MiddleCenter,
            22);
        nextButton.onClick.AddListener(Advance);

        RectTransform scrollView = CreateRect("ScrollView", panelRect);
        Stretch(scrollView, Vector2.zero, Vector2.one, new Vector2(30f, 150f), new Vector2(-30f, -130f));
        UnityEngine.UI.ScrollRect scrollRect
            = scrollView.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 36f;

        UnityEngine.UI.Image viewport = CreateImage(
            "Viewport",
            scrollView,
            new Color(1f, 1f, 1f, 0.01f));
        Stretch(viewport.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        UnityEngine.UI.Mask mask = viewport.gameObject.AddComponent<UnityEngine.UI.Mask>();
        mask.showMaskGraphic = false;

        RectTransform content = CreateRect("Content", viewport.transform);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = Vector2.zero;

        UnityEngine.UI.VerticalLayoutGroup contentLayout
            = content.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        contentLayout.padding = new RectOffset(8, 16, 8, 24);
        contentLayout.spacing = 13f;
        contentLayout.childAlignment = TextAnchor.UpperLeft;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;

        UnityEngine.UI.ContentSizeFitter fitter
            = content.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();
        fitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.viewport = viewport.rectTransform;
        scrollRect.content = content;

        questionGroup = new GameObject("QuestionGroup", typeof(RectTransform));
        questionGroup.transform.SetParent(content, false);
        UnityEngine.UI.VerticalLayoutGroup questionLayout
            = questionGroup.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        questionLayout.spacing = 13f;
        questionLayout.childAlignment = TextAnchor.UpperLeft;
        questionLayout.childControlWidth = true;
        questionLayout.childControlHeight = true;
        questionLayout.childForceExpandWidth = true;
        questionLayout.childForceExpandHeight = false;
        UnityEngine.UI.Text instruction = CreateText(
            "Instruction",
            questionGroup.transform,
            "PILIH SATU JAWABAN YANG PALING TEPAT",
            21,
            new Color32(83, 214, 255, 255),
            TextAnchor.MiddleLeft,
            FontStyle.Bold);
        AddLayout(instruction.gameObject, 38f, -1f);

        questionText = CreateText(
            "QuestionText",
            questionGroup.transform,
            string.Empty,
            29,
            PrimaryTextColor,
            TextAnchor.UpperLeft,
            FontStyle.Bold);
        questionText.horizontalOverflow = HorizontalWrapMode.Wrap;
        questionText.verticalOverflow = VerticalWrapMode.Overflow;
        questionText.lineSpacing = 1.12f;

        for (int i = 0; i < 4; i++)
        {
            int capturedIndex = i;
            UnityEngine.UI.Button answerButton = CreateButton(
                "Answer" + (char)('A' + i),
                questionGroup.transform,
                string.Empty,
                OptionColor,
                PrimaryTextColor,
                TextAnchor.MiddleLeft,
                24);
            AddLayout(answerButton.gameObject, 92f, 92f);
            answerButton.onClick.AddListener(() => SubmitAnswer(capturedIndex));
            answerButtons.Add(answerButton);
            answerLabels.Add(answerButton.transform.Find("Label").GetComponent<UnityEngine.UI.Text>());
        }

        feedbackPanel = CreateImage(
            "FeedbackPanel",
            content,
            new Color32(31, 53, 71, 255)).gameObject;
        UnityEngine.UI.VerticalLayoutGroup feedbackLayout
            = feedbackPanel.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        feedbackLayout.padding = new RectOffset(22, 22, 18, 18);
        feedbackLayout.childControlWidth = true;
        feedbackLayout.childControlHeight = true;
        feedbackLayout.childForceExpandWidth = true;
        feedbackLayout.childForceExpandHeight = false;
        feedbackText = CreateText(
            "FeedbackText",
            feedbackPanel.transform,
            string.Empty,
            24,
            PrimaryTextColor,
            TextAnchor.UpperLeft,
            FontStyle.Normal);
        feedbackText.horizontalOverflow = HorizontalWrapMode.Wrap;
        feedbackText.verticalOverflow = VerticalWrapMode.Overflow;
        feedbackText.lineSpacing = 1.12f;

        resultGroup = new GameObject("ResultGroup", typeof(RectTransform));
        resultGroup.transform.SetParent(content, false);
        UnityEngine.UI.VerticalLayoutGroup resultLayout
            = resultGroup.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        resultLayout.spacing = 18f;
        resultLayout.padding = new RectOffset(20, 20, 48, 48);
        resultLayout.childAlignment = TextAnchor.MiddleCenter;
        resultLayout.childControlWidth = true;
        resultLayout.childControlHeight = true;
        resultLayout.childForceExpandWidth = true;
        resultLayout.childForceExpandHeight = false;
        resultTitleText = CreateText(
            "ResultTitle",
            resultGroup.transform,
            "HASIL KUIS",
            30,
            new Color32(83, 214, 255, 255),
            TextAnchor.MiddleCenter,
            FontStyle.Bold);
        AddLayout(resultTitleText.gameObject, 52f, -1f);
        resultScoreText = CreateText(
            "ResultScore",
            resultGroup.transform,
            "0 / 3",
            64,
            PrimaryTextColor,
            TextAnchor.MiddleCenter,
            FontStyle.Bold);
        AddLayout(resultScoreText.gameObject, 94f, -1f);
        resultMessageText = CreateText(
            "ResultMessage",
            resultGroup.transform,
            string.Empty,
            27,
            PrimaryTextColor,
            TextAnchor.MiddleCenter,
            FontStyle.Normal);
        resultMessageText.horizontalOverflow = HorizontalWrapMode.Wrap;
        resultMessageText.verticalOverflow = VerticalWrapMode.Overflow;
        resultMessageText.lineSpacing = 1.15f;

        feedbackPanel.SetActive(false);
        resultGroup.SetActive(false);
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

    private UnityEngine.UI.Button CreateButton(
        string objectName,
        Transform parent,
        string label,
        Color background,
        Color foreground,
        TextAnchor alignment,
        int fontSize)
    {
        UnityEngine.UI.Image image = CreateImage(objectName, parent, background);
        UnityEngine.UI.Button button = image.gameObject.AddComponent<UnityEngine.UI.Button>();
        button.targetGraphic = image;

        UnityEngine.UI.ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.07f, 1.07f, 1.07f, 1f);
        colors.pressedColor = new Color(0.78f, 0.82f, 0.86f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = Color.white;
        button.colors = colors;

        UnityEngine.UI.Text text = CreateText(
            "Label",
            button.transform,
            label,
            fontSize,
            foreground,
            alignment,
            FontStyle.Bold);
        Stretch(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(20f, 10f), new Vector2(-20f, -10f));
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 16;
        text.resizeTextMaxSize = fontSize;
        text.raycastTarget = false;
        return button;
    }

    private UnityEngine.UI.Text CreateText(
        string objectName,
        Transform parent,
        string value,
        int fontSize,
        Color color,
        TextAnchor alignment,
        FontStyle style)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(UnityEngine.UI.Text));
        textObject.transform.SetParent(parent, false);
        UnityEngine.UI.Text label = textObject.GetComponent<UnityEngine.UI.Text>();
        label.font = runtimeFont;
        label.text = value;
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

    private static UnityEngine.UI.Image CreateImage(string objectName, Transform parent, Color color)
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

    private static void AddLayout(GameObject target, float preferredHeight, float minimumHeight)
    {
        UnityEngine.UI.LayoutElement layout = target.AddComponent<UnityEngine.UI.LayoutElement>();
        layout.preferredHeight = preferredHeight;
        if (minimumHeight >= 0f)
            layout.minHeight = minimumHeight;
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

    private static void SetButtonColor(UnityEngine.UI.Button button, Color color)
    {
        if (button != null && button.targetGraphic != null)
            button.targetGraphic.color = color;
    }

    private static void SetButtonLabel(UnityEngine.UI.Button button, string label)
    {
        if (button == null)
            return;

        UnityEngine.UI.Text text = button.GetComponentInChildren<UnityEngine.UI.Text>(true);
        if (text != null)
            text.text = label;
    }

    private static Color GetAccentColor(string threatId)
    {
        if (string.Equals(threatId, "Malware", StringComparison.OrdinalIgnoreCase))
            return new Color32(255, 84, 77, 255);
        if (string.Equals(threatId, "Ransomware", StringComparison.OrdinalIgnoreCase))
            return new Color32(255, 190, 54, 255);
        return new Color32(22, 199, 255, 255);
    }

    private static string BuildResultMessage(int resultScore, int total)
    {
        if (resultScore >= total)
            return "Sangat baik! Anda memahami ciri ancaman dan langkah pencegahannya.";
        if (resultScore * 2 >= total)
            return "Pemahaman Anda sudah cukup baik. Baca kembali penjelasan agar respons terhadap ancaman semakin tepat.";
        return "Mari pelajari kembali informasi ancaman, kemudian ulangi kuis untuk memperkuat pemahaman.";
    }
}
