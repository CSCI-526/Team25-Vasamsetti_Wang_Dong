using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PieceStats))]
[RequireComponent(typeof(PieceAbility))]
public class DefensiveCounterAbility : MonoBehaviour
{
    private PieceStats pieceStats;
    private PieceAbility pieceAbility;

    private void Awake()
    {
        pieceStats = GetComponent<PieceStats>();
        pieceAbility = GetComponent<PieceAbility>();
    }

    private void OnEnable()
    {
        if (pieceStats != null)
        {
            pieceStats.Damaged += HandleDamaged;
        }
    }

    private void OnDisable()
    {
        if (pieceStats != null)
        {
            pieceStats.Damaged -= HandleDamaged;
        }
    }

    private void HandleDamaged(
        PieceStats attacker,
        int receivedDamage
    )
    {
        if (pieceAbility == null || pieceStats == null)
        {
            return;
        }

        // Only pieces carrying the defensive counter ability continue.
        if (!pieceAbility.HasAbility(
                PieceAbilityType.DefensiveCounter))
        {
            return;
        }

        if (attacker == null)
        {
            return;
        }

        // Friendly fire never triggers a counter.
        if (attacker.Team == pieceStats.Team)
        {
            return;
        }

        // The turn system has to be present.
        if (TurnManager.Instance == null)
        {
            return;
        }

        // Counters only happen while defending, not on your own turn.
        if (TurnManager.Instance.IsTeamTurn(
                pieceStats.Team))
        {
            return;
        }

        // Make sure the attacker belongs to the team that is acting.
        if (!TurnManager.Instance.IsTeamTurn(
                attacker.Team))
        {
            return;
        }

        int counterDamage =
            pieceAbility.CounterDamage;

        if (counterDamage <= 0)
        {
            return;
        }

        Debug.Log(
            $"{name} countered {attacker.name} " +
            $"for {counterDamage} damage."
        );

        // The defending piece is credited as the source of this damage.
        attacker.TakeDamage(
            counterDamage,
            pieceStats
        );
    }
}