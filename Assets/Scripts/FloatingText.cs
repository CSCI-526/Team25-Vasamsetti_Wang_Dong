using TMPro;
using UnityEngine;

/// <summary>
/// Short-lived world-space text (e.g. "+1 HP") that floats up and fades out.
/// </summary>
public class FloatingText : MonoBehaviour
{
    private const float Duration = 1f;
    private const float RiseDistance = 0.8f;

    private TextMeshPro text;
    private Vector3 startPosition;
    private Color startColor;
    private float elapsed;

    public static void Spawn(Vector3 position, string message, Color color)
    {
        GameObject go = new GameObject("FloatingText");
        go.transform.position = position;

        TextMeshPro tmp = go.AddComponent<TextMeshPro>();
        tmp.text = message;
        tmp.fontSize = 5f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        tmp.sortingOrder = 20;
        tmp.rectTransform.sizeDelta = new Vector2(3f, 1f);

        FloatingText floating = go.AddComponent<FloatingText>();
        floating.text = tmp;
        floating.startPosition = position;
        floating.startColor = color;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float t = elapsed / Duration;

        transform.position = startPosition + Vector3.up * (RiseDistance * t);

        Color color = startColor;
        color.a = 1f - t;
        text.color = color;

        if (t >= 1f)
        {
            Destroy(gameObject);
        }
    }
}
