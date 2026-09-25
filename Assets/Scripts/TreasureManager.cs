using UnityEngine;

/// <summary>
/// Comeback treasure: a healing pickup that only helps the team that is behind.
///  - "Behind" = fewer living pieces; with equal pieces, less total HP.
///    If both teams are exactly even, no treasure appears.
///  - At the start of the losing team's turn there is a spawnChance of a treasure
///    appearing (only one at a time, never on top of a piece or against a wall).
///    It is tinted in that team's color.
///  - Only pieces of that team can collect it; enemy pieces pass straight through.
///    The first injured piece to touch it heals healPercent of its starting HP
///    (rounded up, at least minHeal), never above its starting HP.
///  - It lasts for turnsToStay of the owner's turns (counting the one it spawned on),
///    and vanishes early if the owner is no longer behind.
/// </summary>
public class TreasureManager : MonoBehaviour
{
    [Header("Spawning")]
    [Tooltip("Chance per losing-team turn that a treasure appears.")]
    [SerializeField] [Range(0f, 1f)] private float spawnChance = 0.5f;
    [Tooltip("How many of the owner team's turns the treasure stays for.")]
    [SerializeField] [Min(1)] private int turnsToStay = 2;
    [Tooltip("Distance from the camera edge the treasure must keep.")]
    [SerializeField] private float edgeMargin = 1.2f;
    [Tooltip("No collider (piece or wall) may be inside this radius when spawning.")]
    [SerializeField] private float clearRadius = 1.6f;
    [SerializeField] private int maxSpawnAttempts = 30;

    [Header("Reward")]
    [Tooltip("Share of the collector's starting HP that is healed (0.5 = Heavy 2, Striker 2, Speed 1).")]
    [SerializeField] [Range(0f, 1f)] private float healPercent = 0.5f;
    [SerializeField] [Min(1)] private int minHeal = 1;

    [Header("Look")]
    [SerializeField] private float treasureRadius = 0.4f;
    [SerializeField] private Color blueTreasureColor = new Color(0.45f, 0.75f, 1f);
    [SerializeField] private Color redTreasureColor = new Color(1f, 0.5f, 0.5f);

    private TurnManager turnManager;
    private MatchWinManager winManager;
    private PieceStats[] pieces;

    private Treasure activeTreasure;
    private PieceTeamSide ownerTeam;
    private int turnsLeft;

    // Extra actions re-announce the same turn; only count real new turns.
    private int lastTurnSequence = -1;

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

        bool hasLosingTeam = TryGetLosingTeam(out PieceTeamSide losingTeam);

        if (activeTreasure != null)
        {
            // The owner caught up: the comeback help is no longer needed.
            if (!hasLosingTeam || losingTeam != ownerTeam)
            {
                Debug.Log($"Treasure: {ownerTeam} is no longer behind, treasure removed.");
                Despawn();
                return;
            }

            // Count down only on the owner's own turns.
            if (team != ownerTeam)
            {
                return;
            }

            turnsLeft--;

            if (turnsLeft <= 0)
            {
                Despawn();
            }
            else
            {
                activeTreasure.SetLastTurn(turnsLeft == 1);
            }

            return;
        }

        // Only spawn at the start of the losing team's turn.
        if (!hasLosingTeam || team != losingTeam)
        {
            return;
        }

        if (Random.value < spawnChance)
        {
            TrySpawn(losingTeam);
        }
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
        Despawn();
    }

    private void HandleMatchReset()
    {
        Despawn();
        lastTurnSequence = -1;
    }

    private void TrySpawn(PieceTeamSide owner)
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

        Color color = owner == PieceTeamSide.Blue ? blueTreasureColor : redTreasureColor;

        activeTreasure = go.AddComponent<Treasure>();
        activeTreasure.Init(this, treasureRadius, color, pieceRenderer);

        ownerTeam = owner;
        turnsLeft = turnsToStay;
        activeTreasure.SetLastTurn(turnsLeft == 1);

        Debug.Log($"Treasure spawned for {owner} at {position} for {turnsToStay} of their turns.");
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
            if (Physics2D.OverlapCircle(candidate, clearRadius) == null)
            {
                position = candidate;
                return true;
            }
        }

        return false;
    }

    // Called by Treasure when a piece passes over it.
    // Returns false if the piece did not take it (enemy piece, or already at full HP).
    public bool TryCollect(Treasure treasure, PieceStats collector)
    {
        if (treasure != activeTreasure || collector == null || collector.IsDead)
        {
            return false;
        }

        // Only the team that is behind may use it.
        if (collector.Team != ownerTeam)
        {
            return false;
        }

        // Heal up to the starting HP, never beyond it.
        int missing = collector.BaseHealth - collector.CurrentHealth;

        if (missing <= 0)
        {
            return false;
        }

        int heal = Mathf.Max(minHeal, Mathf.CeilToInt(collector.BaseHealth * healPercent));
        int amount = Mathf.Min(heal, missing);

        // PieceStatsDisplay pops up "HP +2" for this.
        collector.Heal(amount);

        Debug.Log($"{collector.name} collected the {ownerTeam} treasure and healed {amount} HP.");

        Despawn();
        return true;
    }

    private void Despawn()
    {
        if (activeTreasure != null)
        {
            Destroy(activeTreasure.gameObject);
        }

        activeTreasure = null;
        turnsLeft = 0;
    }
}
