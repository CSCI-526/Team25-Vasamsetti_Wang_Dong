using UnityEngine;

/// <summary>
/// Stat configuration for the three piece types (Heavy / Striker / Speed).
/// Stored as a ScriptableObject so values can be tuned directly in the Inspector.
/// PieceStats reads this in Awake to override HP / attack / mass / launch force.
/// </summary>
[CreateAssetMenu(
    fileName = "PieceTypeDefinition",
    menuName = "Sling Clash/Piece Type Definition"
)]
public class PieceTypeDefinition : ScriptableObject
{
    [SerializeField]
    private PieceType type = PieceType.Striker;

    [Min(1)]
    [SerializeField]
    private int maxHP = 3;

    [Min(0)]
    [SerializeField]
    private int attack = 2;

    [Tooltip("Rigidbody2D mass. Heavier pieces are harder to push around.")]
    [Min(0.01f)]
    [SerializeField]
    private float mass = 1f;

    [Tooltip("Launch force multiplier. 1 is standard, >1 faster, <1 slower.")]
    [Min(0.01f)]
    [SerializeField]
    private float launchForceMultiplier = 1f;

    public PieceType Type => type;
    public int MaxHP => maxHP;
    public int Attack => attack;
    public float Mass => mass;
    public float LaunchForceMultiplier => launchForceMultiplier;
}
