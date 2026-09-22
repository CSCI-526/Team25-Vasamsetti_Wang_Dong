using TMPro;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    [Header("Current Turn")]
    [SerializeField]
    private PieceTeamSide currentTeam = PieceTeamSide.Blue;

    [Header("References")]
    [SerializeField]
    private PhysicsStopDetector stopDetector;

    [SerializeField]
    private TMP_Text turnText;

    private PieceLauncher[] allLaunchers;

    // 表示本次行动是否已经开始
    private bool actionInProgress;

    // 胜负已定后停止切换回合
    private bool matchOver;

    // 为“首次击杀额外行动”技能提前准备
    private bool extraActionPending;
    // 每次正式切换队伍时增加，用来区分不同回合
    private int turnSequence = 1;
    public PieceTeamSide CurrentTeam => currentTeam;
    public int TurnSequence => turnSequence;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (stopDetector == null)
        {
            stopDetector =
                FindFirstObjectByType<PhysicsStopDetector>();
        }

        RefreshPieces();
        ApplyCurrentTurn();
    }

    private void Update()
    {
        if (stopDetector == null)
        {
            return;
        }

        // 有棋子正在移动，说明本次行动已经开始
        if (!stopDetector.AllPiecesStopped)
        {
            actionInProgress = true;
            return;
        }

        // 行动开始过，并且现在所有棋子已经停止
        if (actionInProgress)
        {
            actionInProgress = false;
            FinishCurrentAction();
        }
    }

    private void FinishCurrentAction()
    {
        // Match already decided: stop switching turns.
        if (matchOver)
        {
            return;
        }

        // 如果获得了额外行动，就不切换队伍
        if (extraActionPending)
        {
            extraActionPending = false;

            Debug.Log(
                $"{currentTeam} received an extra action."
            );

            ApplyCurrentTurn();
            return;
        }

        SwitchTeam();
    }

    private void SwitchTeam()
    {
        if (currentTeam == PieceTeamSide.Blue)
        {
            currentTeam = PieceTeamSide.Red;
        }
        else
        {
            currentTeam = PieceTeamSide.Blue;
        }

        // 只有正式切换队伍才算进入新回合
        // 额外行动不会增加这个编号
        turnSequence++;

        ApplyCurrentTurn();

        Debug.Log(
            $"Current turn: {currentTeam}, " +
            $"turn sequence: {turnSequence}"
        );
    }

    private void ApplyCurrentTurn()
    {
        if (allLaunchers == null)
        {
            RefreshPieces();
        }

        foreach (PieceLauncher piece in allLaunchers)
        {
            if (piece == null)
            {
                continue;
            }

            bool belongsToCurrentTeam =
                piece.Team == currentTeam;

            piece.SetControlEnabled(
                belongsToCurrentTeam
            );
        }

        UpdateTurnText();
    }

    private void UpdateTurnText()
    {
        if (turnText == null)
        {
            return;
        }

        if (currentTeam == PieceTeamSide.Blue)
        {
            turnText.text = "BLUE TURN";
            turnText.color = new Color(0.25f, 0.65f, 1f);
        }
        else
        {
            turnText.text = "RED TURN";
            turnText.color = new Color(1f, 0.35f, 0.35f);
        }
    }

    public bool IsTeamTurn(PieceTeamSide team)
    {
        return currentTeam == team;
    }

    public void GrantExtraAction(PieceTeamSide team)
    {
        // 只能给当前正在行动的队伍增加行动
        if (team != currentTeam)
        {
            return;
        }

        extraActionPending = true;
    }

    public void RefreshPieces()
    {
        allLaunchers = FindObjectsByType<PieceLauncher>(
            FindObjectsSortMode.None
        );
    }

    // Called by MatchWinManager when the match ends or restarts.
    public void SetMatchOver(bool value)
    {
        matchOver = value;
    }

    // Resets the turn state back to Blue's opening turn.
    public void ResetToStart()
    {
        matchOver = false;
        actionInProgress = false;
        extraActionPending = false;

        currentTeam = PieceTeamSide.Blue;
        turnSequence = 1;

        RefreshPieces();
        ApplyCurrentTurn();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}