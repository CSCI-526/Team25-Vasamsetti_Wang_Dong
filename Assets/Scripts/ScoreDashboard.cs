using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Small scoreboard pinned to the top of the screen.
/// Shows each team's kill score, and underlines the team whose turn it is.
/// When a team wins, the dashboard is swapped for a win screen.
/// Builds its own UI at runtime, so it only needs to sit on a Canvas.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class ScoreDashboard : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MatchScoreManager scoreManager;
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private MatchWinManager winManager;

    [Header("Layout")]
    [SerializeField] private Vector2 panelSize = new Vector2(300f, 56f);
    [SerializeField] private float topMargin = 10f;
    [SerializeField] private float labelFontSize = 18f;
    [SerializeField] private float scoreFontSize = 34f;
    [SerializeField] private float winFontSize = 64f;
    [SerializeField] private float timerPieSize = 26f;
    [SerializeField] private float lowTimeWarning = 2f;

    [Header("Colors")]
    [SerializeField] private Color panelColor = new Color(0.1f, 0.1f, 0.12f, 0.85f);
    [SerializeField] private Color blueColor = new Color(0.25f, 0.65f, 1f);
    [SerializeField] private Color redColor = new Color(1f, 0.35f, 0.35f);
    [SerializeField] private Color inactiveLabelColor = new Color(1f, 1f, 1f, 0.45f);
    [SerializeField] private Color timerWarningColor = new Color(1f, 0.8f, 0.2f);
    [SerializeField] private Color winOverlayColor = new Color(0f, 0f, 0f, 0.6f);

    private GameObject dashboardPanel;
    private GameObject winScreen;
    private TMP_Text winText;

    private TMP_Text blueLabel;
    private TMP_Text redLabel;
    private TMP_Text blueScoreText;
    private TMP_Text redScoreText;
    private Image blueTurnBar;
    private Image redTurnBar;
    private GameObject timerPie;
    private Image timerPieFill;
    private static Sprite circleSprite;

    private void Awake()
    {
        BuildPanel();
        BuildWinScreen();
    }

    private void Start()
    {
        if (scoreManager == null)
        {
            scoreManager = FindFirstObjectByType<MatchScoreManager>();
        }

        if (turnManager == null)
        {
            turnManager = FindFirstObjectByType<TurnManager>();
        }

        if (winManager == null)
        {
            winManager = FindFirstObjectByType<MatchWinManager>();
        }

        if (winManager != null)
        {
            winManager.MatchOver += ShowWinScreen;
            winManager.MatchReset += HideWinScreen;
        }

        if (scoreManager != null)
        {
            scoreManager.ScoreChanged += Refresh;
        }

        if (turnManager != null)
        {
            turnManager.TurnChanged += HandleTurnChanged;
        }

        Refresh();
        HideWinScreen();
    }

    private void OnDestroy()
    {
        if (winManager != null)
        {
            winManager.MatchOver -= ShowWinScreen;
            winManager.MatchReset -= HideWinScreen;
        }

        if (scoreManager != null)
        {
            scoreManager.ScoreChanged -= Refresh;
        }

        if (turnManager != null)
        {
            turnManager.TurnChanged -= HandleTurnChanged;
        }
    }

    private void HandleTurnChanged(PieceTeamSide team)
    {
        Refresh();
    }

    private void Refresh()
    {
        int blue = scoreManager != null ? scoreManager.BlueScore : 0;
        int red = scoreManager != null ? scoreManager.RedScore : 0;

        blueScoreText.text = blue.ToString();
        redScoreText.text = red.ToString();

        // Highlight the team that is currently acting.
        bool blueTurn =
            turnManager == null ||
            turnManager.CurrentTeam == PieceTeamSide.Blue;

        blueTurnBar.enabled = blueTurn;
        redTurnBar.enabled = !blueTurn;

        blueLabel.color = blueTurn ? blueColor : inactiveLabelColor;
        redLabel.color = blueTurn ? inactiveLabelColor : redColor;
    }

    private void ShowWinScreen(PieceTeamSide winner)
    {
        bool blueWon = winner == PieceTeamSide.Blue;

        winText.text = blueWon ? "Team Blue won !!!!!" : "Team Red won !!!!!";
        winText.color = blueWon ? blueColor : redColor;

        dashboardPanel.SetActive(false);
        winScreen.SetActive(true);
    }

    private void HideWinScreen()
    {
        winScreen.SetActive(false);
        dashboardPanel.SetActive(true);
        Refresh();
    }

    // ---------- UI construction ----------

    private void BuildPanel()
    {
        RectTransform panel = CreateRect("ScoreDashboardPanel", transform);
        panel.anchorMin = new Vector2(0.5f, 1f);
        panel.anchorMax = new Vector2(0.5f, 1f);
        panel.pivot = new Vector2(0.5f, 1f);
        panel.anchoredPosition = new Vector2(0f, -topMargin);
        panel.sizeDelta = panelSize;

        panel.gameObject.AddComponent<Image>().color = panelColor;
        dashboardPanel = panel.gameObject;

        // Left half = Blue, right half = Red.
        BuildTeamHalf(panel, "BLUE", blueColor, 0f, 0.5f, true,
            out blueLabel, out blueScoreText, out blueTurnBar);

        BuildTeamHalf(panel, "RED", redColor, 0.5f, 1f, false,
            out redLabel, out redScoreText, out redTurnBar);

        // Turn countdown pie in the middle, between the two scores.
        RectTransform pie = CreateRect("TimerPie", panel);
        pie.anchorMin = new Vector2(0.5f, 0.5f);
        pie.anchorMax = new Vector2(0.5f, 0.5f);
        pie.sizeDelta = new Vector2(timerPieSize, timerPieSize);

        Image track = pie.gameObject.AddComponent<Image>();
        track.sprite = GetCircleSprite();
        track.color = new Color(1f, 1f, 1f, 0.15f);
        track.raycastTarget = false;

        RectTransform fill = CreateRect("TimerPieFill", pie);
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = Vector2.one;
        fill.sizeDelta = Vector2.zero;

        timerPieFill = fill.gameObject.AddComponent<Image>();
        timerPieFill.sprite = GetCircleSprite();
        timerPieFill.type = Image.Type.Filled;
        timerPieFill.fillMethod = Image.FillMethod.Radial360;
        timerPieFill.fillOrigin = (int)Image.Origin360.Top;
        timerPieFill.fillClockwise = false;
        timerPieFill.raycastTarget = false;

        timerPie = pie.gameObject;
    }

    private void Update()
    {
        if (timerPie == null)
        {
            return;
        }

        // Hide the pie while a shot is rolling (the clock is paused) or with no time limit.
        bool show =
            turnManager != null &&
            turnManager.TurnTimeLimit > 0f &&
            turnManager.IsTimerRunning;

        timerPie.SetActive(show);

        if (!show)
        {
            return;
        }

        float remaining = turnManager.TimeRemaining;

        // The slice shrinks as time runs out, in the acting team's color.
        timerPieFill.fillAmount = Mathf.Clamp01(remaining / turnManager.TurnTimeLimit);
        timerPieFill.color = remaining <= lowTimeWarning
            ? timerWarningColor
            : (turnManager.CurrentTeam == PieceTeamSide.Blue ? blueColor : redColor);
    }

    // Anti-aliased white circle generated once, used for the timer pie.
    private static Sprite GetCircleSprite()
    {
        if (circleSprite != null)
        {
            return circleSprite;
        }

        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;

        float radius = size / 2f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(
                    new Vector2(x + 0.5f, y + 0.5f),
                    new Vector2(radius, radius)
                );
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(radius - distance)));
            }
        }

        texture.Apply();

        circleSprite = Sprite.Create(
            texture,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f)
        );
        return circleSprite;
    }

    private void BuildWinScreen()
    {
        // Full-screen dim overlay with the winner message in the middle.
        RectTransform overlay = CreateRect("WinScreen", transform);
        overlay.anchorMin = Vector2.zero;
        overlay.anchorMax = Vector2.one;
        overlay.sizeDelta = Vector2.zero;
        overlay.gameObject.AddComponent<Image>().color = winOverlayColor;

        winText = CreateText("WinText", overlay, "", winFontSize);
        winText.fontStyle = FontStyles.Bold;
        SetHorizontalSpan(winText.rectTransform, 0f, 1f);

        winScreen = overlay.gameObject;
        winScreen.SetActive(false);
    }

    private void BuildTeamHalf(
        RectTransform panel,
        string teamName,
        Color teamColor,
        float xMin,
        float xMax,
        bool labelOnLeft,
        out TMP_Text label,
        out TMP_Text score,
        out Image turnBar
    )
    {
        RectTransform half = CreateRect(teamName + "Half", panel);
        half.anchorMin = new Vector2(xMin, 0f);
        half.anchorMax = new Vector2(xMax, 1f);
        half.sizeDelta = Vector2.zero;

        // Team name sits on the outer edge, score toward the center.
        label = CreateText(teamName + "Label", half, teamName, labelFontSize);
        label.fontStyle = FontStyles.Bold;
        label.color = teamColor;
        SetHorizontalSpan(label.rectTransform, labelOnLeft ? 0f : 0.5f, labelOnLeft ? 0.5f : 1f);

        score = CreateText(teamName + "Score", half, "0", scoreFontSize);
        score.fontStyle = FontStyles.Bold;
        score.color = Color.white;
        SetHorizontalSpan(score.rectTransform, labelOnLeft ? 0.5f : 0f, labelOnLeft ? 1f : 0.5f);

        // Colored bar along the bottom marks the active team.
        RectTransform bar = CreateRect(teamName + "TurnBar", half);
        bar.anchorMin = new Vector2(0f, 0f);
        bar.anchorMax = new Vector2(1f, 0f);
        bar.pivot = new Vector2(0.5f, 0f);
        bar.sizeDelta = new Vector2(0f, 4f);
        turnBar = bar.gameObject.AddComponent<Image>();
        turnBar.color = teamColor;
    }

    private static RectTransform CreateRect(string objectName, Transform parent)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform));
        go.layer = parent.gameObject.layer;

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private static TMP_Text CreateText(
        string objectName,
        Transform parent,
        string text,
        float fontSize
    )
    {
        RectTransform rect = CreateRect(objectName, parent);

        TextMeshProUGUI tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static void SetHorizontalSpan(RectTransform rect, float xMin, float xMax)
    {
        rect.anchorMin = new Vector2(xMin, 0f);
        rect.anchorMax = new Vector2(xMax, 1f);
        rect.sizeDelta = Vector2.zero;
    }
}
