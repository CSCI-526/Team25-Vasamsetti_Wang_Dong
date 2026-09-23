using System;
using UnityEngine;

public class PhysicsStopDetector : MonoBehaviour
{
    public static PhysicsStopDetector Instance { get; private set; }

    [Header("Stop Detection")]
    [SerializeField] private float linearSpeedThreshold = 0.08f;
    [SerializeField] private float angularSpeedThreshold = 5f;
    [SerializeField] private float settleDuration = 0.4f;

    public bool AllPiecesStopped { get; private set; } = true;

    public event Action OnAllPiecesStopped;

    private Rigidbody2D[] bodies;
    private float stoppedTimer;

    private void Awake()
    {
        Instance = this;

        RefreshBodies();

        // Allow the first shot immediately when the game starts.
        stoppedTimer = settleDuration;
        AllPiecesStopped = true;
    }

    private void Update()
    {
        if (AreAllBodiesBelowThreshold())
        {
            stoppedTimer += Time.deltaTime;

            if (!AllPiecesStopped && stoppedTimer >= settleDuration)
            {
                AllPiecesStopped = true;
                OnAllPiecesStopped?.Invoke();

                Debug.Log("All pieces have stopped.");
            }
        }
        else
        {
            stoppedTimer = 0f;
            AllPiecesStopped = false;
        }
    }

    private bool AreAllBodiesBelowThreshold()
    {
        foreach (Rigidbody2D body in bodies)
        {
            if (body == null)
            {
                continue;
            }

            if (body.bodyType != RigidbodyType2D.Dynamic)
            {
                continue;
            }

            if (body.linearVelocity.magnitude > linearSpeedThreshold)
            {
                return false;
            }

            if (Mathf.Abs(body.angularVelocity) > angularSpeedThreshold)
            {
                return false;
            }
        }

        return true;
    }

    public void RefreshBodies()
    {
        bodies = FindObjectsByType<Rigidbody2D>(
            FindObjectsSortMode.None
        );
    }

    public void NotifyShotLaunched()
    {
        // Refresh the list in case new pieces were added.
        RefreshBodies();

        // Lock input immediately instead of waiting for physics to update.
        stoppedTimer = 0f;
        AllPiecesStopped = false;

        Debug.Log("Shot launched. Input is now locked.");
    }
}