using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PieceStats))]
[RequireComponent(typeof(PieceAbility))]
public class PieceAllyAbilityCollision : MonoBehaviour
{
    private PieceStats pieceStats;
    private PieceAbility pieceAbility;

    private void Awake()
    {
        pieceStats = GetComponent<PieceStats>();
        pieceAbility = GetComponent<PieceAbility>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (pieceStats == null || pieceAbility == null)
        {
            return;
        }

        if (pieceStats.IsDead)
        {
            return;
        }

        PieceStats otherStats =
            collision.collider.GetComponentInParent<PieceStats>();

        if (otherStats == null)
        {
            return;
        }

        if (otherStats == pieceStats)
        {
            return;
        }

        // 技能只对友军生效
        if (otherStats.Team != pieceStats.Team)
        {
            return;
        }

        pieceAbility.TryActivateOnAlly(otherStats);
    }
}