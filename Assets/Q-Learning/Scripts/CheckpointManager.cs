using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    [Header("Timer Settings")]
    public float MaxTimeToReachNextCheckpoint = 30f;
    public float TimeLeft = 30f;

    [Header("References")]
    public KartAgent kartAgent; // Can be empty for Player!
    public Checkpoint nextCheckPointToReach;

    [Header("Race Progress")]
    public int currentLap = 0;
    public int totalLaps = 3;
    public int position = 1;

    private int CurrentCheckpointIndex;
    private List<Checkpoint> Checkpoints;
    public Checkpoint lastCheckpoint;

    public event Action<Checkpoint> reachedCheckpoint;

    public int TotalCheckpointsPassed
    {
        get { return (currentLap * Checkpoints.Count) + CurrentCheckpointIndex; }
    }

    void Start()
    {
        Checkpoints = FindObjectOfType<Checkpoints>().checkPoints;
        ResetCheckpoints();
    }

    public void ResetCheckpoints()
    {
        CurrentCheckpointIndex = 0;
        currentLap = 0;
        TimeLeft = MaxTimeToReachNextCheckpoint;
        lastCheckpoint = Checkpoints[Checkpoints.Count - 1];

        SetNextCheckpoint();
    }

    private void Update()
    {
        // Only use timer for AI
        if (kartAgent != null)
        {
            TimeLeft -= Time.deltaTime;

            if (TimeLeft < 0f)
            {
                kartAgent.AddReward(-1f);
                kartAgent.EndEpisode();
            }
        }
    }

    public void CheckPointReached(Checkpoint checkpoint)
    {
        if (nextCheckPointToReach != checkpoint) return;

        lastCheckpoint = Checkpoints[CurrentCheckpointIndex];
        reachedCheckpoint?.Invoke(checkpoint);
        CurrentCheckpointIndex++;

        if (CurrentCheckpointIndex >= Checkpoints.Count)
        {
            currentLap++;
            CurrentCheckpointIndex = 0;

            Debug.Log(gameObject.name + " finished lap " + currentLap);

            if (currentLap >= totalLaps)
            {
                Debug.Log(gameObject.name + " FINISHED THE RACE!");

                if (kartAgent != null)
                {
                    kartAgent.AddReward(0.5f);
                    kartAgent.EndEpisode();
                }
            }
            else
            {
                if (kartAgent != null)
                {
                    kartAgent.AddReward(0.3f);
                }
                SetNextCheckpoint();
            }
        }
        else
        {
            if (kartAgent != null)
            {
                kartAgent.AddReward(0.03f);
            }
            SetNextCheckpoint();
        }
    }

    private void SetNextCheckpoint()
    {
        if (Checkpoints.Count > 0)
        {
            TimeLeft = MaxTimeToReachNextCheckpoint;
            nextCheckPointToReach = Checkpoints[CurrentCheckpointIndex];
        }
    }
}