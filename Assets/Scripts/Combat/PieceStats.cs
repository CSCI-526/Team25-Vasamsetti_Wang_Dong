using System;
using UnityEngine;

[RequireComponent(typeof(PieceLauncher))]
public class PieceStats : MonoBehaviour
{
    [Header("Basic Combat Stats")]
    [SerializeField] private int maxHealth = 6;
    [SerializeField] private int attackPower = 2;

    [Header("Runtime Information")]
    [SerializeField] private int currentHealth;
    [SerializeField] private bool isDead;

    private PieceLauncher launcher;

    public PieceTeamSide Team => launcher.Team;

    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public int AttackPower => attackPower;
    public bool IsDead => isDead;

    public event Action<PieceStats, int, int> HealthChanged;
    public event Action<PieceStats> StatsChanged;
    public event Action<PieceStats, PieceStats> Died;

    private void Awake()
    {
        launcher = GetComponent<PieceLauncher>();

        currentHealth = maxHealth;
        isDead = false;
    }

    public int TakeDamage(
        int damage,
        PieceStats attacker
    )
    {
        if (isDead)
        {
            return 0;
        }

        int finalDamage = Mathf.Max(1, damage);

        currentHealth = Mathf.Max(
            0,
            currentHealth - finalDamage
        );

        Debug.Log(
            gameObject.name +
            " took " +
            finalDamage +
            " damage from " +
            attacker.gameObject.name +
            ". HP: " +
            currentHealth +
            "/" +
            maxHealth
        );

        HealthChanged?.Invoke(
            this,
            currentHealth,
            maxHealth
        );
        StatsChanged?.Invoke(this);

        if (currentHealth <= 0)
        {
            Die(attacker);
        }

        return finalDamage;
    }

    public int Heal(int amount)
    {
        if (isDead || amount <= 0)
        {
            return 0;
        }

        int healthBeforeHealing = currentHealth;

        currentHealth = Mathf.Min(
            maxHealth,
            currentHealth + amount
        );

        int actualHealing =
            currentHealth - healthBeforeHealing;

        if (actualHealing > 0)
        {
            Debug.Log(
                gameObject.name +
                " healed " +
                actualHealing +
                " HP. HP: " +
                currentHealth +
                "/" +
                maxHealth
            );

            HealthChanged?.Invoke(
                this,
                currentHealth,
                maxHealth
            );
            StatsChanged?.Invoke(this);
        }

        return actualHealing;
    }

    public void AddAttackPower(int amount)
    {
        if (isDead || amount <= 0)
        {
            return;
        }

        attackPower += amount;

        Debug.Log(
            gameObject.name +
            " gained " +
            amount +
            " attack. Current attack: " +
            attackPower
        );
        StatsChanged?.Invoke(this);
    }

    private void Die(PieceStats attacker)
    {
        if (isDead)
        {
            return;
        }

        isDead = true;

        Debug.Log(
            gameObject.name +
            " was eliminated by " +
            attacker.gameObject.name
        );

        Died?.Invoke(this, attacker);

        Destroy(gameObject);
    }

    private void OnValidate()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        attackPower = Mathf.Max(1, attackPower);

        if (!Application.isPlaying)
        {
            currentHealth = maxHealth;
            isDead = false;
        }
    }
}