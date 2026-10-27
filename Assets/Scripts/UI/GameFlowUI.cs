using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runtime-built presentation layer for the level flow: briefing card, wave
/// banners, clear/defeat/victory screens and a small wave tracker. Built in code
/// so the scene does not need hand-authored UI.
/// </summary>
public class GameFlowUI : MonoBehaviour
{
    public static GameFlowUI Instance { get; private set; }

    private static readonly Color OverlayColor = new Color(0.02f, 0.03f, 0.05f, 0.88f);
    private static readonly Color PanelColor = new Color(0.05f, 0.07f, 0.11f, 0.97f);
    private static readonly Color AccentColor = new Color(0.35f, 0.95f, 0.75f, 1f);
    private static readonly Color DangerColor = new Color(1f, 0.42f, 0.38f, 1f);
    private static readonly Color TextColor = new Color(0.92f, 0.96f, 1f, 1f);
    private static readonly Color MutedColor = new Color(0.60f, 0.68f, 0.78f, 1f);
    private static readonly Color PrimaryColor = new Color(0.16f, 0.60f, 0.47f, 1f);
    private static readonly Color SecondaryColor = new Color(0.20f, 0.24f, 0.31f, 1f);

    private TMP_FontAsset font;

    private GameObject overlay;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI subtitleText;
    private TextMeshProUGUI bodyText;
    private TextMeshProUGUI rewardText;
    private Button primaryButton;
    private TextMeshProUGUI primaryLabel;
    private Button secondaryButton;
    private TextMeshProUGUI secondaryLabel;

    private GameObject bannerRoot;
    private CanvasGroup bannerGroup;
    private TextMeshProUGUI bannerText;
    private TextMeshProUGUI bannerSubText;
    private Coroutine bannerRoutine;

    private TextMeshProUGUI progressText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        font = ResolveFont();
        BuildUI();
        HideOverlay();
        HideBanner();
        HideProgress();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ------------------------------------------------------------- public API

    public void ShowIntro(WaveConfigSO level, int levelIndex, int levelCount)
    {
        ShowOverlay();

        string title = string.IsNullOrEmpty(level.levelName) ? "SECTOR" : level.levelName.ToUpperInvariant();
        string subtitle = level.isTutorial
            ? "TRAINING GROUND"
            : "SECTOR " + (levelIndex + 1) + " OF " + levelCount;

        SetContent(
            title,
            subtitle,
            string.IsNullOrEmpty(level.briefing)
                ? "Plant your bots, collect photons and hold the line."
                : level.briefing,
            string.Empty,
            "START",
            () => GameManager.Instance.BeginPlay(),
            null,
            null);

        HideProgress();
    }

    public void ShowWaveBanner(int waveIndex, int totalWaves, bool huge)
    {
        if (bannerRoutine != null) StopCoroutine(bannerRoutine);
        bannerRoutine = StartCoroutine(BannerRoutine(waveIndex, totalWaves, huge));
    }

    public void UpdateWaveProgress(int wavesStarted, int totalWaves, string levelName)
    {
        if (progressText == null) return;

        progressText.gameObject.SetActive(true);
        string name = string.IsNullOrEmpty(levelName) ? "SECTOR" : levelName.ToUpperInvariant();
        progressText.text = name + "   <color=#8FB3C7>WAVE " + Mathf.Clamp(wavesStarted, 0, totalWaves) + " / " + totalWaves + "</color>";
    }

    public void ShowLevelCleared(WaveConfigSO level, List<SeedSO> rewards, int nextLevelIndex, int levelCount, float elapsedSeconds)
    {
        ShowOverlay();
        HideProgress();

        string title = level != null && level.isTutorial ? "TRAINING COMPLETE" : "SECTOR CLEARED";
        string subtitle = level != null && level.isTutorial
            ? "You are ready for the swarm"
            : "Sector " + (nextLevelIndex) + " of " + levelCount + " held";

        string body = "Clear time  " + FormatTime(elapsedSeconds) +
                      "\nZombies repelled  " + (level != null ? level.TotalZombieCount : 0);

        string reward = BuildRewardText(rewards);

        SetContent(
            title,
            subtitle,
            body,
            reward,
            nextLevelIndex >= levelCount ? "FINISH" : "NEXT SECTOR",
            () => GameManager.Instance.ContinueToNextLevel(),
            "REPLAY",
            () => GameManager.Instance.RetryLevel());
    }

    public void ShowDefeat(WaveConfigSO level, int wavesReached, int totalWaves)
    {
        ShowOverlay();
        HideProgress();

        string title = "THE LINE BROKE";
        string subtitle = level != null ? level.levelName : "Sector";
        string body = "An alien reached the house." +
                      "\nWave " + Mathf.Clamp(wavesReached, 1, Mathf.Max(1, totalWaves)) + " of " + totalWaves + " reached." +
                      "\n\n" + (level != null && level.isTutorial
                          ? "Deploy a PulseBot early and keep it firing."
                          : "Build SolarCores early, then layer attackers in front of a ShieldCore.");

        SetContent(title, subtitle, body, string.Empty, "RETRY", () => GameManager.Instance.RetryLevel(), null, null);
    }

    public void ShowGameComplete(int levelCount)
    {
        ShowOverlay();
        HideProgress();

        SetContent(
            "EARTH IS SAFE",
            "All " + levelCount + " sectors held",
            "The swarm is broken and the house is still standing." +
            "\n\nEvery bot did its part: PulseBots, SolarCores, SpikeMines, ShieldCores and CrushPods.",
            "<color=#59F2BF>CAMPAIGN COMPLETE</color>",
            "PLAY AGAIN",
            () => GameManager.Instance.PlayAgainFromStart(),
            "REPLAY FINALE",
            () => GameManager.Instance.ReplayFinale());
    }

    public void HideOverlay()
    {
        if (overlay != null) overlay.SetActive(false);
    }

    public void HideBanner()
    {
        if (bannerRoot != null) bannerRoot.SetActive(false);
    }

    public void HideProgress()
    {
        if (progressText != null) progressText.gameObject.SetActive(false);
    }

    // ------------------------------------------------------------- building

    private void BuildUI()
    {
        if (GetComponent<Canvas>() == null)
        {
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            gameObject.AddComponent<CanvasScaler>();
            gameObject.AddComponent<GraphicRaycaster>();
        }

        CanvasScaler scaler = GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        BuildOverlay();
        BuildBanner();
        BuildProgress();
    }

    private void BuildOverlay()
    {
        RectTransform root = NewRect("Overlay", transform);
        Stretch(root);
        overlay = root.gameObject;

        Image dim = overlay.AddComponent<Image>();
        dim.color = OverlayColor;
        dim.raycastTarget = true;

        RectTransform panel = NewRect("Panel", root);
        Center(panel, new Vector2(1240f, 700f), Vector2.zero);

        Image panelImage = panel.gameObject.AddComponent<Image>();
        panelImage.color = PanelColor;
        panelImage.raycastTarget = true;

        RectTransform accent = NewRect("Accent", panel);
        accent.anchorMin = new Vector2(0f, 1f);
        accent.anchorMax = new Vector2(1f, 1f);
        accent.pivot = new Vector2(0.5f, 1f);
        accent.offsetMin = new Vector2(0f, -6f);
        accent.offsetMax = Vector2.zero;
        Image accentImage = accent.gameObject.AddComponent<Image>();
        accentImage.color = AccentColor;
        accentImage.raycastTarget = false;

        titleText = NewText("Title", panel, 76f, TextColor, TextAlignmentOptions.Center);
        Center(titleText.rectTransform, new Vector2(1120f, 120f), new Vector2(0f, 250f));

        subtitleText = NewText("Subtitle", panel, 30f, AccentColor, TextAlignmentOptions.Center);
        Center(subtitleText.rectTransform, new Vector2(1120f, 56f), new Vector2(0f, 172f));

        bodyText = NewText("Body", panel, 30f, TextColor, TextAlignmentOptions.Center);
        Center(bodyText.rectTransform, new Vector2(1040f, 190f), new Vector2(0f, 34f));

        rewardText = NewText("Reward", panel, 27f, AccentColor, TextAlignmentOptions.Center);
        Center(rewardText.rectTransform, new Vector2(1040f, 96f), new Vector2(0f, -124f));

        primaryButton = NewButton("PrimaryButton", panel, PrimaryColor, new Vector2(420f, 92f), new Vector2(0f, -258f), out primaryLabel);
        secondaryButton = NewButton("SecondaryButton", panel, SecondaryColor, new Vector2(340f, 92f), new Vector2(392f, -258f), out secondaryLabel);

        // Listeners are assigned per screen so one button can mean
        // Start / Next / Retry without stacking callbacks.
        primaryButton.onClick.RemoveAllListeners();
        secondaryButton.onClick.RemoveAllListeners();
    }

    private void BuildBanner()
    {
        RectTransform rect = NewRect("WaveBanner", transform);
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(980f, 150f);
        rect.anchoredPosition = new Vector2(0f, -70f);

        bannerRoot = rect.gameObject;
        bannerGroup = bannerRoot.AddComponent<CanvasGroup>();
        bannerGroup.blocksRaycasts = false;
        bannerGroup.interactable = false;

        bannerText = NewText("BannerText", rect, 66f, TextColor, TextAlignmentOptions.Center);
        Center(bannerText.rectTransform, new Vector2(980f, 76f), new Vector2(0f, 26f));

        bannerSubText = NewText("BannerSub", rect, 26f, MutedColor, TextAlignmentOptions.Center);
        Center(bannerSubText.rectTransform, new Vector2(980f, 40f), new Vector2(0f, -36f));
    }

    private void BuildProgress()
    {
        RectTransform rect = NewRect("WaveProgress", transform);
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = new Vector2(900f, 48f);
        rect.anchoredPosition = new Vector2(0f, 18f);

        progressText = rect.gameObject.AddComponent<TextMeshProUGUI>();
        progressText.font = font;
        progressText.fontSize = 26f;
        progressText.color = MutedColor;
        progressText.alignment = TextAlignmentOptions.Center;
        progressText.richText = true;
        progressText.raycastTarget = false;
        progressText.enableWordWrapping = false;
    }

    private void ShowOverlay()
    {
        if (overlay != null) overlay.SetActive(true);
    }

    private void SetContent(
        string title,
        string subtitle,
        string body,
        string reward,
        string primaryText,
        System.Action primaryAction,
        string secondaryText,
        System.Action secondaryAction)
    {
        titleText.text = title;
        subtitleText.text = subtitle;
        bodyText.text = body;
        rewardText.text = reward;
        rewardText.gameObject.SetActive(!string.IsNullOrEmpty(reward));

        primaryButton.gameObject.SetActive(true);
        primaryLabel.text = primaryText;
        primaryButton.onClick.RemoveAllListeners();
        if (primaryAction != null) primaryButton.onClick.AddListener(() => primaryAction());

        bool hasSecondary = secondaryAction != null && !string.IsNullOrEmpty(secondaryText);
        secondaryButton.gameObject.SetActive(hasSecondary);
        if (hasSecondary)
        {
            secondaryLabel.text = secondaryText;
            secondaryButton.onClick.RemoveAllListeners();
            secondaryButton.onClick.AddListener(() => secondaryAction());
        }

        // Centre the primary button when it stands alone.
        RectTransform primaryRect = primaryButton.GetComponent<RectTransform>();
        primaryRect.anchoredPosition = hasSecondary ? new Vector2(-220f, -258f) : new Vector2(0f, -258f);
    }

    private IEnumerator BannerRoutine(int waveIndex, int totalWaves, bool huge)
    {
        bannerRoot.SetActive(true);
        bannerGroup.alpha = 0f;

        bool lastWave = waveIndex >= totalWaves - 1;
        bannerText.text = huge || lastWave ? "FINAL WAVE" : "WAVE " + (waveIndex + 1);
        bannerText.color = huge || lastWave ? DangerColor : TextColor;
        bannerSubText.text = huge
            ? "They are throwing everything at you"
            : lastWave
                ? "Hold until every alien is down"
                : (waveIndex + 1) + " of " + totalWaves;

        float t = 0f;
        while (t < 0.25f)
        {
            t += Time.unscaledDeltaTime;
            bannerGroup.alpha = Mathf.Clamp01(t / 0.25f);
            yield return null;
        }

        bannerGroup.alpha = 1f;
        yield return new WaitForSecondsRealtime(1.8f);

        t = 0f;
        while (t < 0.45f)
        {
            t += Time.unscaledDeltaTime;
            bannerGroup.alpha = Mathf.Clamp01(1f - t / 0.45f);
            yield return null;
        }

        bannerGroup.alpha = 0f;
        bannerRoot.SetActive(false);
        bannerRoutine = null;
    }

    // ------------------------------------------------------------- helpers

    private static string BuildRewardText(List<SeedSO> rewards)
    {
        if (rewards == null || rewards.Count == 0) return string.Empty;

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        builder.Append("<color=#59F2BF>NEW BOT UNLOCKED</color>");

        foreach (SeedSO seed in rewards)
        {
            if (seed == null) continue;
            builder.Append("\n<size=32><b>").Append(seed.seedPlantName).Append("</b></size>");
            builder.Append("  <size=24><color=#8FB3C7>").Append(RoleFor(seed.seedID)).Append("</color></size>");
        }

        return builder.ToString();
    }

    private static string RoleFor(string seedID)
    {
        if (string.IsNullOrEmpty(seedID)) return string.Empty;
        string id = seedID.Trim();

        switch (id)
        {
            case "PulseBot": return "Ranged attacker";
            case "SpikeMine": return "Cheap delayed mine";
            case "CrushPod": return "Squashes a lane";
            case "SolarCore": return "Generates photons";
            case "ShieldCore": return "High-health blocker";
            default: return string.Empty;
        }
    }

    private static string FormatTime(float seconds)
    {
        int total = Mathf.Max(0, Mathf.RoundToInt(seconds));
        return (total / 60).ToString("0") + ":" + (total % 60).ToString("00");
    }

    private TextMeshProUGUI NewText(string name, Transform parent, float size, Color color, TextAlignmentOptions alignment)
    {
        RectTransform rect = NewRect(name, parent);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Overflow;
        return text;
    }

    private Button NewButton(string name, Transform parent, Color color, Vector2 size, Vector2 position, out TextMeshProUGUI label)
    {
        RectTransform rect = NewRect(name, parent);
        Center(rect, size, position);

        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = true;

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
        colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
        colors.colorMultiplier = 1f;
        button.colors = colors;

        label = NewText(name + "Label", rect, 30f, TextColor, TextAlignmentOptions.Center);
        Stretch(label.rectTransform);

        return button;
    }

    private static RectTransform NewRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.localScale = Vector3.one;
        return rect;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void Center(RectTransform rect, Vector2 size, Vector2 position)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    private static TMP_FontAsset ResolveFont()
    {
        TMP_FontAsset font = TMP_Settings.defaultFontAsset;
        if (font != null) return font;

        font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (font != null) return font;

        TMP_FontAsset[] fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        return fonts.Length > 0 ? fonts[0] : null;
    }
}
