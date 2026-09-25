using UnityEngine;

/// <summary>
/// Display names, descriptions and colors for piece types and abilities.
/// Shared by the ability selection screen and the on-board piece labels,
/// so both always describe a piece the same way.
/// Text is English because the default TextMeshPro font has no Chinese glyphs.
/// </summary>
public static class PieceInfo
{
    public static string TypeName(PieceTypeDefinition definition)
    {
        if (definition == null)
        {
            return "PIECE";
        }

        switch (definition.Type)
        {
            case PieceType.Heavy: return "HEAVY";
            case PieceType.Striker: return "STRIKER";
            case PieceType.Speed: return "SPEED";
            default: return definition.Type.ToString().ToUpper();
        }
    }

    public static string TypeBlurb(PieceTypeDefinition definition)
    {
        if (definition == null)
        {
            return "";
        }

        switch (definition.Type)
        {
            case PieceType.Heavy: return "Tough, hard to push, slow shot";
            case PieceType.Striker: return "Hits the hardest";
            case PieceType.Speed: return "Light and fast, but fragile";
            default: return "";
        }
    }

    public static string TypeStats(PieceStats stats)
    {
        string text = $"HP {stats.BaseHealth}   ATK {stats.BaseAttackPower}";

        PieceTypeDefinition definition = stats.TypeDefinition;

        if (definition != null)
        {
            text += $"   Shot x{definition.LaunchForceMultiplier:0.#}";
        }

        return text;
    }

    public static string AbilityName(PieceAbilityType type)
    {
        switch (type)
        {
            case PieceAbilityType.ExtraActionOnKill: return "EXTRA TURN";
            case PieceAbilityType.AllyAttackBoost: return "RALLY";
            case PieceAbilityType.AllyHeal: return "MEDIC";
            case PieceAbilityType.DefensiveCounter: return "COUNTER";
            default: return "NO ABILITY";
        }
    }

    // ability supplies the tuned values (heal amount, etc.) shown in the text.
    public static string AbilityDescription(PieceAbilityType type, PieceAbility ability)
    {
        switch (type)
        {
            case PieceAbilityType.ExtraActionOnKill:
                return "First kill: shoot again (once per match)";

            case PieceAbilityType.AllyAttackBoost:
                return $"Bump an ally: ATK +{ability.AttackBoostAmount} (max +{ability.MaxBoostPerAlly} per ally)";

            case PieceAbilityType.AllyHeal:
                return $"Bump an ally: it heals {ability.HealAmount}, you heal {ability.SelfHealAmount}";

            case PieceAbilityType.DefensiveCounter:
                return $"Hit in enemy turn: deal {ability.CounterDamage} back";

            default:
                return "No special ability";
        }
    }

    public static Color AbilityColor(PieceAbilityType type)
    {
        switch (type)
        {
            case PieceAbilityType.ExtraActionOnKill: return new Color(1f, 0.85f, 0.2f);  // gold
            case PieceAbilityType.AllyAttackBoost: return new Color(1f, 0.55f, 0.1f);    // orange
            case PieceAbilityType.AllyHeal: return new Color(0.3f, 0.9f, 0.4f);          // green
            case PieceAbilityType.DefensiveCounter: return new Color(0.75f, 0.45f, 1f);  // purple
            default: return new Color(0.7f, 0.7f, 0.7f);
        }
    }
}
