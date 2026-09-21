public enum PieceAbilityType
{
    None,

    // 每回合第一次击杀后，获得一次额外行动
    ExtraActionOnKill,

    // 碰到友军时，提高被碰友军的攻击力
    AllyAttackBoost,

    // 碰到友军时，治疗被碰友军
    AllyHeal,

    // 防守时受到攻击，对攻击者造成固定伤害
    DefensiveCounter
}