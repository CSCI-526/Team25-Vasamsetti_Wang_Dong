using TMPro;
using UnityEngine;

[RequireComponent(typeof(PieceStats))]
public class PieceStatsDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text statsText;

    private PieceStats stats;

    private void Awake()
    {
        stats = GetComponent<PieceStats>();

        if (statsText == null)
        {
            statsText = GetComponentInChildren<TMP_Text>(true);
        }
    }

    private void OnEnable()
    {
        if (stats == null)
        {
            stats = GetComponent<PieceStats>();
        }

        stats.StatsChanged += HandleStatsChanged;
    }

    private void Start()
    {
        RefreshDisplay();
    }

    private void OnDisable()
    {
        if (stats != null)
        {
            stats.StatsChanged -= HandleStatsChanged;
        }
    }

    private void HandleStatsChanged(
        PieceStats changedStats
    )
    {
        RefreshDisplay();
    }

    private void RefreshDisplay()
    {
        if (statsText == null)
        {
            Debug.LogError(
                "Stats Text is missing on " +
                gameObject.name,
                this
            );

            return;
        }

        statsText.text =
            "HP " +
            stats.CurrentHealth +
            "/" +
            stats.MaxHealth +
            "\nATK " +
            stats.AttackPower;
    }
}