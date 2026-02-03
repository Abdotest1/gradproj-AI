using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RaceManager : MonoBehaviour
{
    public List<CheckpointManager> allRacers;

    void Update()
    {
        UpdatePositions();
    }

    void UpdatePositions()
    {
        // Sort by total progress
        List<CheckpointManager> sorted = allRacers
            .OrderByDescending(r => r.TotalCheckpointsPassed)
            .ToList();

        // Assign positions
        for (int i = 0; i < sorted.Count; i++)
        {
            sorted[i].position = i + 1;
        }
    }
}