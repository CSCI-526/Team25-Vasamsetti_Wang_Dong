using System.Collections;
using TMPro;
using UnityEngine;

public class PieceStatsDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PieceStats stats;
    [SerializeField] private TMP_Text statsText;

    [Header("Change Popups")]
    [Tooltip("Seconds the \"HP +1\" / \"ATK +3\" popup shows before the numbers update.")]
    [SerializeField] private float valueUpdateDelay = 0.8f;
    [SerializeField] private float popupHeight = 1.1f;
    [SerializeField] private float popupSpacing = 0.45f;
    [SerializeField] private Color hpGainColor = new Color(0.4f, 1f, 0.5f);
    [SerializeField] private Color hpLossColor = new Color(1f, 0.35f, 0.3f);
    [SerializeField] private Color atkGainColor = new Color(1f, 0.8f, 0.2f);
    [SerializeField] private Color atkLossColor = new Color(0.7f, 0.7f, 0.7f);

    // Values the last popup was based on (ahead of what the text shows).
    private int knownHealth;
    private int knownAttack;
    private bool initialized;
    private bool changePending;
    private Coroutine pendingTextUpdate;

    private void Awake()
    {
        // No PieceStats assigned in the Inspector: find it automatically.
        if (stats == null)
        {
            stats = GetComponentInParent<PieceStats>();
        }

        // Script and text component on the same object: look on ourselves.
        if (statsText == null)
        {
            statsText = GetComponent<TMP_Text>();
        }

        // Script on the piece root: look for the text among the children.
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
            stats.StatsReset += SnapToCurrent;
        }
    }

    private void Start()
    {
        SnapToCurrent();
    }

    private void OnDisable()
    {
        if (stats != null)
        {
            stats.StatsChanged -= HandleStatsChanged;
            stats.StatsReset -= SnapToCurrent;
        }
    }

    private void HandleStatsChanged()
    {
        if (stats == null)
        {
            return;
        }

        if (!initialized)
        {
            SnapToCurrent();
            return;
        }

        // Handled in LateUpdate so several changes in one frame
        // (e.g. +1 ATK then doubled) show as a single popup.
        changePending = true;
    }

    private void LateUpdate()
    {
        if (!changePending || stats == null)
        {
            return;
        }

        changePending = false;

        int healthChange = stats.CurrentHealth - knownHealth;
        int attackChange = stats.AttackPower - knownAttack;

        knownHealth = stats.CurrentHealth;
        knownAttack = stats.AttackPower;

        if (healthChange == 0 && attackChange == 0)
        {
            return;
        }

        // First show what changed, e.g. "HP -2" / "ATK +3" ...
        float height = popupHeight;

        if (healthChange != 0)
        {
            ShowPopup("HP", healthChange, healthChange > 0 ? hpGainColor : hpLossColor, height);
            height += popupSpacing;
        }

        if (attackChange != 0)
        {
            ShowPopup("ATK", attackChange, attackChange > 0 ? atkGainColor : atkLossColor, height);
        }

        // ... then update the numbers on the piece a moment later.
        if (pendingTextUpdate != null)
        {
            StopCoroutine(pendingTextUpdate);
        }

        pendingTextUpdate = StartCoroutine(UpdateTextAfterDelay());
    }

    private void ShowPopup(string statName, int change, Color color, float height)
    {
        string sign = change > 0 ? "+" : "-";

        FloatingText.Spawn(
            stats.transform.position + Vector3.up * height,
            $"{statName} {sign}{Mathf.Abs(change)}",
            color
        );
    }

    private IEnumerator UpdateTextAfterDelay()
    {
        yield return new WaitForSeconds(valueUpdateDelay);

        pendingTextUpdate = null;
        UpdateText();
    }

    // Jump straight to the current values with no popup (start / match restart).
    private void SnapToCurrent()
    {
        if (stats == null)
        {
            return;
        }

        if (pendingTextUpdate != null)
        {
            StopCoroutine(pendingTextUpdate);
            pendingTextUpdate = null;
        }

        knownHealth = stats.CurrentHealth;
        knownAttack = stats.AttackPower;
        initialized = true;
        changePending = false;

        UpdateText();
    }

    private void UpdateText()
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
