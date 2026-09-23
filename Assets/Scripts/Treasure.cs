using TMPro;
using UnityEngine;

/// <summary>
/// A single treasure on the board. Pieces pass through it (trigger collider);
/// the first living piece to touch it collects it via TreasureManager.
/// Created at runtime by TreasureManager.
/// </summary>
public class Treasure : MonoBehaviour
{
    private const int SortingOrder = -1; // above the background, under the pieces

    private TreasureManager owner;
    private Transform visual;
    private Vector3 visualBaseScale;
    private SpriteRenderer spriteRenderer;
    private TextMeshPro label;
    private Color baseColor;
    private bool lastTurn;
    private bool collected;

    public void Init(
        TreasureManager manager,
        float radius,
        Color color,
        SpriteRenderer pieceRenderer
    )
    {
        owner = manager;
        baseColor = color;

        CircleCollider2D trigger = gameObject.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = radius;

        // Sprite lives on a child so the pulse animation does not resize the trigger.
        GameObject visualObject = new GameObject("Visual");
        visual = visualObject.transform;
        visual.SetParent(transform, false);

        spriteRenderer = visualObject.AddComponent<SpriteRenderer>();
        spriteRenderer.color = color;
        spriteRenderer.sortingOrder = SortingOrder;

        if (pieceRenderer != null)
        {
            spriteRenderer.sprite = pieceRenderer.sprite;
            spriteRenderer.sharedMaterial = pieceRenderer.sharedMaterial;
            spriteRenderer.sortingLayerID = pieceRenderer.sortingLayerID;
        }

        float spriteWidth = spriteRenderer.sprite != null
            ? spriteRenderer.sprite.bounds.size.x
            : 1f;

        visualBaseScale = Vector3.one * (radius * 2f / spriteWidth);
        visual.localScale = visualBaseScale;

        // "+" sign so it reads as healing, not just a gold dot.
        GameObject labelObject = new GameObject("Label");
        labelObject.transform.SetParent(transform, false);

        label = labelObject.AddComponent<TextMeshPro>();
        label.text = "+";
        label.fontSize = radius * 16f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(0.35f, 0.2f, 0f);
        label.sortingOrder = SortingOrder + 1;
        label.rectTransform.sizeDelta = new Vector2(radius * 2f, radius * 2f);
    }

    // On its final turn the treasure blinks to warn it is about to vanish.
    public void SetLastTurn(bool value)
    {
        lastTurn = value;
    }

    private void Update()
    {
        if (visual == null)
        {
            return;
        }

        float pulse = 1f + 0.08f * Mathf.Sin(Time.time * 4f);
        visual.localScale = visualBaseScale * pulse;

        Color color = baseColor;
        if (lastTurn)
        {
            color.a = 0.45f + 0.55f * Mathf.Abs(Mathf.Sin(Time.time * 5f));
        }

        spriteRenderer.color = color;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected || owner == null)
        {
            return;
        }

        PieceStats piece = other.GetComponentInParent<PieceStats>();

        if (piece == null || piece.IsDead)
        {
            return;
        }

        collected = true;
        owner.Collect(this, piece);
    }
}
