using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(LineRenderer))]
public class PieceLauncher : MonoBehaviour
{
    [Header("Launch Settings")]
    [SerializeField] private float launchForce = 4f;
    [SerializeField] private float maxDragDistance = 3f;
    [SerializeField] private float minimumDragDistance = 0.1f;

    [Header("Selection Settings")]
    [SerializeField] private float selectableSpeed = 0.1f;

    [Header("Aim Line Settings")]
    [SerializeField] private float lineStartWidth = 0.1f;
    [SerializeField] private float lineEndWidth = 0.04f;
    [SerializeField] private Color lineStartColor = Color.white;
    [SerializeField] private Color lineEndColor = Color.cyan;

    private Rigidbody2D rb;
    private Camera mainCamera;
    private LineRenderer aimLine;

    private Vector2 dragStartPosition;
    private bool isDragging;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCamera = Camera.main;
        aimLine = GetComponent<LineRenderer>();

        ConfigureAimLine();
    }

    private void ConfigureAimLine()
    {
        aimLine.positionCount = 2;
        aimLine.useWorldSpace = true;

        aimLine.startWidth = lineStartWidth;
        aimLine.endWidth = lineEndWidth;

        aimLine.startColor = lineStartColor;
        aimLine.endColor = lineEndColor;

        aimLine.numCapVertices = 6;
        aimLine.sortingOrder = 10;

        aimLine.enabled = false;
    }

    private void OnMouseDown()
    {
        // Do not select this piece while it is still moving.
        if (rb.linearVelocity.magnitude > selectableSpeed)
        {
            return;
        }

        isDragging = true;
        dragStartPosition = GetMouseWorldPosition();

        aimLine.enabled = true;
        UpdateAimLine();
    }

    private void OnMouseDrag()
    {
        if (!isDragging)
        {
            return;
        }

        UpdateAimLine();
    }

    private void OnMouseUp()
    {
        if (!isDragging)
        {
            return;
        }

        Vector2 currentMousePosition = GetMouseWorldPosition();
        Vector2 launchVector = CalculateLaunchVector(currentMousePosition);

        isDragging = false;
        aimLine.enabled = false;

        if (launchVector.magnitude >= minimumDragDistance)
        {
            rb.AddForce(
                launchVector * launchForce,
                ForceMode2D.Impulse
            );
        }
    }

    private void UpdateAimLine()
    {
        Vector2 currentMousePosition = GetMouseWorldPosition();
        Vector2 launchVector = CalculateLaunchVector(currentMousePosition);

        Vector3 lineStart = transform.position;
        Vector3 lineEnd = (Vector2)transform.position + launchVector;

        aimLine.SetPosition(0, lineStart);
        aimLine.SetPosition(1, lineEnd);
    }

    private Vector2 CalculateLaunchVector(Vector2 currentMousePosition)
    {
        Vector2 launchVector = dragStartPosition - currentMousePosition;

        return Vector2.ClampMagnitude(
            launchVector,
            maxDragDistance
        );
    }

    private Vector2 GetMouseWorldPosition()
    {
        Vector3 mouseScreenPosition = Input.mousePosition;
        mouseScreenPosition.z = -mainCamera.transform.position.z;

        return mainCamera.ScreenToWorldPoint(mouseScreenPosition);
    }

    private void OnDisable()
    {
        if (aimLine != null)
        {
            aimLine.enabled = false;
        }

        isDragging = false;
    }
}