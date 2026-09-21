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

        // 只有防守反击棋子才继续执行
        if (!pieceAbility.HasAbility(
                PieceAbilityType.DefensiveCounter))
        {
            return;
        }

        if (attacker == null)
        {
            return;
        }

        // 友军伤害不触发反击
        if (attacker.Team == pieceStats.Team)
        {
            return;
        }

        // 必须存在回合系统
        if (TurnManager.Instance == null)
        {
            return;
        }

        // 自己队伍的回合不能触发防守反击
        if (TurnManager.Instance.IsTeamTurn(
                pieceStats.Team))
        {
            return;
        }

        // 确认攻击者属于当前行动队伍
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

        // 防守棋子成为这次反伤的伤害来源
        attacker.TakeDamage(
            counterDamage,
            pieceStats
        );
    }
}