using System;
using TMPro;
using UnityEngine;

public class MatchScoreManager : MonoBehaviour
{
    [Header("Score")]
    [SerializeField] private int blueScore;
    [SerializeField] private int redScore;

    [Header("UI")]
    [SerializeField] private TMP_Text scoreText;

    private PieceStats[] registeredPieces;

    public int BlueScore => blueScore;
    public int RedScore => redScore;

    // Fired whenever either team's score changes (used by ScoreDashboard).
    public event Action ScoreChanged;

    private void Start()
    {
        blueScore = 0;
        redScore = 0;

        RegisterAllPieces();
        UpdateScoreText();
    }

    private void RegisterAllPieces()
    {
        registeredPieces = FindObjectsByType<PieceStats>(
            FindObjectsSortMode.None
        );

        foreach (PieceStats piece in registeredPieces)
        {
            piece.Died += HandlePieceDefeated;
        }
    }

    private void HandlePieceDefeated(PieceStats attacker)
    {
        // 如果没有攻击者，例如以后加入地图陷阱，就不加分
        if (attacker == null)
        {
            return;
        }

        if (attacker.Team == PieceTeamSide.Blue)
        {
            blueScore++;
        }
        else if (attacker.Team == PieceTeamSide.Red)
        {
            redScore++;
        }

        Debug.Log(
            $"Score updated: Blue {blueScore} - {redScore} Red"
        );

        UpdateScoreText();
    }

    private void UpdateScoreText()
    {
        ScoreChanged?.Invoke();

        if (scoreText == null)
        {
            return;
        }

        scoreText.text =
            $"BLUE  {blueScore}  :  {redScore}  RED";
    }

    public void ResetScores()
    {
        blueScore = 0;
        redScore = 0;

        UpdateScoreText();
    }

    private void OnDestroy()
    {
        if (registeredPieces == null)
        {
            return;
        }

        foreach (PieceStats piece in registeredPieces)
        {
            if (piece != null)
            {
                piece.Died -= HandlePieceDefeated;
            }
        }
    }
}