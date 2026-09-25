using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Healing treasures. Every treasure belongs to one team and is tinted in its color.
///  - Only pieces of the owner team can heal from it. Touching it always uses it up:
///    the collector heals healPercent of its starting HP (rounded up, at least
///    minHeal), never above its starting HP. A full-HP collector passes the heal
///    to its most injured teammate.
///  - An enemy piece that touches it knocks it away: it disappears, nobody heals.
///  - It lasts turnsToStay turns in total (default 2 = one turn for each team).
///
/// Two sources of treasure:
///  1. Opening treasures (second-player compensation): when the match starts,
///     openingTreasureCount treasures appear on the second team's half of the board.
///     The first team gets one turn to knock them away, then the second team gets
///     one turn to use them.
///  2. Comeback treasure: at the start of the losing team's turn there is a
///     spawnChance of one appearing, if that team has an injured piece and no
///     other treasure is on the board. "Losing" = fewer living pieces; with equal
///     pieces, less total HP. It vanishes early if the owner is no longer behind
///     or nobody on the team is injured.
/// </summary>
public class TreasureManager : MonoBehaviour
{
    [Header("Opening Treasures (second team)")]
    [Tooltip("Treasures given to the team that moves second, placed on its half at match start.")]
    [SerializeField] [Min(0)] private int openingTreasureCount = 2;

    [Header("Comeback Treasure")]
    [Tooltip("Chance per losing-team turn that a treasure appears.")]
    [SerializeField] [Range(0f, 1f)] private float spawnChance = 0.5f;

    [Header("Spawning")]
    [Tooltip("Turns a treasure stays for, counting both teams (2 = one try each).")]
    [SerializeField] [Min(1)] private int turnsToStay = 2;
    [Tooltip("Distance from the camera edge a treasure must keep.")]
    [SerializeField] private float edgeMargin = 1.2f;
    [Tooltip("No collider (piece, wall or treasure) may be inside this radius when spawning.")]
    [SerializeField] private float clearRadius = 1.6f;
    [SerializeField] private int maxSpawnAttempts = 30;

    [Header("Reward")]
    [Tooltip("Share of the collector's starting HP that is healed (0.5 = Heavy 3, Striker 3, Speed 2).")]
    [SerializeField] [Range(0f, 1f)] private float healPercent = 0.5f;
    [SerializeField] [Min(1)] private int minHeal = 1;

    [Header("Look")]
    [SerializeField] private float treasureRadius = 0.4f;
    [SerializeField] private Color blueTreasureColor = new Color(0.45f, 0.75f, 1f);
    [SerializeField] private Color redTreasureColor = new Color(1f, 0.5f, 0.5f);

    private class ActiveTreasure
    {
        public Treasure Treasure;
        public PieceTeamSide Owner;
        public int TurnsLeft;
        public bool IsOpening;  // opening treasures are not removed early
    }

    private readonly List<ActiveTreasure> treasures = new List<ActiveTreasure>();

    private TurnManager turnManager;
    private MatchWinManager winManager;
    private PieceStats[] pieces;

    // Extra actions re-announce the same turn; only count real new turns.
    private int lastTurnSequence = -1;
    private bool openingSpawned;

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
            winManager.MatchReset += HandleMatchReset;
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
            winManager.MatchReset -= HandleMatchReset;
        }
    }

    private void HandleTurnChanged(PieceTeamSide team)
    {
        if (winManager != null && winManager.IsMatchOver)
        {
            return;
        }

        if (turnManager.TurnSequence == lastTurnSequence)
        {
            return;
        }

        lastTurnSequence = turnManager.TurnSequence;

        // First turn of the match: the team that is NOT moving now moves second.
        if (turnManager.TurnSequence == 1 && !openingSpawned)
        {
            openingSpawned = true;
            SpawnOpeningTreasures(Opponent(team));
            return;
        }

        bool hasLosingTeam = TryGetLosingTeam(out PieceTeamSide losingTeam);

        AgeTreasures(hasLosingTeam, losingTeam);

        // Comeback treasure: one at a time, only when the board is empty.
        if (treasures.Count > 0)
        {
            return;
        }

        // Only spawn at the start of the losing team's turn,
        // and only if one of its pieces can actually use the heal.
        if (!hasLosingTeam || team != losingTeam ||
            FindMostInjured(losingTeam) == null)
        {
            return;
        }

        if (Random.value < spawnChance)
        {
            TrySpawn(losingTeam, false, false);
        }
    }

    // Counts down every treasure by one turn and removes the ones that expired.
    private void AgeTreasures(bool hasLosingTeam, PieceTeamSide losingTeam)
    {
        for (int i = treasures.Count - 1; i >= 0; i--)
        {
            ActiveTreasure entry = treasures[i];

            // A comeback treasure is removed once its owner caught up
            // or has nobody left to heal.
            if (!entry.IsOpening &&
                (!hasLosingTeam || losingTeam != entry.Owner ||
                 FindMostInjured(entry.Owner) == null))
            {
                Debug.Log($"Treasure: {entry.Owner} no longer needs it, treasure removed.");
                Remove(entry);
                continue;
            }

            entry.TurnsLeft--;

            if (entry.TurnsLeft <= 0)
            {
                Remove(entry);
            }
            else
            {
                entry.Treasure.SetLastTurn(entry.TurnsLeft == 1);
            }
        }
    }

    private void SpawnOpeningTreasures(PieceTeamSide owner)
    {
        for (int i = 0; i < openingTreasureCount; i++)
        {
            TrySpawn(owner, true, true);
        }
    }

    private static PieceTeamSide Opponent(PieceTeamSide team)
    {
        return team == PieceTeamSide.Blue ? PieceTeamSide.Red : PieceTeamSide.Blue;
    }

    // Fewer living pieces loses; with equal pieces, less total HP loses.
    private bool TryGetLosingTeam(out PieceTeamSide losingTeam)
    {
        losingTeam = PieceTeamSide.Blue;

        int blueAlive = 0, redAlive = 0;
        int blueHealth = 0, redHealth = 0;

        foreach (PieceStats piece in pieces)
        {
            if (piece == null || piece.IsDead)
            {
                continue;
            }

            if (piece.Team == PieceTeamSide.Blue)
            {
                blueAlive++;
                blueHealth += piece.CurrentHealth;
            }
            else
            {
                redAlive++;
                redHealth += piece.CurrentHealth;
            }
        }

        if (blueAlive != redAlive)
        {
            losingTeam = blueAlive < redAlive ? PieceTeamSide.Blue : PieceTeamSide.Red;
            return true;
        }

        if (blueHealth != redHealth)
        {
            losingTeam = blueHealth < redHealth ? PieceTeamSide.Blue : PieceTeamSide.Red;
            return true;
        }

        return false;
    }

    private void HandleMatchOver(PieceTeamSide winner)
    {
        RemoveAll();
    }

    private void HandleMatchReset()
    {
        RemoveAll();
        lastTurnSequence = -1;
        openingSpawned = false;
    }

    private void TrySpawn(PieceTeamSide owner, bool onOwnerHalf, bool isOpening)
    {
        if (!TryFindSpawnPosition(owner, onOwnerHalf, out Vector2 position))
        {
            Debug.Log("Treasure: no free spot found.");
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

        GameObject go = new GameObject(isOpening ? "OpeningTreasure" : "Treasure");
        go.transform.position = position;

        Color color = owner == PieceTeamSide.Blue ? blueTreasureColor : redTreasureColor;

        Treasure treasure = go.AddComponent<Treasure>();
        treasure.Init(this, treasureRadius, color, pieceRenderer);

        ActiveTreasure entry = new ActiveTreasure
        {
            Treasure = treasure,
            Owner = owner,
            TurnsLeft = turnsToStay,
            IsOpening = isOpening
        };

        treasure.SetLastTurn(entry.TurnsLeft == 1);
        treasures.Add(entry);

        Debug.Log($"Treasure spawned for {owner} at {position} for {turnsToStay} turns.");
    }

    // onOwnerHalf: keep to the side of the board where the owner's pieces are.
    private bool TryFindSpawnPosition(PieceTeamSide owner, bool onOwnerHalf, out Vector2 position)
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

        float minX = -halfWidth;
        float maxX = halfWidth;

        if (onOwnerHalf)
        {
            if (OwnerSideSign(owner, center.x) > 0f)
            {
                minX = edgeMargin;
            }
            else
            {
                maxX = -edgeMargin;
            }
        }

        for (int i = 0; i < maxSpawnAttempts; i++)
        {
            Vector2 candidate = center + new Vector2(
                Random.Range(minX, maxX),
                Random.Range(-halfHeight, halfHeight)
            );

            // Nothing (pieces, walls, other treasures) nearby.
            if (Physics2D.OverlapCircle(candidate, clearRadius) == null &&
                !IsNearTreasure(candidate))
            {
                position = candidate;
                return true;
            }
        }

        return false;
    }

    // +1 if the owner's living pieces sit right of the board center, -1 if left.
    private float OwnerSideSign(PieceTeamSide owner, float centerX)
    {
        float sum = 0f;
        int count = 0;

        foreach (PieceStats piece in pieces)
        {
            if (piece == null || piece.IsDead || piece.Team != owner)
            {
                continue;
            }

            sum += piece.transform.position.x;
            count++;
        }

        if (count == 0)
        {
            return 1f;
        }

        return sum / count >= centerX ? 1f : -1f;
    }

    // Backup for OverlapCircle: a treasure made this frame may not be in the physics scene yet.
    private bool IsNearTreasure(Vector2 candidate)
    {
        foreach (ActiveTreasure entry in treasures)
        {
            Vector2 other = entry.Treasure.transform.position;

            if (Vector2.Distance(candidate, other) < clearRadius)
            {
                return true;
            }
        }

        return false;
    }

    // Called by Treasure when a piece passes over it.
    // Returns true when the treasure is used up (healed or knocked away).
    public bool TryCollect(Treasure treasure, PieceStats collector)
    {
        ActiveTreasure entry = treasures.Find(t => t.Treasure == treasure);

        if (entry == null || collector == null || collector.IsDead)
        {
            return false;
        }

        // The other team can only knock it away, not heal from it.
        if (collector.Team != entry.Owner)
        {
            FloatingText.Spawn(
                treasure.transform.position + Vector3.up * 0.6f,
                "DENIED",
                new Color(0.8f, 0.8f, 0.8f)
            );

            Debug.Log($"{collector.name} knocked away a {entry.Owner} treasure.");

            Remove(entry);
            return true;
        }

        // Any owner piece picks it up. If the collector is at full HP,
        // the most injured teammate gets the heal instead.
        PieceStats target = IsInjured(collector)
            ? collector
            : FindMostInjured(entry.Owner);

        if (target != null)
        {
            int heal = Mathf.Max(minHeal, Mathf.CeilToInt(target.BaseHealth * healPercent));

            // Heal caps at the starting HP; PieceStatsDisplay pops up "HP +2".
            target.Heal(heal);

            Debug.Log($"{collector.name} collected a {entry.Owner} treasure. {target.name} was healed.");
        }
        else
        {
            Debug.Log($"{collector.name} collected a {entry.Owner} treasure, but nobody needed healing.");
        }

        Remove(entry);
        return true;
    }

    private static bool IsInjured(PieceStats piece)
    {
        return piece != null &&
               !piece.IsDead &&
               piece.CurrentHealth < piece.BaseHealth;
    }

    // Living piece of this team missing the most HP, or null if nobody is hurt.
    private PieceStats FindMostInjured(PieceTeamSide team)
    {
        PieceStats best = null;
        int bestMissing = 0;

        foreach (PieceStats piece in pieces)
        {
            if (!IsInjured(piece) || piece.Team != team)
            {
                continue;
            }

            int missing = piece.BaseHealth - piece.CurrentHealth;

            if (missing > bestMissing)
            {
                best = piece;
                bestMissing = missing;
            }
        }

        return best;
    }

    private void Remove(ActiveTreasure entry)
    {
        if (entry.Treasure != null)
        {
            Destroy(entry.Treasure.gameObject);
        }

        treasures.Remove(entry);
    }

    private void RemoveAll()
    {
        for (int i = treasures.Count - 1; i >= 0; i--)
        {
            Remove(treasures[i]);
        }
    }
}
