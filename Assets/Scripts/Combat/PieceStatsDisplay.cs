using TMPro;
using UnityEngine;

public class PieceStatsDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PieceStats stats;
    [SerializeField] private TMP_Text statsText;

    private void Awake()
    {
        // 如果没有手动拖入 PieceStats，就自动寻找
        if (stats == null)
        {
            stats = GetComponentInParent<PieceStats>();
        }

        // 如果脚本和文字组件在同一个物体上，就从自身寻找
        if (statsText == null)
        {
            statsText = GetComponent<TMP_Text>();
        }

        // 如果脚本放在棋子根物体上，就从子物体中寻找文字
        if (statsText == null)
        {
            statsText = GetComponentInChildren<TMP_Text>(true);
        }
    }

    private void OnEnable()
    {
        if (stats != null)
        {
            stats.StatsChanged += HandleStatsChanged;
        }
    }

    private void Start()
    {
        HandleStatsChanged();
    }

    private void OnDisable()
    {
        if (stats != null)
        {
            stats.StatsChanged -= HandleStatsChanged;
        }
    }

    private void HandleStatsChanged()
    {
        if (stats == null || statsText == null)
        {
            return;
        }

        statsText.text =
            $"HP {stats.CurrentHealth}\n" +
            $"ATK {stats.AttackPower}";
    }
}