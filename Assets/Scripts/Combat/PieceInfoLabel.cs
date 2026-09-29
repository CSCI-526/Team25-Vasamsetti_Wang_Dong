using TMPro;
using UnityEngine;

/// <summary>
/// Shows what a piece is on the board: a small label under the piece with
/// its type (HEAVY / STRIKER / SPEED) and its ability name in the ability's color.
/// A used-up EXTRA TURN is shown greyed out and struck through.
/// Added automatically by PieceStats; updates live when the ability changes.
/// </summary>
[RequireComponent(typeof(PieceStats))]
public class PieceInfoLabel : MonoBehaviour
{
    private const float LabelGap = 0.12f;     // world units between piece and label
    private const float FontSize = 2.2f;

    private PieceStats stats;
    private PieceAbility ability;
    private ExtraActionOnKillAbility extraAction;

    private TextMeshPro label;

    private void Start()
    {
        stats = GetComponent<PieceStats>();
        ability = GetComponent<PieceAbility>();
        extraAction = GetComponent<ExtraActionOnKillAbility>();

        SpriteRenderer body = GetComponent<SpriteRenderer>();

        BuildLabel(body);

        stats.Died += HandleDied;
        stats.StatsReset += HandleReset;

        if (ability != null)
        {
            ability.AbilityChanged += Refresh;
        }

        if (extraAction != null)
        {
            extraAction.UsedChanged += Refresh;
        }

        SetVisible(!stats.IsDead);
        Refresh();
    }

    private void OnDestroy()
    {
        if (stats != null)
        {
            stats.Died -= HandleDied;
            stats.StatsReset -= HandleReset;
        }

        if (ability != null)
        {
            ability.AbilityChanged -= Refresh;
        }

        if (extraAction != null)
        {
            extraAction.UsedChanged -= Refresh;
        }
    }

    private void BuildLabel(SpriteRenderer body)
    {
        GameObject labelObject = new GameObject("InfoLabel");
        labelObject.transform.SetParent(transform, false);

        // Undo the piece's scale so the font size is in world units.
        Vector3 parentScale = transform.lossyScale;
        labelObject.transform.localScale = new Vector3(
            1f / parentScale.x,
            1f / parentScale.y,
            1f
        );

        float radius = body != null ? body.bounds.extents.y : 0.5f;
        labelObject.transform.localPosition =
            new Vector3(0f, -(radius + LabelGap) / parentScale.y, -0.2f);

        label = labelObject.AddComponent<TextMeshPro>();
        label.fontSize = FontSize;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Top;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.color = Color.white;
        label.outlineWidth = 0.25f;
        label.outlineColor = new Color32(0, 0, 0, 255);
        label.sortingOrder = 5;

        label.rectTransform.pivot = new Vector2(0.5f, 1f);
        label.rectTransform.sizeDelta = new Vector2(3f, 1f);
    }

    private void Refresh()
    {
        if (label == null)
        {
            return;
        }

        PieceAbilityType type = ability != null
            ? ability.AbilityType
            : PieceAbilityType.None;

        string text = PieceInfo.TypeName(stats.TypeDefinition);

        if (type != PieceAbilityType.None)
        {
            string abilityName = PieceInfo.AbilityName(type);

            bool usedUp =
                type == PieceAbilityType.ExtraActionOnKill &&
                extraAction != null &&
                extraAction.UsedThisMatch;

            if (usedUp)
            {
                text += $"\n<color=#808080><s>{abilityName}</s></color>";
            }
            else
            {
                string hex = ColorUtility.ToHtmlStringRGB(PieceInfo.AbilityColor(type));
                text += $"\n<color=#{hex}>{abilityName}</color>";
            }
        }

        label.text = text;
    }

    private void HandleDied(PieceStats attacker)
    {
        SetVisible(false);
    }

    private void HandleReset()
    {
        SetVisible(true);
        Refresh();
    }

    private void SetVisible(bool visible)
    {
        if (label != null)
        {
            label.gameObject.SetActive(visible);
        }
    }
}
