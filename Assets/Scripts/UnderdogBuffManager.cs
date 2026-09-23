using UnityEngine;

/// <summary>
/// Underdog buff:
///  - Every time a piece dies, each surviving teammate gets +1 ATK.
///  - When only one piece is left on a team, its ATK doubles.
/// Buffs are normal attack points, so a match restart clears them.
/// </summary>
public class UnderdogBuffManager : MonoBehaviour
{
    [SerializeField] [Min(0)] private int attackPerFallenTeammate = 1;
    [SerializeField] private bool doubleLastSurvivor = true;

    private PieceStats[] pieces;

    private void Start()
    {
        pieces = FindObjectsByType<PieceStats>(FindObjectsSortMode.None);

        foreach (PieceStats piece in pieces)
        {
            // Pass the dead piece along so we know which team lost a member.
            piece.Died += attacker => HandlePieceDied(piece);
        }
    }

    private void HandlePieceDied(PieceStats deadPiece)
    {
        PieceStats lastSurvivor = null;
        int survivors = 0;

        // +1 ATK to every living teammate of the fallen piece.
        foreach (PieceStats piece in pieces)
        {
            if (piece == null || piece.IsDead || piece.Team != deadPiece.Team)
            {
                continue;
            }

            piece.AddAttackPower(attackPerFallenTeammate);

            survivors++;
            lastSurvivor = piece;
        }

        // Last one standing: double its attack.
        if (doubleLastSurvivor && survivors == 1)
        {
            lastSurvivor.AddAttackPower(lastSurvivor.AttackPower);

            Debug.Log($"{lastSurvivor.name} is the last survivor. ATK doubled!");
        }
    }
}
