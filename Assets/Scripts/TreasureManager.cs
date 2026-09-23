using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Randomly spawns healing treasures on the board.
///  - At the start of a turn there is a spawnChance of a treasure appearing,
///    but only while the two teams have an uneven number of living pieces.
///  - Several treasures can be out at once (up to maxActiveTreasures); none of
///    them spawns on top of a piece, a wall or another treasure.
///  - Each one stays for turnsToStay turns, so both teams get a chance at it.
///  - Any piece that passes over one collects it for its own team:
///    every living piece on that team heals healAmount HP (up to its starting HP).
/// </summary>
public class TreasureManager : MonoBehaviour
{
    [Header("Spawning")]
    [SerializeField] [Range(0f, 1f)] private float spawnChance = 0.6f;
    [SerializeField] [Min(1)] private int turnsToStay = 2;
    [Tooltip("How many treasures may sit on the board at the same time.")]
    [SerializeField] [Min(1)] private int maxActiveTreasures = 3;
    [Tooltip("Distance from the camera edge the treasure must keep.")]
    [SerializeField] private float edgeMargin = 1.2f;
    [Tooltip("No collider (piece or wall) may be inside this radius when spawning.")]
    [SerializeField] private float clearRadius = 1.6f;
    [Tooltip("Minimum distance between two treasures.")]
    [SerializeField] private float treasureSpacing = 2.5f;
    [SerializeField] private int maxSpawnAttempts = 30;

    [Header("Reward")]
    [SerializeField] [Min(1)] private int healAmount = 1;

    [Header("Look")]
    [SerializeField] private float treasureRadius = 0.4f;
    [SerializeField] private Color treasureColor = new Color(1f, 0.8f, 0.2f);

    // A treasure on the board plus how many turns it has left.
    private class ActiveTreasure
    {
        public Treasure treasure;
        public int turnsLeft;
    }

    private TurnManager turnManager;
    private MatchWinManager winManager;
    private PieceStats[] pieces;

    private readonly List<ActiveTreasure> activeTreasures = new List<ActiveTreasure>();

    private void Start()
    {
        pieces = FindObjectsByType<PieceStats>(FindObjectsSortMode.None);

        turnManager = FindFirstObjectByType<TurnManager>();
        winManager = FindFirstObjectByType<MatchWinManager>();

        if (turnManager != null)
        {
            turnManager.TurnChanged += HandleTurnChanged;
        }

        if (winManager != null)
        {
            winManager.MatchOver += HandleMatchOver;
            winManager.MatchReset += DespawnAll;
        }
    }

    private void OnDestroy()
    {
        if (turnManager != null)
        {
            turnManager.TurnChanged -= HandleTurnChanged;
        }

        if (winManager != null)
        {
            winManager.MatchOver -= HandleMatchOver;
            winManager.MatchReset -= DespawnAll;
        }
    }

    private void HandleTurnChanged(PieceTeamSide team)
    {
        if (winManager != null && winManager.IsMatchOver)
        {
            return;
        }

        // Age the treasures that are already out before adding a new one.
        TickActiveTreasures();

        if (activeTreasures.Count >= maxActiveTreasures)
        {
            return;
        }

        // Treasures are a comeback mechanic: they only show up while one team
        // has more bodies on the board than the other.
        if (!TeamsAreUneven())
        {
            return;
        }

        if (Random.value < spawnChance)
        {
            TrySpawn();
        }
    }

    private void TickActiveTreasures()
    {
        for (int i = activeTreasures.Count - 1; i >= 0; i--)
        {
            ActiveTreasure entry = activeTreasures[i];

            if (entry.treasure == null)
            {
                activeTreasures.RemoveAt(i);
                continue;
            }

            entry.turnsLeft--;

            if (entry.turnsLeft <= 0)
            {
                Destroy(entry.treasure.gameObject);
                activeTreasures.RemoveAt(i);
            }
            else
            {
                entry.treasure.SetLastTurn(entry.turnsLeft == 1);
            }
        }
    }

    private bool TeamsAreUneven()
    {
        int blueAlive = 0;
        int redAlive = 0;

        foreach (PieceStats piece in pieces)
        {
            if (piece == null || piece.IsDead)
            {
                continue;
            }

            if (piece.Team == PieceTeamSide.Blue)
            {
                blueAlive++;
            }
            else
            {
                redAlive++;
            }
        }

        // A wiped out team means the match is already decided.
        return blueAlive > 0 && redAlive > 0 && blueAlive != redAlive;
    }

    private void HandleMatchOver(PieceTeamSide winner)
    {
        DespawnAll();
    }

    private void TrySpawn()
    {
        if (!TryFindSpawnPosition(out Vector2 position))
        {
            Debug.Log("Treasure: no free spot found this turn.");
            return;
        }

        // Borrow a piece's circle sprite and material so it renders like the pieces.
        SpriteRenderer pieceRenderer = null;
        foreach (PieceStats piece in pieces)
        {
            if (piece != null)
            {
                pieceRenderer = piece.GetComponent<SpriteRenderer>();
                break;
            }
        }

        GameObject go = new GameObject("Treasure");
        go.transform.position = position;

        Treasure treasure = go.AddComponent<Treasure>();
        treasure.Init(this, treasureRadius, treasureColor, pieceRenderer);
        treasure.SetLastTurn(turnsToStay == 1);

        activeTreasures.Add(new ActiveTreasure
        {
            treasure = treasure,
            turnsLeft = turnsToStay
        });

        Debug.Log(
            $"Treasure spawned at {position} for {turnsToStay} turns " +
            $"({activeTreasures.Count} on the board)."
        );
    }

    private bool TryFindSpawnPosition(out Vector2 position)
    {
        position = Vector2.zero;

        Camera cam = Camera.main;
        if (cam == null)
        {
            return false;
        }

        float halfHeight = cam.orthographicSize - edgeMargin;
        float halfWidth = cam.orthographicSize * cam.aspect - edgeMargin;
        Vector2 center = cam.transform.position;

        for (int i = 0; i < maxSpawnAttempts; i++)
        {
            Vector2 candidate = center + new Vector2(
                Random.Range(-halfWidth, halfWidth),
                Random.Range(-halfHeight, halfHeight)
            );

            // Nothing (pieces, walls) nearby = a fair spot nobody gets for free.
            if (Physics2D.OverlapCircle(candidate, clearRadius) != null)
            {
                continue;
            }

            // Keep treasures apart so one shot cannot sweep up a whole cluster.
            if (IsTooCloseToAnotherTreasure(candidate))
            {
                continue;
            }

            position = candidate;
            return true;
        }

        return false;
    }

    private bool IsTooCloseToAnotherTreasure(Vector2 candidate)
    {
        float squaredSpacing = treasureSpacing * treasureSpacing;

        foreach (ActiveTreasure entry in activeTreasures)
        {
            if (entry.treasure == null)
            {
                continue;
            }

            Vector2 other = entry.treasure.transform.position;

            if ((other - candidate).sqrMagnitude < squaredSpacing)
            {
                return true;
            }
        }

        return false;
    }

    // Called by Treasure when a piece passes over it.
    public void Collect(Treasure treasure, PieceStats collector)
    {
        int index = activeTreasures.FindIndex(entry => entry.treasure == treasure);

        if (index < 0)
        {
            return;
        }

        PieceTeamSide team = collector.Team;

        foreach (PieceStats piece in pieces)
        {
            if (piece == null || piece.IsDead || piece.Team != team)
            {
                continue;
            }

            // Heal up to the starting HP, never beyond it.
            int missing = piece.BaseHealth - piece.CurrentHealth;
            int amount = Mathf.Min(healAmount, missing);

            if (amount <= 0)
            {
                continue;
            }

            // PieceStatsDisplay pops up "HP +1" for this.
            piece.Heal(amount);
        }

        Debug.Log($"{collector.name} collected the treasure for {team}.");

        activeTreasures.RemoveAt(index);
        Destroy(treasure.gameObject);
    }

    private void DespawnAll()
    {
        foreach (ActiveTreasure entry in activeTreasures)
        {
            if (entry.treasure != null)
            {
                Destroy(entry.treasure.gameObject);
            }
        }

        activeTreasures.Clear();
    }
}
