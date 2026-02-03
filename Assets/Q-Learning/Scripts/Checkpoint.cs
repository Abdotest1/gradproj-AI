using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [HideInInspector] public int checkpointIndex;
    public int checkpointNumber;
    public bool isFinishLine = false;

    private void OnTriggerEnter(Collider other)
    {
        // Handle AI training checkpoint system
        CheckpointManager checkpointManager = other.GetComponent<CheckpointManager>();
        if (checkpointManager != null)
        {
            checkpointManager.CheckPointReached(this);
        }

        // Handle race ranking system
        RaceProgress raceProgress = other.GetComponentInParent<RaceProgress>();
        if (raceProgress == null)
        {
            raceProgress = other.GetComponent<RaceProgress>();
        }

        if (raceProgress != null)
        {
            raceProgress.OnCheckpointReached(checkpointIndex);
        }
    }
}