using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pre-match screen where both teams give an ability to each of their pieces.
///  - Opens when the scene starts and again after every match restart.
///  - Each row is one piece: its type and stats on the left, and "&lt; ability &gt;"
///    arrows on the right to cycle through the abilities.
///  - With uniqueAbilitiesPerTeam, a team cannot put the same ability on two pieces.
///  - While open, no piece can be launched and the turn timer is paused.
/// Builds its own UI at runtime, so it only needs to sit on a Canvas.
/// </summary>
[DefaultExecutionOrder(100)] // Start after TurnManager so our input lock wins.
[RequireComponent(typeof(RectTransform))]
public class AbilitySelectionScreen : MonoBehaviour
{
    [Header("Rules")]
    [Tooltip("A team cannot give the same ability to two of its pieces (NO ABILITY is always allowed).")]
    [SerializeField] private bool uniqueAbilitiesPerTeam = true;

    [Header("Layout")]
    [SerializeField] private Vector2 panelSize = new Vector2(780f, 500f);
    [SerializeField] private float rowHeight = 96f;
    [SerializeField] private float titleFontSize = 30f;
    [SerializeField] private float headerFontSize = 22f;
    [SerializeField] private float nameFontSize = 20f;
    [SerializeField] private float smallFontSize = 13f;

    [Header("Colors")]
    [SerializeField] private Color overlayColor = new Color(0f, 0f, 0f, 0.7f);
    [SerializeField] private Color panelColor = new Color(0.12f, 0.12f, 0.14f, 0.97f);
    [SerializeField] private Color rowColor = new Color(1f, 1f, 1f, 0.06f);
    [SerializeField] private Color arrowColor = new Color(1f, 1f, 1f, 0.8f);
    [SerializeField] private Color arrowHoverColor = new Color(1f, 0.85f, 0.3f);
    [SerializeField] private Color startButtonColor = new Color(0.2f, 0.7f, 0.35f);
    [SerializeField] private Color blueColor = new Color(0.25f, 0.65f, 1f);
    [SerializeField] private Color redColor = new Color(1f, 0.35f, 0.35f);

    // Order the arrows cycle through.
    private static readonly PieceAbilityType[] AbilityOrder =
    {
        PieceAbilityType.None,
        PieceAbilityType.ExtraActionOnKill,
        PieceAbilityType.AllyAttackBoost,
        PieceAbilityType.AllyHeal,
        PieceAbilityType.DefensiveCounter
    };

    private class Row
    {
        public PieceStats Stats;
        public PieceAbility Ability;
        public TMP_Text AbilityName;
        public TMP_Text AbilityDescription;
    }

    private readonly List<Row> rows = new List<Row>();

    private TurnManager turnManager;
    private MatchWinManager winManager;
    private GameObject screen;

    private void Start()
    {
        turnManager = FindFirstObjectByType<TurnManager>();
        winManager = FindFirstObjectByType<MatchWinManager>();

        if (winManager != null)
        {
            winManager.MatchReset += Open;
        }

        CollectPieces();
        GiveDefaultAbilities();
        BuildScreen();

        Open();
    }

    private void OnDestroy()
    {
        if (winManager != null)
        {
            winManager.MatchReset -= Open;
        }
    }

    // ---------- Open / close ----------

    private void Open()
    {
        foreach (Row row in rows)
        {
            RefreshRow(row);
        }

        screen.SetActive(true);
        screen.transform.SetAsLastSibling();

        // Pause the match: timer stops and nobody can shoot.
        if (turnManager != null)
        {
            turnManager.SetMatchOver(true);
        }

        SetAllControl(false);
    }

    private void StartMatch()
    {
        screen.SetActive(false);

        // Hands the first turn to Blue with a full clock and re-enables input.
        if (turnManager != null)
        {
            turnManager.ResetToStart();
        }
        else
        {
            SetAllControl(true);
        }

        Debug.Log("Abilities chosen. Match started.");
    }

    private static void SetAllControl(bool enabled)
    {
        PieceLauncher[] launchers =
            FindObjectsByType<PieceLauncher>(FindObjectsSortMode.None);

        foreach (PieceLauncher launcher in launchers)
        {
            launcher.SetControlEnabled(enabled);
        }
    }

    // ---------- Pieces and rules ----------

    private void CollectPieces()
    {
        PieceStats[] pieces =
            FindObjectsByType<PieceStats>(FindObjectsSortMode.None);

        foreach (PieceStats piece in pieces)
        {
            PieceAbility ability = piece.GetComponent<PieceAbility>();

            if (ability != null)
            {
                rows.Add(new Row { Stats = piece, Ability = ability });
            }
        }

        // Blue first, then Heavy / Striker / Speed, then by name.
        rows.Sort((a, b) =>
        {
            int byTeam = a.Stats.Team.CompareTo(b.Stats.Team);
            if (byTeam != 0) return byTeam;

            int byType = TypeOrder(a.Stats).CompareTo(TypeOrder(b.Stats));
            if (byType != 0) return byType;

            return string.CompareOrdinal(a.Stats.name, b.Stats.name);
        });
    }

    private static int TypeOrder(PieceStats stats)
    {
        return stats.TypeDefinition != null ? (int)stats.TypeDefinition.Type : 99;
    }

    // Pieces without an ability start with one that suits their type,
    // so the screen opens with a sensible setup players can just accept.
    private void GiveDefaultAbilities()
    {
        foreach (Row row in rows)
        {
            if (row.Ability.AbilityType != PieceAbilityType.None ||
                row.Stats.TypeDefinition == null)
            {
                continue;
            }

            PieceAbilityType suggestion = SuggestedAbility(row.Stats.TypeDefinition.Type);

            if (!IsTakenByTeammate(row, suggestion))
            {
                row.Ability.SetAbility(suggestion);
            }
        }
    }

    private static PieceAbilityType SuggestedAbility(PieceType type)
    {
        switch (type)
        {
            case PieceType.Heavy: return PieceAbilityType.DefensiveCounter;
            case PieceType.Striker: return PieceAbilityType.ExtraActionOnKill;
            case PieceType.Speed: return PieceAbilityType.AllyHeal;
            default: return PieceAbilityType.None;
        }
    }

    private bool IsTakenByTeammate(Row row, PieceAbilityType type)
    {
        if (!uniqueAbilitiesPerTeam || type == PieceAbilityType.None)
        {
            return false;
        }

        foreach (Row other in rows)
        {
            if (other != row &&
                other.Stats.Team == row.Stats.Team &&
                other.Ability.AbilityType == type)
            {
                return true;
            }
        }

        return false;
    }

    // direction: +1 = next ability, -1 = previous. Skips abilities a teammate has.
    private void Cycle(Row row, int direction)
    {
        int index = System.Array.IndexOf(AbilityOrder, row.Ability.AbilityType);

        for (int step = 0; step < AbilityOrder.Length; step++)
        {
            index = (index + direction + AbilityOrder.Length) % AbilityOrder.Length;

            if (!IsTakenByTeammate(row, AbilityOrder[index]))
            {
                break;
            }
        }

        // PieceInfoLabel on the board updates itself through AbilityChanged.
        row.Ability.SetAbility(AbilityOrder[index]);
        RefreshRow(row);
    }

    private void RefreshRow(Row row)
    {
        PieceAbilityType type = row.Ability.AbilityType;

        row.AbilityName.text = PieceInfo.AbilityName(type);
        row.AbilityName.color = PieceInfo.AbilityColor(type);
        row.AbilityDescription.text = PieceInfo.AbilityDescription(type, row.Ability);
    }

    // ---------- UI construction ----------

    private void BuildScreen()
    {
        // Full-screen dim overlay.
        RectTransform overlay = CreateRect("AbilitySelectionScreen", transform);
        overlay.anchorMin = Vector2.zero;
        overlay.anchorMax = Vector2.one;
        overlay.sizeDelta = Vector2.zero;
        overlay.gameObject.AddComponent<Image>().color = overlayColor;
        screen = overlay.gameObject;

        RectTransform panel = CreateRect("Panel", overlay);
        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = panelSize;
        panel.gameObject.AddComponent<Image>().color = panelColor;

        TMP_Text title = CreateText("Title", panel, "CHOOSE YOUR ABILITIES", titleFontSize);
        title.fontStyle = FontStyles.Bold;
        Place(title.rectTransform, 0f, 14f, panelSize.x, 40f);

        string hint = uniqueAbilitiesPerTeam
            ? "Use the arrows to pick an ability for each piece. Each ability once per team."
            : "Use the arrows to pick an ability for each piece.";

        TMP_Text hintText = CreateText("Hint", panel, hint, smallFontSize);
        hintText.color = new Color(1f, 1f, 1f, 0.6f);
        Place(hintText.rectTransform, 0f, 54f, panelSize.x, 20f);

        float margin = 20f;
        float columnWidth = (panelSize.x - margin * 3f) / 2f;

        BuildColumn(panel, PieceTeamSide.Blue, "BLUE TEAM", blueColor, margin, columnWidth);
        BuildColumn(panel, PieceTeamSide.Red, "RED TEAM", redColor, margin * 2f + columnWidth, columnWidth);

        Button startButton = CreateButton(
            "StartButton", panel, "START", headerFontSize, startButtonColor
        );
        Place(
            (RectTransform)startButton.transform,
            (panelSize.x - 200f) / 2f, panelSize.y - 58f, 200f, 44f
        );
        startButton.onClick.AddListener(StartMatch);

        screen.SetActive(false);
    }

    private void BuildColumn(
        RectTransform panel,
        PieceTeamSide team,
        string header,
        Color teamColor,
        float x,
        float width
    )
    {
        TMP_Text headerText = CreateText(team + "Header", panel, header, headerFontSize);
        headerText.fontStyle = FontStyles.Bold;
        headerText.color = teamColor;
        Place(headerText.rectTransform, x, 82f, width, 30f);

        float y = 118f;

        foreach (Row row in rows)
        {
            if (row.Stats.Team != team)
            {
                continue;
            }

            BuildRow(panel, row, teamColor, x, y, width);
            y += rowHeight + 6f;
        }
    }

    private void BuildRow(
        RectTransform panel,
        Row row,
        Color teamColor,
        float x,
        float y,
        float width
    )
    {
        RectTransform rowRect = CreateRect(row.Stats.name + "Row", panel);
        Place(rowRect, x, y, width, rowHeight);
        rowRect.gameObject.AddComponent<Image>().color = rowColor;

        // Team-colored stripe on the left edge.
        RectTransform stripe = CreateRect("Stripe", rowRect);
        Place(stripe, 0f, 0f, 4f, rowHeight);
        stripe.gameObject.AddComponent<Image>().color = teamColor;

        // Left: what the piece is.
        float infoWidth = 140f;
        PieceTypeDefinition definition = row.Stats.TypeDefinition;

        TMP_Text typeName = CreateText("Type", rowRect, PieceInfo.TypeName(definition), nameFontSize);
        typeName.fontStyle = FontStyles.Bold;
        typeName.alignment = TextAlignmentOptions.Left;
        Place(typeName.rectTransform, 14f, 8f, infoWidth, 26f);

        TMP_Text statsText = CreateText("Stats", rowRect, PieceInfo.TypeStats(row.Stats), smallFontSize);
        statsText.alignment = TextAlignmentOptions.Left;
        Place(statsText.rectTransform, 14f, 36f, infoWidth + 20f, 20f);

        TMP_Text blurb = CreateText("Blurb", rowRect, PieceInfo.TypeBlurb(definition), smallFontSize);
        blurb.alignment = TextAlignmentOptions.TopLeft;
        blurb.textWrappingMode = TextWrappingModes.Normal;
        blurb.color = new Color(1f, 1f, 1f, 0.6f);
        Place(blurb.rectTransform, 14f, 56f, infoWidth, 36f);

        // Right: "<  ABILITY  >" and what the ability does.
        float pickerX = infoWidth + 30f;
        float pickerWidth = width - pickerX - 10f;
        float arrowSize = 30f;

        Button previous = CreateArrowButton("Previous", rowRect, "<");
        Place((RectTransform)previous.transform, pickerX, 10f, arrowSize, arrowSize);
        previous.onClick.AddListener(() => Cycle(row, -1));

        Button next = CreateArrowButton("Next", rowRect, ">");
        Place((RectTransform)next.transform, pickerX + pickerWidth - arrowSize, 10f, arrowSize, arrowSize);
        next.onClick.AddListener(() => Cycle(row, 1));

        row.AbilityName = CreateText("AbilityName", rowRect, "", nameFontSize - 4f);
        row.AbilityName.fontStyle = FontStyles.Bold;
        Place(row.AbilityName.rectTransform, pickerX + arrowSize, 10f, pickerWidth - arrowSize * 2f, arrowSize);

        row.AbilityDescription = CreateText("AbilityDescription", rowRect, "", smallFontSize);
        row.AbilityDescription.alignment = TextAlignmentOptions.Top;
        row.AbilityDescription.textWrappingMode = TextWrappingModes.Normal;
        Place(row.AbilityDescription.rectTransform, pickerX, 46f, pickerWidth, 44f);
    }

    // Positions a rect by its top-left corner, measured from the parent's top-left.
    private static void Place(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(width, height);
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

    private static Button CreateButton(
        string objectName,
        Transform parent,
        string label,
        float fontSize,
        Color color
    )
    {
        RectTransform rect = CreateRect(objectName, parent);

        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;

        TMP_Text text = CreateText("Label", rect, label, fontSize);
        text.fontStyle = FontStyles.Bold;
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.sizeDelta = Vector2.zero;

        return button;
    }

    // Just the "<" / ">" character, no box. The invisible image still catches
    // clicks; hover and press tint the arrow itself.
    private Button CreateArrowButton(string objectName, Transform parent, string arrow)
    {
        Button button = CreateButton(objectName, parent, arrow, nameFontSize, Color.clear);

        TMP_Text text = button.GetComponentInChildren<TMP_Text>();
        text.color = arrowColor;
        button.targetGraphic = text;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = arrowHoverColor;
        colors.selectedColor = Color.white;
        colors.pressedColor = new Color(0.6f, 0.6f, 0.6f);
        button.colors = colors;

        return button;
    }
}
