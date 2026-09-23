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
    [Tooltip("Attack power the attack boost ability grants per activation.")]
    [SerializeField]
    [Min(0)]
    private int attackBoostAmount = 1;

    [Tooltip("HP the heal ability restores per activation.")]
    [SerializeField]
    [Min(0)]
    private int healAmount = 2;

    [Tooltip("Flat damage the defensive counter ability deals.")]
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
    /// Called when this piece bumps into a teammate.
    /// allyStats is the teammate that was hit.
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

        // Attack boost and healing only fire on their own team's turn.
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