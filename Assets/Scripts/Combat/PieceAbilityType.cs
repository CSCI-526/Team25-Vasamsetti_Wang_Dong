public enum PieceAbilityType
{
    None,

    // Grants one extra action after the first kill of a turn.
    ExtraActionOnKill,

    // On bumping into a teammate, raises that teammate's attack power.
    AllyAttackBoost,

    // On bumping into a teammate, heals that teammate.
    AllyHeal,

    // When damaged while defending, deals flat damage back to the attacker.
    DefensiveCounter
}