using System;
using System.Collections.Generic;
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

    [Tooltip("攻击强化：每个友军每局最多能从这枚棋子获得的攻击力加成")]
    [SerializeField]
    [Min(0)]
    private int maxBoostPerAlly = 2;

    [Tooltip("治疗技能每次恢复的生命值")]
    [SerializeField]
    [Min(0)]
    private int healAmount = 2;

    [Tooltip("治疗：每次治疗友军时，治疗棋子自己也恢复的生命值")]
    [SerializeField]
    [Min(0)]
    private int selfHealAmount = 1;

    [Tooltip("防守反击技能造成的固定伤害")]
    [SerializeField]
    [Min(0)]
    private int counterDamage = 1;

    private PieceStats pieceStats;

    // 攻击强化：记录这一局已经给每个友军加了多少攻击力
    private readonly Dictionary<PieceStats, int> boostGiven =
        new Dictionary<PieceStats, int>();

    public PieceAbilityType AbilityType => abilityType;
    public int AttackBoostAmount => attackBoostAmount;
    public int MaxBoostPerAlly => maxBoostPerAlly;
    public int HealAmount => healAmount;
    public int SelfHealAmount => selfHealAmount;
    public int CounterDamage => counterDamage;

    // Fired when the ability is changed at runtime (ability selection screen).
    public event Action AbilityChanged;

    private void Awake()
    {
        pieceStats = GetComponent<PieceStats>();
    }

    private void OnEnable()
    {
        if (pieceStats != null)
        {
            pieceStats.StatsReset += HandleMatchReset;
        }
    }

    private void OnDisable()
    {
        if (pieceStats != null)
        {
            pieceStats.StatsReset -= HandleMatchReset;
        }
    }

    // StatsReset fires when the match restarts; ATK buffs are cleared too.
    private void HandleMatchReset()
    {
        boostGiven.Clear();
    }

    public bool HasAbility(PieceAbilityType type)
    {
        return abilityType == type;
    }

    // Called by AbilitySelectionScreen before the match starts.
    public void SetAbility(PieceAbilityType type)
    {
        if (abilityType == type)
        {
            return;
        }

        abilityType = type;

        AbilityChanged?.Invoke();
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
        boostGiven.TryGetValue(allyStats, out int given);

        // 每个友军从这枚棋子身上最多获得 maxBoostPerAlly 点攻击力
        int amount = Mathf.Min(
            attackBoostAmount,
            maxBoostPerAlly - given
        );

        if (amount <= 0)
        {
            return;
        }

        allyStats.AddAttackPower(amount);
        boostGiven[allyStats] = given + amount;

        Debug.Log(
            $"{name} boosted {allyStats.name}. " +
            $"ATK +{amount} ({given + amount}/{maxBoostPerAlly})"
        );
    }

    private void ApplyHealing(PieceStats allyStats)
    {
        // Heal() never goes above the starting HP.
        allyStats.Heal(healAmount);

        // 治疗棋子自己也恢复一点，让它更能在前线存活
        // No "FULL HP" popup for the medic itself; it would show on every bump.
        pieceStats.Heal(selfHealAmount, reportIfFull: false);

        Debug.Log(
            $"{name} healed {allyStats.name} (up to +{healAmount}) " +
            $"and itself (up to +{selfHealAmount})."
        );
    }

    private void OnValidate()
    {
        attackBoostAmount =
            Mathf.Max(0, attackBoostAmount);

        healAmount =
            Mathf.Max(0, healAmount);

        maxBoostPerAlly =
            Mathf.Max(0, maxBoostPerAlly);

        selfHealAmount =
            Mathf.Max(0, selfHealAmount);

        counterDamage =
            Mathf.Max(0, counterDamage);
    }
}