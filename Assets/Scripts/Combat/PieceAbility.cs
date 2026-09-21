using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PieceStats))]
public class PieceAbility : MonoBehaviour
{
    [Header("Ability Type")]
    [SerializeField]
    private PieceAbilityType abilityType =
        PieceAbilityType.None;

    [Header("Ability Values")]
    [Tooltip("攻击强化技能每次增加的攻击力")]
    [SerializeField]
    [Min(0)]
    private int attackBoostAmount = 1;

    [Tooltip("治疗技能每次恢复的生命值")]
    [SerializeField]
    [Min(0)]
    private int healAmount = 2;

    [Tooltip("防守反击技能造成的固定伤害")]
    [SerializeField]
    [Min(0)]
    private int counterDamage = 1;

    private PieceStats pieceStats;

    public PieceAbilityType AbilityType => abilityType;
    public int AttackBoostAmount => attackBoostAmount;
    public int HealAmount => healAmount;
    public int CounterDamage => counterDamage;

    private void Awake()
    {
        pieceStats = GetComponent<PieceStats>();
    }

    public bool HasAbility(PieceAbilityType type)
    {
        return abilityType == type;
    }

    /// <summary>
    /// 当这枚棋子碰到友军时调用。
    /// allyStats 是被碰到的友军。
    /// </summary>
    public void TryActivateOnAlly(PieceStats allyStats)
    {
        if (pieceStats == null || allyStats == null)
        {
            return;
        }

        if (pieceStats.IsDead || allyStats.IsDead)
        {
            return;
        }

        // 攻击强化和治疗只能在自己队伍的回合触发
        if (TurnManager.Instance != null &&
            !TurnManager.Instance.IsTeamTurn(pieceStats.Team))
        {
            return;
        }

        if (pieceStats == allyStats)
        {
            return;
        }

        if (pieceStats == allyStats)
        {
            return;
        }

        if (pieceStats.Team != allyStats.Team)
        {
            return;
        }

        switch (abilityType)
        {
            case PieceAbilityType.AllyAttackBoost:
                ApplyAttackBoost(allyStats);
                break;

            case PieceAbilityType.AllyHeal:
                ApplyHealing(allyStats);
                break;
        }
    }

    private void ApplyAttackBoost(PieceStats allyStats)
    {
        if (attackBoostAmount <= 0)
        {
            return;
        }

        allyStats.AddAttackPower(attackBoostAmount);

        Debug.Log(
            $"{name} boosted {allyStats.name}. " +
            $"ATK +{attackBoostAmount}"
        );
    }

    private void ApplyHealing(PieceStats allyStats)
    {
        if (healAmount <= 0)
        {
            return;
        }

        allyStats.Heal(healAmount);

        Debug.Log(
            $"{name} healed {allyStats.name}. " +
            $"HP +{healAmount}"
        );
    }

    private void OnValidate()
    {
        attackBoostAmount =
            Mathf.Max(0, attackBoostAmount);

        healAmount =
            Mathf.Max(0, healAmount);

        counterDamage =
            Mathf.Max(0, counterDamage);
    }
}