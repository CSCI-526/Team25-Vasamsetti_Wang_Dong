using System;
using System.Collections;
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

    [Header("Runtime Stats - Do Not Edit")]
    [SerializeField]
    private int currentHealth;

    [SerializeField]
    private int currentAttackPower;

    [SerializeField]
    private bool isDead;

    private PieceLauncher launcher;
    private Rigidbody2D rb;
    private PhysicsStopDetector stopDetector;

    private Vector3 startingPosition;
    private Quaternion startingRotation;

    private Collider2D[] pieceColliders;
    private Renderer[] pieceRenderers;

    private bool[] previousColliderStates;
    private bool[] previousRendererStates;
    private bool previousSimulatedState;

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

    // 参数是击杀者
    public event Action<PieceStats> Died;

    private void Awake()
    {
        launcher = GetComponent<PieceLauncher>();
        rb = GetComponent<Rigidbody2D>();

        stopDetector =
            FindFirstObjectByType<PhysicsStopDetector>();

        startingPosition = transform.position;
        startingRotation = transform.rotation;

        pieceColliders =
            GetComponentsInChildren<Collider2D>(true);

        pieceRenderers =
            GetComponentsInChildren<Renderer>(true);

        previousColliderStates =
            new bool[pieceColliders.Length];

        previousRendererStates =
            new bool[pieceRenderers.Length];

        ResetRuntimeStats();
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

        // 立即通知计分和技能系统
        Died?.Invoke(attacker);

        // 关闭死亡棋子的物理和画面
        EnterDefeatedState();

        // 等本次行动完全结束后再复活
        StartCoroutine(WaitForActionEndAndRespawn());
    }

    private void EnterDefeatedState()
    {
        // 保存并关闭所有碰撞体
        for (int i = 0; i < pieceColliders.Length; i++)
        {
            previousColliderStates[i] =
                pieceColliders[i].enabled;

            pieceColliders[i].enabled = false;
        }

        // 保存并隐藏棋子、属性文字和技能标记
        for (int i = 0; i < pieceRenderers.Length; i++)
        {
            previousRendererStates[i] =
                pieceRenderers[i].enabled;

            pieceRenderers[i].enabled = false;
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;

            previousSimulatedState = rb.simulated;
            rb.simulated = false;
        }
    }

    private IEnumerator WaitForActionEndAndRespawn()
    {
        // 先等待一帧，让当前碰撞结算彻底结束
        yield return null;

        if (stopDetector == null)
        {
            stopDetector =
                FindFirstObjectByType<PhysicsStopDetector>();
        }

        // 等场上的所有棋子停止
        if (stopDetector != null)
        {
            while (!stopDetector.AllPiecesStopped)
            {
                yield return null;
            }
        }

        Respawn();
    }

    private void Respawn()
    {
        // Rigidbody2D 仍然关闭时，先移动回出生点
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

            rb.simulated = previousSimulatedState;
        }

        // 恢复碰撞体原来的状态
        for (int i = 0; i < pieceColliders.Length; i++)
        {
            pieceColliders[i].enabled =
                previousColliderStates[i];
        }

        // 恢复棋子、文字和技能标记原来的显示状态
        for (int i = 0; i < pieceRenderers.Length; i++)
        {
            pieceRenderers[i].enabled =
                previousRendererStates[i];
        }

        // 恢复基础生命值和基础攻击力
        ResetRuntimeStats();

        Debug.Log(
            $"{name} respawned after the action ended."
        );
    }

    public void ResetRuntimeStats()
    {
        currentHealth = baseHealth;
        currentAttackPower = baseAttackPower;
        isDead = false;

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