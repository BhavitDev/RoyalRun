using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public sealed class EndGameLeaderboardUI : MonoBehaviour
{
    private sealed class FloatingMotion
    {
        public RectTransform rect;
        public Vector2 origin;
        public float phase;
        public float speed;
        public float amplitude;
    }

    private sealed class GridHighlight
    {
        public RectTransform rect;
        public Image image;
        public bool vertical;
        public float phase;
        public float length;
    }

    private const float IdleSeconds = 30f;
    private static EndGameLeaderboardUI instance;
    private static int pendingScore;
    private int score;
    private float idle;
    private bool submitted;
    private bool leaderboardShown;
    private TMP_InputField nameInput;
    private CanvasGroup rootGroup;
    private Canvas activeCanvas;
    private GameObject persistentEventSystem;
    private readonly List<FloatingMotion> floatingDecorations = new List<FloatingMotion>();
    private readonly List<GridHighlight> gridHighlights = new List<GridHighlight>();
    private static readonly Color Purple = new Color32(29, 7, 48, 255);
    private static readonly Color Violet = new Color32(171, 55, 226, 255);
    private static readonly Color Lime = new Color32(182, 255, 50, 255);

    public static void Show(int finalScore)
    {
        if (instance != null) return;
        pendingScore = Mathf.Max(0, finalScore);
        var host = new GameObject("End Game Scorecard");
        instance = host.AddComponent<EndGameLeaderboardUI>();
        instance.score = pendingScore;
        var displayCamera = host.AddComponent<Camera>();
        displayCamera.clearFlags = CameraClearFlags.SolidColor;
        displayCamera.backgroundColor = new Color32(19, 5, 32, 255);
        displayCamera.cullingMask = 1 << LayerMask.NameToLayer("Default");
        displayCamera.depth = -100f;
        displayCamera.allowHDR = false;
        displayCamera.allowMSAA = false;
        DontDestroyOnLoad(host);
        if (EventSystem.current != null)
        {
            instance.persistentEventSystem = EventSystem.current.gameObject;
            DontDestroyOnLoad(instance.persistentEventSystem);
        }
        SceneManager.sceneLoaded += instance.OnSceneLoaded;
        instance.ShowGameOverNotice();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (persistentEventSystem != null)
            Destroy(persistentEventSystem);
        if (instance == this)
            instance = null;
    }

    private void ShowGameOverNotice()
    {
        activeCanvas = MakeCanvas("Game Over Notice");
        var bg = Panel(activeCanvas.transform, Vector2.zero, new Vector2(0.5f, 0.5f), new Color32(18, 5, 32, 245));
        bg.GetComponent<RectTransform>().sizeDelta = new Vector2(720, 220);
        Text(activeCanvas.transform, "GAME OVER", 64, Vector2.zero, new Vector2(700, 110), Lime);
        StartCoroutine(LoadScorecardAfterNotice());
    }

    private System.Collections.IEnumerator LoadScorecardAfterNotice()
    {
        yield return new WaitForSecondsRealtime(0.85f);
        if (activeCanvas != null) Destroy(activeCanvas.gameObject);
        AsyncOperation load = SceneManager.LoadSceneAsync("LeaderboardScene", LoadSceneMode.Single);
        if (load == null) { Debug.LogError("LeaderboardScene is missing from Build Settings."); yield break; }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "LeaderboardScene") { score = pendingScore; BuildNameEntry(); }
    }

    private void Update()
    {
        idle += Time.unscaledDeltaTime;
        if (Keyboard.current != null && (Keyboard.current.anyKey.wasPressedThisFrame))
            idle = 0f;
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            idle = 0f;
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            idle = 0f;
        if (idle >= IdleSeconds) ReturnHome();
        if (!leaderboardShown && nameInput != null && Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
            SubmitName();
        AnimateFloatingDecorations();
        AnimateGridHighlights();
    }

    private void BuildNameEntry()
    {
        Canvas canvas = MakeCanvas("Name Entry Canvas");
        activeCanvas = canvas;
        rootGroup = canvas.GetComponent<CanvasGroup>();
        var backdrop = Panel(canvas.transform, Vector2.zero, new Vector2(0.5f, 0.5f), new Color32(19, 5, 33, 238));
        backdrop.GetComponent<RectTransform>().sizeDelta = new Vector2(1120, 680);
        backdrop.GetComponent<Image>().raycastTarget = false;
        Text(canvas.transform, "GAME OVER", 58, new Vector2(0, 300), new Vector2(800, 80), Lime);
        Text(canvas.transform, "YOUR SCORE  " + score.ToString("N0"), 40, new Vector2(0, 210), new Vector2(800, 60), Color.white);
        Text(canvas.transform, "Enter your name to join the leaderboard!", 28, new Vector2(0, 120), new Vector2(900, 50), Color.white);
        var fieldGo = Panel(canvas.transform, new Vector2(0, 32), new Vector2(0.5f, 0.5f), Color.white);
        RectTransform fr = fieldGo.GetComponent<RectTransform>(); fr.sizeDelta = new Vector2(650, 78);
        nameInput = fieldGo.AddComponent<TMP_InputField>();
        nameInput.targetGraphic = fieldGo.GetComponent<Image>();
        nameInput.characterLimit = 16;
        nameInput.contentType = TMP_InputField.ContentType.Standard;
        nameInput.lineType = TMP_InputField.LineType.SingleLine;
        var viewportGo = new GameObject("Text Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewportGo.transform.SetParent(fieldGo.transform, false);
        var viewportRect = viewportGo.GetComponent<RectTransform>();
        viewportRect.anchorMin = viewportRect.anchorMax = new Vector2(0.5f, 0.5f);
        viewportRect.anchoredPosition = Vector2.zero;
        viewportRect.sizeDelta = new Vector2(610, 62);
        nameInput.textViewport = viewportRect;
        var text = Text(viewportGo.transform, "", 32, Vector2.zero, new Vector2(610, 62), Purple);
        nameInput.textComponent = text;
        var placeholder = Text(viewportGo.transform, "Type your name here", 30, Vector2.zero, new Vector2(610, 62), new Color(0.4f, 0.35f, 0.45f));
        nameInput.placeholder = placeholder;
        Button(canvas.transform, "SUBMIT SCORE", new Vector2(0, -90), new Vector2(380, 82), Lime, SubmitName);
        Text(canvas.transform, "Press Enter to submit", 20, new Vector2(0, -170), new Vector2(500, 34), Color.white);
        AddLeaderboardDecorations(canvas.transform);
        nameInput.ActivateInputField();
        StartCoroutine(FadeIn());
    }

    private void SubmitName()
    {
        if (submitted || leaderboardShown || nameInput == null) return;
        string playerName = (nameInput.text ?? "").Trim();
        if (playerName.Length == 0) { nameInput.ActivateInputField(); return; }
        submitted = true;
        idle = 0;
        List<ScoreRecord> rows;
        int rank = LocalLeaderboard.Submit(playerName, score, out rows);
        leaderboardShown = true;
        BuildLeaderboard(playerName, rank, rows);
    }

    private void BuildLeaderboard(string playerName, int rank, List<ScoreRecord> rows)
    {
        if (activeCanvas != null) Destroy(activeCanvas.gameObject);
        Canvas canvas = MakeCanvas("Leaderboard Canvas");
        activeCanvas = canvas;
        rootGroup = canvas.GetComponent<CanvasGroup>();
        var tableBackdrop = Panel(canvas.transform, Vector2.zero, new Vector2(0.5f, 0.5f), new Color32(20, 5, 34, 240));
        tableBackdrop.GetComponent<RectTransform>().sizeDelta = new Vector2(1550, 680);
        tableBackdrop.GetComponent<Image>().raycastTarget = false;
        Text(canvas.transform, "LEADERBOARD", 54, new Vector2(0, 390), new Vector2(1040, 82), Lime);
        Text(canvas.transform, "PLAYER: " + playerName + "     YOUR SCORE: " + score.ToString("N0") + "     YOUR RANK: #" + rank,
            25, new Vector2(0, 320), new Vector2(1300, 48), Color.white);
        Text(canvas.transform, "#                         PLAYER                                      HIGH SCORE", 23,
            new Vector2(0, 265), new Vector2(1250, 42), Color.white);
        for (int i = 0; i < Mathf.Min(10, rows.Count); i++)
        {
            var row = rows[i];
            float rowY = 215 - i * 44;
            var rowBackground = Panel(canvas.transform, new Vector2(0, rowY), new Vector2(0.5f, 0.5f),
                i == 0 ? Lime : new Color32(34, 12, 51, 245));
            rowBackground.GetComponent<RectTransform>().sizeDelta = new Vector2(1390, 40);
            rowBackground.GetComponent<Image>().raycastTarget = false;
            Color color = i == 0 ? Purple : i == 1 ? new Color32(215, 220, 235, 255) : i == 2 ? new Color32(240, 170, 84, 255) : Lime;
            Text(canvas.transform, (i + 1).ToString().PadLeft(2) + "          " + row.name + "                                      " + row.score.ToString("N0"),
                24, new Vector2(0, rowY), new Vector2(1200, 40), color);
        }
        if (rows.Count == 0) Text(canvas.transform, "No scores yet. Be the first!", 30, new Vector2(0, 150), new Vector2(800, 50), Color.white);
        Button(canvas.transform, "HOME", new Vector2(-220, -410), new Vector2(300, 72), Violet, ReturnHome);
        Button(canvas.transform, "PLAY AGAIN", new Vector2(220, -410), new Vector2(360, 72), Lime, PlayAgain);
        AddLeaderboardDecorations(canvas.transform);
        StartCoroutine(FadeIn());
    }

    private void AddLeaderboardDecorations(Transform parent)
    {
        floatingDecorations.Clear();
        AddTriangle(parent, new Vector2(0.15f, 0.77f), new Color32(211, 45, 235, 255), 1.1f);
        AddTriangle(parent, new Vector2(0.86f, 0.72f), Lime, 1.4f);
        AddCross(parent, new Vector2(0.08f, 0.51f), new Color32(211, 45, 235, 255), 0.9f);
        AddCross(parent, new Vector2(0.90f, 0.49f), new Color32(211, 45, 235, 255), 1.2f);
    }

    private void AddTriangle(Transform parent, Vector2 anchor, Color color, float speed)
    {
        var go = new GameObject("Floating Triangle", typeof(RectTransform), typeof(CanvasRenderer), typeof(MenuTriangleGraphic));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = anchor; rect.sizeDelta = new Vector2(42, 42);
        var graphic = go.GetComponent<MenuTriangleGraphic>(); graphic.PointRight(true); graphic.color = color;
        floatingDecorations.Add(new FloatingMotion { rect = rect, origin = Vector2.zero, phase = floatingDecorations.Count * 1.7f, speed = speed, amplitude = 10f });
    }

    private void AddCross(Transform parent, Vector2 anchor, Color color, float speed)
    {
        var go = new GameObject("Floating Cross", typeof(RectTransform)); go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = anchor; rect.sizeDelta = new Vector2(38, 38);
        AddCrossBar(go.transform, color, 0f); AddCrossBar(go.transform, color, 90f);
        floatingDecorations.Add(new FloatingMotion { rect = rect, origin = Vector2.zero, phase = floatingDecorations.Count * 2.2f, speed = speed, amplitude = 12f });
    }

    private static void AddCrossBar(Transform parent, Color color, float angle)
    {
        var bar = new GameObject("Cross Bar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); bar.transform.SetParent(parent, false);
        var rect = bar.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f); rect.sizeDelta = new Vector2(36, 5); rect.localEulerAngles = new Vector3(0, 0, angle);
        var image = bar.GetComponent<Image>(); image.color = color; image.raycastTarget = false;
    }

    private void AnimateFloatingDecorations()
    {
        float time = Time.unscaledTime;
        for (int i = 0; i < floatingDecorations.Count; i++)
        {
            FloatingMotion item = floatingDecorations[i];
            if (item.rect == null) continue;
            float phase = time * item.speed + item.phase;
            item.rect.anchoredPosition = item.origin + new Vector2(Mathf.Sin(phase * 0.65f) * 7f, Mathf.Sin(phase) * item.amplitude);
            item.rect.localEulerAngles = new Vector3(0f, 0f, Mathf.Sin(phase * 0.8f) * 4f);
        }
    }

    private Canvas MakeCanvas(string canvasName)
    {
        gridHighlights.Clear();
        var go = new GameObject(canvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        go.transform.SetParent(transform, false);
        var canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 0.5f;
        var background = new GameObject("Purple Grid Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        background.transform.SetParent(go.transform, false);
        var rect = background.GetComponent<RectTransform>(); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        background.GetComponent<Image>().color = new Color32(19, 5, 32, 255);
        background.GetComponent<Image>().raycastTarget = false;
        for (int i = 1; i < 16; i++) AddGridLine(go.transform, new Vector2(i / 16f, 0), new Vector2(i / 16f, 1), new Vector2(2, 1080), 0.35f, true, i / 16f);
        for (int i = 1; i < 10; i++) AddGridLine(go.transform, new Vector2(0, i / 10f), new Vector2(1, i / 10f), new Vector2(1920, 2), 0.35f, false, i / 10f + 0.5f);
        return canvas;
    }

    private void AddGridLine(Transform parent, Vector2 start, Vector2 end, Vector2 size, float alpha, bool vertical, float phase)
    {
        var go = new GameObject("Grid Line", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = (start + end) * 0.5f; rect.sizeDelta = size;
        go.GetComponent<Image>().color = new Color32(96, 43, 125, (byte)(255 * alpha)); go.GetComponent<Image>().raycastTarget = false;

        var highlight = new GameObject("Grid Line Flow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); highlight.transform.SetParent(go.transform, false);
        var highlightRect = highlight.GetComponent<RectTransform>();
        highlightRect.anchorMin = highlightRect.anchorMax = new Vector2(0.5f, 0.5f);
        float segmentLength = vertical ? 132f : 190f;
        highlightRect.sizeDelta = vertical ? new Vector2(3f, segmentLength) : new Vector2(segmentLength, 3f);
        var highlightImage = highlight.GetComponent<Image>();
        highlightImage.color = new Color32(205, 143, 255, 0);
        highlightImage.raycastTarget = false;
        gridHighlights.Add(new GridHighlight { rect = highlightRect, image = highlightImage, vertical = vertical, phase = phase, length = size.y > size.x ? size.y : size.x });
    }

    private void AnimateGridHighlights()
    {
        const float sweepDuration = 12f;
        float time = Time.unscaledTime / sweepDuration;
        for (int i = 0; i < gridHighlights.Count; i++)
        {
            GridHighlight item = gridHighlights[i];
            if (item.rect == null || item.image == null) continue;

            float progress = Mathf.Repeat(time + item.phase, 1f);
            float segmentLength = item.vertical ? item.rect.sizeDelta.y : item.rect.sizeDelta.x;
            float position = Mathf.Lerp(-item.length * 0.5f + segmentLength * 0.5f, item.length * 0.5f - segmentLength * 0.5f, progress);
            item.rect.anchoredPosition = item.vertical ? new Vector2(0f, position) : new Vector2(position, 0f);
            Color color = item.image.color;
            color.a = Mathf.Lerp(0.05f, 0.46f, Mathf.Sin(progress * Mathf.PI));
            item.image.color = color;
        }
    }

    private static GameObject Panel(Transform parent, Vector2 position, Vector2 anchor, Color color)
    {
        var go = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = anchor; rect.anchoredPosition = position; rect.sizeDelta = new Vector2(1550, 920);
        go.GetComponent<Image>().color = color; return go;
    }

    private static TMP_Text Text(Transform parent, string value, float size, Vector2 position, Vector2 dimensions, Color color)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)); go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f); rect.anchoredPosition = position; rect.sizeDelta = dimensions;
        var text = go.GetComponent<TextMeshProUGUI>(); text.font = TMP_Settings.defaultFontAsset; text.text = value; text.fontSize = size;
        text.color = color; text.alignment = TextAlignmentOptions.Center; text.enableAutoSizing = true; text.fontSizeMin = size * 0.6f; text.fontSizeMax = size;
        return text;
    }

    private static void Button(Transform parent, string label, Vector2 position, Vector2 dimensions, Color color, UnityEngine.Events.UnityAction action)
    {
        var go = Panel(parent, position, new Vector2(0.5f, 0.5f), color); go.GetComponent<RectTransform>().sizeDelta = dimensions;
        var button = go.AddComponent<Button>(); button.targetGraphic = go.GetComponent<Image>(); button.onClick.AddListener(action);
        Text(go.transform, label, 30, Vector2.zero, dimensions, Purple);
    }

    private System.Collections.IEnumerator FadeIn()
    {
        if (rootGroup == null) yield break;
        rootGroup.alpha = 0;
        for (float t = 0; t < 0.28f; t += Time.unscaledDeltaTime) { rootGroup.alpha = Mathf.Clamp01(t / 0.28f); yield return null; }
        rootGroup.alpha = 1;
    }

    private void ReturnHome()
    {
        Time.timeScale = 1f;
        RemovePersistentEventSystem();
        SceneManager.sceneLoaded -= OnSceneLoaded; instance = null; Destroy(gameObject);
        SceneManager.LoadScene("RoyalRunnerMenu");
    }

    private void PlayAgain()
    {
        Time.timeScale = 1f;
        RemovePersistentEventSystem();
        SceneManager.sceneLoaded -= OnSceneLoaded; instance = null; Destroy(gameObject);
        SceneManager.LoadScene("IndianMarketPreview");
    }

    private void RemovePersistentEventSystem()
    {
        if (persistentEventSystem != null)
            Destroy(persistentEventSystem);
    }
}
