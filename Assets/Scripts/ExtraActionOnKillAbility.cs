using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PieceStats))]
[RequireComponent(typeof(PieceAbility))]
public class ExtraActionOnKillAbility : MonoBehaviour
{
    private PieceStats pieceStats;
    private PieceAbility pieceAbility;

    // 一局游戏只能触发一次；重开比赛时清除
    private bool usedThisMatch;

    public bool UsedThisMatch => usedThisMatch;

    // Fired when the ability is used up or restored (PieceInfoLabel greys it out).
    public event Action UsedChanged;

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
            pieceStats.StatsReset += HandleMatchReset;
        }
    }

    private void OnDisable()
    {
        if (pieceStats != null)
        {
            pieceStats.KilledEnemy -= HandleKilledEnemy;
            pieceStats.StatsReset -= HandleMatchReset;
        }
    }

    // StatsReset fires when the match restarts (PieceStats.ResetToStart).
    private void HandleMatchReset()
    {
        usedThisMatch = false;
        UsedChanged?.Invoke();
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

        // 这一局已经触发过，不再重复触发
        if (usedThisMatch)
        {
            return;
        }

        usedThisMatch = true;
        UsedChanged?.Invoke();

        TurnManager.Instance.GrantExtraAction(
            pieceStats.Team
        );

        Debug.Log(
            $"{name} earned its one extra action " +
            $"of the match on turn {TurnManager.Instance.TurnSequence}."
        );
    }
}