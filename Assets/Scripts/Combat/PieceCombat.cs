using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PieceLauncher))]
[RequireComponent(typeof(PieceStats))]
public class PieceCombat : MonoBehaviour
{
    [Header("Collision Damage Settings")]
    [SerializeField] private float damageCooldown = 0.02f;
    [SerializeField] private float minimumDamageSpeed = 0.05f;

    private PieceLauncher launcher;
    private PieceStats pieceStats;

    private readonly Dictionary<int, float> lastDamageTimes =
        new Dictionary<int, float>();

    private void Awake()
    {
        launcher = GetComponent<PieceLauncher>();
        pieceStats = GetComponent<PieceStats>();
    }

    public void BeginAttack()
    {
        lastDamageTimes.Clear();
    }

    private void OnCollisionEnter2D(
        Collision2D collision
    )
    {
        ProcessCollision(collision);
    }

    private void OnCollisionStay2D(
        Collision2D collision
    )
    {
        ProcessCollision(collision);
    }

    private bool CanThisPieceDealDamage()
    {
        if (pieceStats == null)
        {
            return false;
        }

        // 有正式回合系统时：
        // 当前行动队伍的所有棋子都可以造成普通碰撞伤害
        if (TurnManager.Instance != null)
        {
            return TurnManager.Instance.IsTeamTurn(
                pieceStats.Team
            );
        }

        // 没有 TurnManager 时保留旧逻辑，
        // 方便单独测试物理场景
        if (launcher != null)
        {
            return launcher.CanDealDamageThisShot;
        }

        return false;
    }

    private void ProcessCollision(
        Collision2D collision
    )
    {
        if (!CanThisPieceDealDamage())
        {
            return;
        }

        if (pieceStats.IsDead)
        {
            return;
        }

        PieceStats targetStats =
            collision.collider.GetComponentInParent<PieceStats>();

        if (targetStats == null)
        {
            return;
        }

        if (targetStats == pieceStats)
        {
            return;
        }

        if (targetStats.IsDead)
        {
            return;
        }

        if (targetStats.Team == pieceStats.Team)
        {
            ActivateAllyForChainAttack(targetStats);
            return;
        }

        TryDamageEnemy(
            collision,
            targetStats
        );
    }

    private void ActivateAllyForChainAttack(
        PieceStats allyStats
    )
    {
        PieceLauncher allyLauncher =
            allyStats.GetComponent<PieceLauncher>();

        if (allyLauncher == null)
        {
            return;
        }

        allyLauncher.EnableChainAttackDamage();

        Debug.Log(
            pieceStats.gameObject.name +
            " activated " +
            allyStats.gameObject.name +
            " for chain attack damage."
        );
    }

    private void TryDamageEnemy(
        Collision2D collision,
        PieceStats targetStats
    )
    {
        float collisionSpeed =
            collision.relativeVelocity.magnitude;

        if (collisionSpeed < minimumDamageSpeed)
        {
            return;
        }

        int targetID =
            targetStats.GetInstanceID();

        if (lastDamageTimes.TryGetValue(
            targetID,
            out float lastDamageTime
        ))
        {
            float timeSinceLastDamage =
                Time.time - lastDamageTime;

            if (timeSinceLastDamage < damageCooldown)
            {
                return;
            }
        }

        lastDamageTimes[targetID] = Time.time;

        targetStats.TakeDamage(
            pieceStats.AttackPower,
            pieceStats
        );
    }

    private void OnValidate()
    {
        damageCooldown = Mathf.Max(
            0f,
            damageCooldown
        );

        minimumDamageSpeed = Mathf.Max(
            0f,
            minimumDamageSpeed
        );
    }
}