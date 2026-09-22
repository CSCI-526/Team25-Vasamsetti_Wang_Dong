using System;
using UnityEngine;
using UnityEngine.Serialization;

public class PieceStats : MonoBehaviour
{
    [Header("Base Stats")]
    [SerializeField]
    [Min(1)]
    [FormerlySerializedAs("maxHealth")]
    private int baseHealth = 10;

    [SerializeField]
    [Min(0)]
    [FormerlySerializedAs("attackPower")]
    private int baseAttackPower = 2;

    [Header("Piece Type")]
    [Tooltip("When assigned, this type config drives HP / attack / mass / launch force.")]
    [SerializeField]
    private PieceTypeDefinition typeDefinition;

    [Header("Runtime Stats - Do Not Edit")]
    [SerializeField]
    private int currentHealth;

    [SerializeField]
    private int currentAttackPower;

    [SerializeField]
    private bool isDead;

    private PieceLauncher launcher;
    private Rigidbody2D rb;

    private Vector3 startingPosition;
    private Quaternion startingRotation;

    private Collider2D[] pieceColliders;
    private Renderer[] pieceRenderers;

    public int BaseHealth => baseHealth;
    public int CurrentHealth => currentHealth;
    public int BaseAttackPower => baseAttackPower;
    public int AttackPower => currentAttackPower;
    public bool IsDead => isDead;

    public PieceTeamSide Team
    {
        get
        {
            if (launcher == null)
            {
                launcher = GetComponent<PieceLauncher>();
            }

            return launcher.Team;
        }
    }

    public event Action HealthChanged;
    public event Action StatsChanged;

    // Fired when stats are restored to their starting values (match restart),
    // just before StatsChanged, so displays can snap instead of animating.
    public event Action StatsReset;

    // 参数是击杀者
    public event Action<PieceStats> Died;
    // 当前棋子完成击杀时触发
    // 参数是被击杀的敌方棋子
    public event Action<PieceStats> KilledEnemy;
    public event Action<PieceStats, int> Damaged;


    private void Awake()
    {
        launcher = GetComponent<PieceLauncher>();
        rb = GetComponent<Rigidbody2D>();

        startingPosition = transform.position;
        startingRotation = transform.rotation;

        pieceColliders =
            GetComponentsInChildren<Collider2D>(true);

        pieceRenderers =
            GetComponentsInChildren<Renderer>(true);

        ApplyTypeDefinition();

        ResetRuntimeStats();
    }

    // Pulls base stats, mass and launch force from the assigned type config.
    private void ApplyTypeDefinition()
    {
        if (typeDefinition == null)
        {
            return;
        }

        baseHealth = Mathf.Max(1, typeDefinition.MaxHP);
        baseAttackPower = Mathf.Max(0, typeDefinition.Attack);

        if (rb != null)
        {
            rb.mass = Mathf.Max(0.01f, typeDefinition.Mass);
        }

        if (launcher != null)
        {
            launcher.SetLaunchForceMultiplier(
                typeDefinition.LaunchForceMultiplier
            );
        }
    }

    public void RegisterKill(PieceStats defeatedPiece)
    {
        if (defeatedPiece == null)
        {
            return;
        }

        if (defeatedPiece.Team == Team)
        {
            return;
        }

        KilledEnemy?.Invoke(defeatedPiece);
    }

    public void TakeDamage(int damage, PieceStats attacker)
    {
        if (isDead || damage <= 0)
        {
            return;
        }

        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0);

        HealthChanged?.Invoke();
        StatsChanged?.Invoke();

        // 通知技能系统：这枚棋子确实受到了伤害
        Damaged?.Invoke(attacker, damage);

        Debug.Log(
            $"{name} took {damage} damage. " +
            $"Remaining HP: {currentHealth}"
        );

        if (currentHealth <= 0)
        {
            Die(attacker);
        }
    }

    public void Heal(int amount)
    {
        if (isDead || amount <= 0)
        {
            return;
        }

        currentHealth += amount;

        HealthChanged?.Invoke();
        StatsChanged?.Invoke();

        Debug.Log(
            $"{name} gained {amount} HP. " +
            $"Current HP: {currentHealth}"
        );
    }

    public void AddAttackPower(int amount)
    {
        if (isDead || amount <= 0)
        {
            return;
        }

        currentAttackPower += amount;

        StatsChanged?.Invoke();

        Debug.Log(
            $"{name} gained {amount} attack. " +
            $"Current ATK: {currentAttackPower}"
        );
    }

    private void Die(PieceStats attacker)
    {
        if (isDead)
        {
            return;
        }

        isDead = true;

        Debug.Log(
            $"{name} was defeated by " +
            $"{(attacker != null ? attacker.name : "unknown")}"
        );

        // 通知计分系统：当前棋子被击杀
        Died?.Invoke(attacker);

        // 通知攻击者：它完成了一次击杀
        if (attacker != null)
        {
            attacker.RegisterKill(this);
        }

        // Disable the defeated piece's physics and visuals.
        // It stays eliminated for the rest of the match (no respawn).
        EnterDefeatedState();
    }

    private void EnterDefeatedState()
    {
        // Disable all colliders.
        for (int i = 0; i < pieceColliders.Length; i++)
        {
            pieceColliders[i].enabled = false;
        }

        // Hide the piece, its stats text and any ability markers.
        for (int i = 0; i < pieceRenderers.Length; i++)
        {
            pieceRenderers[i].enabled = false;
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = false;
        }
    }

    /// <summary>
    /// Fully restores this piece to its starting state.
    /// Called by MatchWinManager.RestartMatch(); not used on normal death.
    /// </summary>
    public void ResetToStart()
    {
        transform.SetPositionAndRotation(
            startingPosition,
            startingRotation
        );

        if (rb != null)
        {
            rb.position = (Vector2)startingPosition;
            rb.rotation = startingRotation.eulerAngles.z;

            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;

            rb.simulated = true;
        }

        // Re-enable every collider.
        for (int i = 0; i < pieceColliders.Length; i++)
        {
            pieceColliders[i].enabled = true;
        }

        // Show the piece, its stats text and any ability markers again.
        for (int i = 0; i < pieceRenderers.Length; i++)
        {
            pieceRenderers[i].enabled = true;
        }

        // Restore base HP / attack and clear the dead flag.
        ResetRuntimeStats();

        Debug.Log(
            $"{name} was reset to its starting state."
        );
    }

    public void ResetRuntimeStats()
    {
        currentHealth = baseHealth;
        currentAttackPower = baseAttackPower;
        isDead = false;

        StatsReset?.Invoke();
        HealthChanged?.Invoke();
        StatsChanged?.Invoke();
    }

    private void OnValidate()
    {
        baseHealth = Mathf.Max(1, baseHealth);
        baseAttackPower = Mathf.Max(0, baseAttackPower);

        if (!Application.isPlaying)
        {
            currentHealth = baseHealth;
            currentAttackPower = baseAttackPower;
        }
    }
}