using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PieceLauncher))]
[RequireComponent(typeof(PieceStats))]
public class PieceCombat : MonoBehaviour
{
    private PieceLauncher launcher;
    private PieceStats stats;

    private readonly HashSet<int> damagedTargets =
        new HashSet<int>();

    private void Awake()
    {
        launcher = GetComponent<PieceLauncher>();
        stats = GetComponent<PieceStats>();
    }

    public void BeginAttack()
    {
        damagedTargets.Clear();
    }

    private void OnCollisionEnter2D(
        Collision2D collision
    )
    {
        // Only the piece actively launched by the player
        // can cause damage.
        if (!launcher.IsActiveAttacker)
        {
            return;
        }

        if (stats.IsDead)
        {
            return;
        }

        PieceStats targetStats =
            collision.collider.GetComponentInParent<PieceStats>();

        // Walls and normal obstacles do not have PieceStats.
        if (targetStats == null)
        {
            return;
        }

        if (targetStats == stats)
        {
            return;
        }

        // Friendly pieces do not take damage.
        if (targetStats.Team == stats.Team)
        {
            return;
        }

        if (targetStats.IsDead)
        {
            return;
        }

        int targetID = targetStats.GetInstanceID();

        // The same target can only be damaged once
        // during one launch.
        if (damagedTargets.Contains(targetID))
        {
            return;
        }

        damagedTargets.Add(targetID);

        targetStats.TakeDamage(
            stats.AttackPower,
            stats
        );
    }
}