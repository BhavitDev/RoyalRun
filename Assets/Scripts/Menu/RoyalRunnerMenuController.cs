using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Drives the Royal Runner title screen, its restrained idle motion, and the transition
/// into the existing gameplay scene.
/// </summary>
public sealed class RoyalRunnerMenuController : MonoBehaviour
{
    private sealed class GridFlowHighlight
    {
        public RectTransform rect;
        public Image image;
        public bool vertical;
        public float phase;
        public float segmentLength;
    }

    [Header("Scene transition")]
    [SerializeField] private string gameplaySceneName = "IndianMarketPreview";
    [SerializeField, Min(0.1f)] private float fadeDuration = 0.38f;

    [Header("Intro groups")]
    [SerializeField] private CanvasGroup menuGroup;
    [SerializeField] private CanvasGroup backgroundGroup;
    [SerializeField] private CanvasGroup characterGroup;
    [SerializeField] private CanvasGroup logoGroup;
    [SerializeField] private CanvasGroup titleGroup;
    [SerializeField] private CanvasGroup buttonGroup;
    [SerializeField] private CanvasGroup subtitleGroup;
    [SerializeField] private RectTransform characterVisual;
    [SerializeField] private RectTransform logoVisual;
    [SerializeField] private RectTransform titleVisual;
    [SerializeField] private RectTransform buttonVisual;

    [Header("Idle details")]
    [SerializeField] private Image gridImage;
    [SerializeField] private Graphic societyOutline;
    [SerializeField] private Image buttonImage;
    [SerializeField] private Button startButton;
    [SerializeField] private RectTransform[] driftingDecorations;
    [SerializeField] private RectTransform[] slowlyRotatingDecorations;

    private Vector2 characterBasePosition;
    private Vector2 logoBasePosition;
    private Vector3 logoBaseScale;
    private Vector2 titleBasePosition;
    private Vector2 buttonBasePosition;
    private Vector3 buttonBaseScale;
    private Vector2[] decorationBasePositions;
    private Vector3[] decorationBaseRotations;
    private Vector3[] rotatingBaseRotations;
    private readonly System.Collections.Generic.List<GridFlowHighlight> gridFlowHighlights =
        new System.Collections.Generic.List<GridFlowHighlight>();

    private bool introComplete;
    private bool transitionStarted;
    private bool buttonHovered;
    private bool buttonPressed;
    private float idleStartTime;

    private static readonly Color ButtonIdleColor = new Color32(182, 255, 50, 255);
    private static readonly Color ButtonBrightColor = new Color32(203, 255, 105, 255);

    public void Configure(CanvasGroup fullMenu, CanvasGroup background, CanvasGroup character,
        CanvasGroup logo, CanvasGroup title, CanvasGroup button, CanvasGroup subtitle,
        RectTransform characterRect, RectTransform logoRect, RectTransform titleRect,
        RectTransform buttonRect, Image grid, Graphic society, Image buttonGraphic,
        Button buttonComponent, RectTransform[] drifting, RectTransform[] rotating)
    {
        menuGroup = fullMenu;
        backgroundGroup = background;
        characterGroup = character;
        logoGroup = logo;
        titleGroup = title;
        buttonGroup = button;
        subtitleGroup = subtitle;
        characterVisual = characterRect;
        logoVisual = logoRect;
        titleVisual = titleRect;
        buttonVisual = buttonRect;
        gridImage = grid;
        societyOutline = society;
        buttonImage = buttonGraphic;
        startButton = buttonComponent;
        driftingDecorations = drifting;
        slowlyRotatingDecorations = rotating;
    }

    private void Awake()
    {
        Time.timeScale = 1f;

        if (startButton != null)
        {
            startButton.onClick.AddListener(BeginGameplayTransition);
            startButton.interactable = false;
        }

        if (menuGroup != null)
        {
            menuGroup.alpha = 1f;
            menuGroup.interactable = false;
            menuGroup.blocksRaycasts = false;
        }
        if (buttonGroup != null)
        {
            buttonGroup.interactable = false;
            buttonGroup.blocksRaycasts = false;
        }

        CacheIdlePositions();
    }

    private void OnDestroy()
    {
        if (startButton != null)
            startButton.onClick.RemoveListener(BeginGameplayTransition);
    }

    private IEnumerator Start()
    {
        CreateGridFlowHighlights();
        yield return Reveal(backgroundGroup, null, Vector2.zero, Vector2.zero, Vector3.one, Vector3.one, 0.16f);

        if (characterVisual != null)
            characterVisual.anchoredPosition = characterBasePosition + new Vector2(-34f, -7f);
        if (logoVisual != null)
            logoVisual.anchoredPosition = logoBasePosition + new Vector2(0f, -20f);

        StartCoroutine(Reveal(characterGroup, characterVisual,
            characterBasePosition + new Vector2(-34f, -7f), characterBasePosition,
            Vector3.one * 0.98f, Vector3.one, 0.56f));
        StartCoroutine(Reveal(logoGroup, logoVisual,
            logoBasePosition + new Vector2(0f, -20f), logoBasePosition,
            Vector3.one * 0.96f, Vector3.one, 0.52f));

        yield return WaitRealtime(0.31f);
        StartCoroutine(Reveal(titleGroup, titleVisual,
            titleBasePosition + new Vector2(0f, -16f), titleBasePosition,
            Vector3.one * 0.98f, Vector3.one, 0.39f));
        StartCoroutine(Reveal(subtitleGroup, null, Vector2.zero, Vector2.zero,
            Vector3.one, Vector3.one, 0.39f));

        yield return WaitRealtime(0.27f);
        StartCoroutine(Reveal(buttonGroup, buttonVisual,
            buttonBasePosition + new Vector2(0f, -10f), buttonBasePosition,
            Vector3.one * 0.86f, buttonBaseScale, 0.42f));

        yield return WaitRealtime(0.43f);
        introComplete = true;
        idleStartTime = Time.unscaledTime;

        if (menuGroup != null)
        {
            menuGroup.interactable = true;
            menuGroup.blocksRaycasts = true;
        }

        if (startButton != null)
        {
            startButton.interactable = true;
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(startButton.gameObject);
        }
        if (buttonGroup != null)
        {
            buttonGroup.interactable = true;
            buttonGroup.blocksRaycasts = true;
        }
    }

    private void Update()
    {
        if (!introComplete || transitionStarted)
            return;

        float time = Time.unscaledTime - idleStartTime;
        AnimateCharacterAndLogo(time);
        AnimateDecorations(time);
        AnimateButton(time);
        AnimateGridFlowHighlights();
    }

    private void CreateGridFlowHighlights()
    {
        gridFlowHighlights.Clear();
        if (gridImage == null)
            return;

        RectTransform gridRect = gridImage.rectTransform;
        const int verticalLineCount = 30;
        const int horizontalLineCount = 17;
        const float sourceGridHeight = 540f;
        const float sourceCellSize = 32f;
        const float firstHorizontalLine = 27f;

        for (int i = 1; i < verticalLineCount; i++)
        {
            float x = i / (float)verticalLineCount;
            AddGridFlowHighlight(gridRect, new Vector2(x, 0.5f), true, x, 132f);
        }

        for (int i = 0; i < horizontalLineCount; i++)
        {
            float sourceY = firstHorizontalLine + i * sourceCellSize;
            float y = 1f - sourceY / sourceGridHeight;
            AddGridFlowHighlight(gridRect, new Vector2(0.5f, y), false, 0.5f + i / (float)horizontalLineCount, 190f);
        }
    }

    private void AddGridFlowHighlight(Transform parent, Vector2 anchor, bool vertical, float phase, float segmentLength)
    {
        var go = new GameObject("Grid Line Flow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = anchor;
        rect.sizeDelta = vertical ? new Vector2(3f, segmentLength) : new Vector2(segmentLength, 3f);

        var image = go.GetComponent<Image>();
        image.color = new Color32(205, 143, 255, 0);
        image.raycastTarget = false;
        gridFlowHighlights.Add(new GridFlowHighlight
        {
            rect = rect,
            image = image,
            vertical = vertical,
            phase = phase,
            segmentLength = segmentLength
        });
    }

    private void AnimateGridFlowHighlights()
    {
        if (gridImage == null)
            return;

        const float sweepDuration = 12f;
        Rect gridBounds = gridImage.rectTransform.rect;
        float time = Time.unscaledTime / sweepDuration;

        for (int i = 0; i < gridFlowHighlights.Count; i++)
        {
            GridFlowHighlight item = gridFlowHighlights[i];
            if (item.rect == null || item.image == null)
                continue;

            float progress = Mathf.Repeat(time + item.phase, 1f);
            float axisLength = item.vertical ? gridBounds.height : gridBounds.width;
            float position = Mathf.Lerp(-axisLength * 0.5f + item.segmentLength * 0.5f,
                axisLength * 0.5f - item.segmentLength * 0.5f, progress);
            item.rect.anchoredPosition = item.vertical ? new Vector2(0f, position) : new Vector2(position, 0f);

            Color color = item.image.color;
            color.a = Mathf.Lerp(0.05f, 0.46f, Mathf.Sin(progress * Mathf.PI));
            item.image.color = color;
        }
    }

    private void CacheIdlePositions()
    {
        if (characterVisual != null)
            characterBasePosition = characterVisual.anchoredPosition;
        if (logoVisual != null)
        {
            logoBasePosition = logoVisual.anchoredPosition;
            logoBaseScale = logoVisual.localScale;
        }
        if (titleVisual != null)
            titleBasePosition = titleVisual.anchoredPosition;
        if (buttonVisual != null)
        {
            buttonBasePosition = buttonVisual.anchoredPosition;
            buttonBaseScale = buttonVisual.localScale;
        }

        int decorationCount = driftingDecorations == null ? 0 : driftingDecorations.Length;
        decorationBasePositions = new Vector2[decorationCount];
        decorationBaseRotations = new Vector3[decorationCount];
        for (int i = 0; i < decorationCount; i++)
        {
            RectTransform decoration = driftingDecorations[i];
            if (decoration == null)
                continue;
            decorationBasePositions[i] = decoration.anchoredPosition;
            decorationBaseRotations[i] = decoration.localEulerAngles;
        }

        int rotatingCount = slowlyRotatingDecorations == null ? 0 : slowlyRotatingDecorations.Length;
        rotatingBaseRotations = new Vector3[rotatingCount];
        for (int i = 0; i < rotatingCount; i++)
        {
            if (slowlyRotatingDecorations[i] != null)
                rotatingBaseRotations[i] = slowlyRotatingDecorations[i].localEulerAngles;
        }
    }

    private void AnimateCharacterAndLogo(float time)
    {
        if (characterVisual != null)
        {
            float phase = time * (Mathf.PI * 2f / 3.6f);
            characterVisual.anchoredPosition = characterBasePosition + new Vector2(
                Mathf.Sin(phase + 0.8f) * 2.5f,
                Mathf.Sin(phase) * 10f);
        }

        if (logoVisual != null)
        {
            float phase = time * (Mathf.PI * 2f / 4.1f) + 1.1f;
            logoVisual.anchoredPosition = logoBasePosition + new Vector2(0f, Mathf.Sin(phase) * 6f);
            float scale = 1f + Mathf.Sin(phase + 0.5f) * 0.012f;
            logoVisual.localScale = logoBaseScale * scale;
        }

        if (gridImage != null)
        {
            Color color = gridImage.color;
            color.a = 0.91f + Mathf.Sin(time * 0.75f) * 0.035f;
            gridImage.color = color;
        }

        if (societyOutline != null)
        {
            Color color = societyOutline.color;
            color.a = 0.47f + Mathf.Sin(time * 0.8f + 0.7f) * 0.055f;
            societyOutline.color = color;
        }
    }

    private void AnimateDecorations(float time)
    {
        if (driftingDecorations != null)
        {
            for (int i = 0; i < driftingDecorations.Length; i++)
            {
                RectTransform decoration = driftingDecorations[i];
                if (decoration == null)
                    continue;

                float phase = time * (0.62f + i * 0.09f) + i * 1.73f;
                decoration.anchoredPosition = decorationBasePositions[i] + new Vector2(
                    Mathf.Sin(phase * 0.72f) * (3f + i % 3),
                    Mathf.Sin(phase) * (5f + i % 2 * 2f));
                decoration.localEulerAngles = decorationBaseRotations[i] + new Vector3(0f, 0f,
                    Mathf.Sin(phase + 0.5f) * (i % 2 == 0 ? 2f : 3f));
            }
        }

        if (slowlyRotatingDecorations != null)
        {
            for (int i = 0; i < slowlyRotatingDecorations.Length; i++)
            {
                RectTransform decoration = slowlyRotatingDecorations[i];
                if (decoration == null)
                    continue;

                float phase = time * (Mathf.PI * 2f / (7f + i * 1.4f)) + i * 1.9f;
                decoration.localEulerAngles = rotatingBaseRotations[i] + new Vector3(0f, 0f,
                    Mathf.Sin(phase) * (i % 2 == 0 ? 3.5f : 2.5f));
            }
        }
    }

    private void AnimateButton(float time)
    {
        if (buttonVisual != null)
        {
            float idlePulse = 1f + (0.0125f * (0.5f + 0.5f * Mathf.Sin(time * Mathf.PI)));
            float targetScale = buttonPressed ? 0.96f : buttonHovered ? 1.06f : idlePulse;
            float smoothFactor = 1f - Mathf.Exp(-12f * Time.unscaledDeltaTime);
            buttonVisual.localScale = Vector3.Lerp(buttonVisual.localScale, buttonBaseScale * targetScale, smoothFactor);
        }

        if (buttonImage != null)
        {
            float pulse = 0.18f + 0.18f * (0.5f + 0.5f * Mathf.Sin(time * Mathf.PI));
            Color targetColor = Color.Lerp(ButtonIdleColor, ButtonBrightColor, pulse);
            if (buttonHovered)
                targetColor = Color.Lerp(targetColor, Color.white, 0.14f);
            float smoothFactor = 1f - Mathf.Exp(-10f * Time.unscaledDeltaTime);
            buttonImage.color = Color.Lerp(buttonImage.color, targetColor, smoothFactor);
        }
    }

    private IEnumerator Reveal(CanvasGroup group, RectTransform visual,
        Vector2 startPosition, Vector2 endPosition,
        Vector3 startScale, Vector3 endScale, float duration)
    {
        if (group == null)
            yield break;

        group.alpha = 0f;
        if (visual != null)
        {
            visual.anchoredPosition = startPosition;
            visual.localScale = startScale;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = t * t * (3f - 2f * t);
            group.alpha = eased;
            if (visual != null)
            {
                visual.anchoredPosition = Vector2.LerpUnclamped(startPosition, endPosition, eased);
                visual.localScale = Vector3.LerpUnclamped(startScale, endScale, eased);
            }
            yield return null;
        }

        group.alpha = 1f;
        if (visual != null)
        {
            visual.anchoredPosition = endPosition;
            visual.localScale = endScale;
        }
    }

    private static IEnumerator WaitRealtime(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    public void SetButtonHover(bool hovered)
    {
        buttonHovered = hovered;
        if (!hovered)
            buttonPressed = false;
    }

    public void SetButtonPressed(bool pressed)
    {
        buttonPressed = pressed;
    }

    private void BeginGameplayTransition()
    {
        if (!introComplete || transitionStarted)
            return;

        transitionStarted = true;
        StartCoroutine(FadeAndLoadGameplay());
    }

    private IEnumerator FadeAndLoadGameplay()
    {
        if (startButton != null)
            startButton.interactable = false;
        if (buttonGroup != null)
        {
            buttonGroup.interactable = false;
            buttonGroup.blocksRaycasts = false;
        }
        if (menuGroup != null)
        {
            menuGroup.interactable = false;
            menuGroup.blocksRaycasts = false;
        }

        float elapsed = 0f;
        float startAlpha = menuGroup != null ? menuGroup.alpha : 1f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            if (menuGroup != null)
                menuGroup.alpha = Mathf.Lerp(startAlpha, 0f, t * t * (3f - 2f * t));
            yield return null;
        }

        if (!Application.CanStreamedLevelBeLoaded(gameplaySceneName))
        {
            Debug.LogError($"The gameplay scene '{gameplaySceneName}' is not enabled in Build Settings.", this);
            RestoreMenuAfterLoadFailure();
            yield break;
        }

        AsyncOperation load = SceneManager.LoadSceneAsync(gameplaySceneName, LoadSceneMode.Single);
        if (load == null)
        {
            Debug.LogError($"Unity could not begin loading gameplay scene '{gameplaySceneName}'.", this);
            RestoreMenuAfterLoadFailure();
            yield break;
        }

        while (!load.isDone)
            yield return null;
    }

    private void RestoreMenuAfterLoadFailure()
    {
        transitionStarted = false;
        if (menuGroup != null)
        {
            menuGroup.alpha = 1f;
            menuGroup.interactable = true;
            menuGroup.blocksRaycasts = true;
        }
        if (startButton != null)
            startButton.interactable = true;
        if (buttonGroup != null)
        {
            buttonGroup.interactable = true;
            buttonGroup.blocksRaycasts = true;
        }
    }
}

/// <summary>Forwards pointer feedback from the Start button to the menu animation controller.</summary>
public sealed class RoyalRunnerMenuButtonFeedback : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private RoyalRunnerMenuController controller;

    public void Bind(RoyalRunnerMenuController menuController)
    {
        controller = menuController;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (controller != null)
            controller.SetButtonHover(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (controller != null)
            controller.SetButtonHover(false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (controller != null)
            controller.SetButtonPressed(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (controller != null)
            controller.SetButtonPressed(false);
    }
}
