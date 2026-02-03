using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Checkpoints : MonoBehaviour
{
    public List<Checkpoint> checkPoints;

    private void Awake()
    {
        checkPoints = new List<Checkpoint>(GetComponentsInChildren<Checkpoint>());

        // Auto-assign checkpoint indices based on list order
        for (int i = 0; i < checkPoints.Count; i++)
        {
            if (checkPoints[i] != null)
            {
                checkPoints[i].checkpointIndex = i;
            }
        }
    }
}