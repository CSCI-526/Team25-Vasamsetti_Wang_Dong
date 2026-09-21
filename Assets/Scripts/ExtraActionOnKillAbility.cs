using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PieceStats))]
[RequireComponent(typeof(PieceAbility))]
public class ExtraActionOnKillAbility : MonoBehaviour
{
    private PieceStats pieceStats;
    private PieceAbility pieceAbility;

    // 记录上一次触发技能时的回合编号
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

        // 只有额外行动棋子才执行
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

        // 必须击杀敌方棋子
        if (defeatedPiece.Team == pieceStats.Team)
        {
            return;
        }

        if (TurnManager.Instance == null)
        {
            return;
        }

        // 必须发生在技能棋子所属方的回合
        if (!TurnManager.Instance.IsTeamTurn(
                pieceStats.Team))
        {
            return;
        }

        int currentTurnSequence =
            TurnManager.Instance.TurnSequence;

        // 本回合已经触发过，不再重复触发
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