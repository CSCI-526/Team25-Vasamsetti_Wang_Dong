using UnityEngine;

/// <summary>
/// Randomly spawns a healing treasure on the board.
///  - At the start of a turn there is a spawnChance of a treasure appearing
///    (only one at a time, never on top of a piece or against a wall).
///  - It stays for turnsToStay turns, so both teams get a chance at it.
///  - Any piece that passes over it collects it for its own team:
///    every living piece on that team heals healAmount HP (up to its starting HP).
/// </summary>
public class TreasureManager : MonoBehaviour
{
    [Header("Spawning")]
    [SerializeField] [Range(0f, 1f)] private float spawnChance = 0.3f;
    [SerializeField] [Min(1)] private int turnsToStay = 2;
    [Tooltip("Distance from the camera edge the treasure must keep.")]
    [SerializeField] private float edgeMargin = 1.2f;
    [Tooltip("No collider (piece or wall) may be inside this radius when spawning.")]
    [SerializeField] private float clearRadius = 1.6f;
    [SerializeField] private int maxSpawnAttempts = 30;

    [Header("Reward")]
    [SerializeField] [Min(1)] private int healAmount = 1;

    [Header("Look")]
    [SerializeField] private float treasureRadius = 0.4f;
    [SerializeField] private Color treasureColor = new Color(1f, 0.8f, 0.2f);

    private TurnManager turnManager;
    private MatchWinManager winManager;
    private PieceStats[] pieces;

    private Treasure activeTreasure;
    private int turnsLeft;

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
            winManager.MatchReset += Despawn;
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
            winManager.MatchReset -= Despawn;
        }
    }

    private void HandleTurnChanged(PieceTeamSide team)
    {
        if (winManager != null && winManager.IsMatchOver)
        {
            return;
        }

        // A treasure is already out: count down its remaining turns.
        if (activeTreasure != null)
        {
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

        if (Random.value < spawnChance)
        {
            TrySpawn();
        }
    }

    private void HandleMatchOver(PieceTeamSide winner)
    {
        Despawn();
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

        activeTreasure = go.AddComponent<Treasure>();
        activeTreasure.Init(this, treasureRadius, treasureColor, pieceRenderer);

        turnsLeft = turnsToStay;
        activeTreasure.SetLastTurn(turnsLeft == 1);

        Debug.Log($"Treasure spawned at {position} for {turnsToStay} turns.");
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
    public void Collect(Treasure treasure, PieceStats collector)
    {
        if (treasure != activeTreasure)
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

        Despawn();
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
