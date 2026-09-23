using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PieceStats))]
[RequireComponent(typeof(PieceAbility))]
public class ExtraActionOnKillAbility : MonoBehaviour
{
    private PieceStats pieceStats;
    private PieceAbility pieceAbility;

    // Turn number this ability last fired on.
    private int lastTriggeredTurnSequence = -1;

    private void Awake()
    {
        pieceStats = GetComponent<PieceStats>();
        pieceAbility = GetComponent<PieceAbility>();
    }

    private void OnEnable()
    {
        if (pieceStats != null)
        {
            pieceStats.KilledEnemy += HandleKilledEnemy;
        }
    }

    private void OnDisable()
    {
        if (pieceStats != null)
        {
            pieceStats.KilledEnemy -= HandleKilledEnemy;
        }
    }

    private void HandleKilledEnemy(
        PieceStats defeatedPiece
    )
    {
        if (pieceStats == null || pieceAbility == null)
        {
            return;
        }

        // Only pieces carrying the extra action ability continue.
        if (!pieceAbility.HasAbility(
                PieceAbilityType.ExtraActionOnKill))
        {
            return;
        }

        if (pieceStats.IsDead)
        {
            return;
        }

        if (defeatedPiece == null)
        {
            return;
        }

        // The kill must be on an enemy piece.
        if (defeatedPiece.Team == pieceStats.Team)
        {
            return;
        }

        if (TurnManager.Instance == null)
        {
            return;
        }

        // It must happen during this piece's own team turn.
        if (!TurnManager.Instance.IsTeamTurn(
                pieceStats.Team))
        {
            return;
        }

        int currentTurnSequence =
            TurnManager.Instance.TurnSequence;

        // Already fired this turn: do not grant a second action.
        if (lastTriggeredTurnSequence ==
            currentTurnSequence)
        {
            return;
        }

        lastTriggeredTurnSequence =
            currentTurnSequence;

        TurnManager.Instance.GrantExtraAction(
            pieceStats.Team
        );

        Debug.Log(
            $"{name} earned an extra action " +
            $"on turn {currentTurnSequence}."
        );
    }
}