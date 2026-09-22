using System;
using UnityEngine;

/// <summary>
/// Detects the win condition (a whole team eliminated) and exposes the result
/// so the UI (Person 3) can show BLUE WINS / RED WINS. Also owns the match
/// restart logic that the Restart button can call via RestartMatch().
/// Does NOT touch MatchScoreManager, which keeps its own kill counter.
/// </summary>
public class MatchWinManager : MonoBehaviour
{
    public static MatchWinManager Instance { get; private set; }

    [Header("References")]
    [SerializeField]
    private TurnManager turnManager;

    private PieceStats[] pieces;

    public bool IsMatchOver { get; private set; }

    // Null while the match is still running.
    public PieceTeamSide? Winner { get; private set; }

    // Fired once when a team wins. Argument is the winning team.
    public event Action<PieceTeamSide> MatchOver;

    // Fired when the match has been reset back to its starting state.
    public event Action MatchReset;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (turnManager == null)
        {
            turnManager = FindFirstObjectByType<TurnManager>();
        }

        RegisterPieces();
    }

    private void RegisterPieces()
    {
        pieces = FindObjectsByType<PieceStats>(
            FindObjectsSortMode.None
        );

        foreach (PieceStats piece in pieces)
        {
            if (piece != null)
            {
                piece.Died += HandlePieceDied;
            }
        }
    }

    private void HandlePieceDied(PieceStats attacker)
    {
        if (IsMatchOver)
        {
            return;
        }

        CheckForWinner();
    }

    private void CheckForWinner()
    {
        bool blueAlive = false;
        bool redAlive = false;

        foreach (PieceStats piece in pieces)
        {
            if (piece == null || piece.IsDead)
            {
                continue;
            }

            if (piece.Team == PieceTeamSide.Blue)
            {
                blueAlive = true;
            }
            else
            {
                redAlive = true;
            }
        }

        // Both teams still have pieces: the match continues.
        if (blueAlive && redAlive)
        {
            return;
        }

        if (blueAlive)
        {
            DeclareWinner(PieceTeamSide.Blue);
        }
        else if (redAlive)
        {
            DeclareWinner(PieceTeamSide.Red);
        }
        else
        {
            // Mutual wipe in the same action: award it to the team
            // that is currently acting (the attacker's side).
            PieceTeamSide fallback =
                turnManager != null
                    ? turnManager.CurrentTeam
                    : PieceTeamSide.Blue;

            DeclareWinner(fallback);
        }
    }

    private void DeclareWinner(PieceTeamSide team)
    {
        IsMatchOver = true;
        Winner = team;

        if (turnManager != null)
        {
            turnManager.SetMatchOver(true);
        }

        DisableAllInput();

        Debug.Log($"Match over. Winner: {team}");

        MatchOver?.Invoke(team);
    }

    private void DisableAllInput()
    {
        PieceLauncher[] launchers =
            FindObjectsByType<PieceLauncher>(
                FindObjectsSortMode.None
            );

        foreach (PieceLauncher launcher in launchers)
        {
            if (launcher != null)
            {
                launcher.SetControlEnabled(false);
            }
        }
    }

    /// <summary>
    /// Resets every piece, the turn state and the win state back to the
    /// starting configuration. Meant to be wired to the Restart button.
    /// </summary>
    public void RestartMatch()
    {
        if (pieces == null)
        {
            RegisterPieces();
        }

        foreach (PieceStats piece in pieces)
        {
            if (piece != null)
            {
                piece.ResetToStart();
            }
        }

        IsMatchOver = false;
        Winner = null;

        if (turnManager != null)
        {
            turnManager.SetMatchOver(false);
            turnManager.ResetToStart();
        }

        Debug.Log("Match restarted.");

        MatchReset?.Invoke();
    }

    private void OnDestroy()
    {
        if (pieces != null)
        {
            foreach (PieceStats piece in pieces)
            {
                if (piece != null)
                {
                    piece.Died -= HandlePieceDied;
                }
            }
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }
}
