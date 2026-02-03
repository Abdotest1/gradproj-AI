using UnityEngine;

public class RaceProgress : MonoBehaviour
{
    [HideInInspector] public int currentLap = 0;
    [HideInInspector] public int lastCheckpointIndex = -1;
    [HideInInspector] public float distanceToNextCheckpoint = float.MaxValue;
    [HideInInspector] public float distanceFromLastCheckpoint = 0f;
    [HideInInspector] public bool hasStarted = false;
    [HideInInspector] public bool hasFinished = false;

    private Transform kartTransform;
    private Checkpoints checkpointsManager;
    private int totalCheckpoints;
    private int totalLaps = 3;

    public float TotalProgress
    {
        get
        {
            if (totalCheckpoints <= 0)
            {
                if (checkpointsManager == null)
                {
                    checkpointsManager = FindObjectOfType<Checkpoints>();
                }
                if (checkpointsManager != null)
                {
                    totalCheckpoints = checkpointsManager.checkPoints.Count;
                }
            }

            if (totalCheckpoints <= 0)
            {
                return currentLap * 1000f + lastCheckpointIndex;
            }

            float lapProgress = currentLap * totalCheckpoints;
            float checkpointProgress = Mathf.Max(0, lastCheckpointIndex);
            float betweenCheckpointProgress = GetProgressBetweenCheckpoints();

            return lapProgress + checkpointProgress + betweenCheckpointProgress;
        }
    }

    void Start()
    {
        kartTransform = transform;

        checkpointsManager = FindObjectOfType<Checkpoints>();
        if (checkpointsManager != null)
        {
            totalCheckpoints = checkpointsManager.checkPoints.Count;
        }

        // Get total laps from RaceRankingManager if available
        RaceRankingManager rankingManager = FindObjectOfType<RaceRankingManager>();
        if (rankingManager != null)
        {
            totalLaps = rankingManager.totalLaps;
        }
    }

    void Update()
    {
        UpdateDistances();
    }

    private void UpdateDistances()
    {
        if (checkpointsManager == null || checkpointsManager.checkPoints.Count == 0)
            return;

        int currentCheckpointIndex = Mathf.Max(0, lastCheckpointIndex);
        int nextCheckpointIndex = lastCheckpointIndex + 1;

        if (nextCheckpointIndex >= totalCheckpoints)
        {
            nextCheckpointIndex = 0;
        }

        if (!hasStarted)
        {
            currentCheckpointIndex = 0;
            nextCheckpointIndex = 0;
        }

        Transform currentCheckpoint = checkpointsManager.checkPoints[currentCheckpointIndex].transform;
        Transform nextCheckpoint = checkpointsManager.checkPoints[nextCheckpointIndex].transform;

        distanceFromLastCheckpoint = Vector3.Distance(kartTransform.position, currentCheckpoint.position);
        distanceToNextCheckpoint = Vector3.Distance(kartTransform.position, nextCheckpoint.position);
    }

    private float GetProgressBetweenCheckpoints()
    {
        if (checkpointsManager == null || !hasStarted)
            return 0f;

        if (totalCheckpoints <= 0)
            return 0f;

        int currentCheckpointIndex = Mathf.Max(0, lastCheckpointIndex);
        int nextCheckpointIndex = lastCheckpointIndex + 1;

        if (nextCheckpointIndex >= totalCheckpoints)
        {
            nextCheckpointIndex = 0;
        }

        Transform currentCheckpoint = checkpointsManager.checkPoints[currentCheckpointIndex].transform;
        Transform nextCheckpoint = checkpointsManager.checkPoints[nextCheckpointIndex].transform;

        float totalDistance = Vector3.Distance(currentCheckpoint.position, nextCheckpoint.position);

        if (totalDistance <= 0.01f)
            return 0f;

        float progress = distanceFromLastCheckpoint / (distanceFromLastCheckpoint + distanceToNextCheckpoint);

        return Mathf.Clamp01(progress);
    }

    public void OnCheckpointReached(int checkpointIndex)
    {
        if (hasFinished)
            return;

        if (!hasStarted && checkpointIndex == 0)
        {
            hasStarted = true;
            lastCheckpointIndex = 0;
            return;
        }

        if (!hasStarted)
            return;

        int expectedNextCheckpoint = lastCheckpointIndex + 1;

        // Completed a lap
        if (checkpointIndex == 0 && lastCheckpointIndex == totalCheckpoints - 1)
        {
            currentLap++;
            lastCheckpointIndex = 0;

            // Check if finished the race
            if (currentLap >= totalLaps)
            {
                hasFinished = true;
            }
            return;
        }

        // Normal forward progression
        if (checkpointIndex == expectedNextCheckpoint)
        {
            lastCheckpointIndex = checkpointIndex;
            return;
        }

        // Skipped a few checkpoints forward
        if (checkpointIndex > lastCheckpointIndex && checkpointIndex <= lastCheckpointIndex + 5)
        {
            lastCheckpointIndex = checkpointIndex;
            return;
        }

        // Going backwards
        if (checkpointIndex < lastCheckpointIndex)
        {
            // Went back across finish line
            if (lastCheckpointIndex <= 2 && checkpointIndex >= totalCheckpoints - 3)
            {
                if (currentLap > 0)
                {
                    currentLap--;
                    lastCheckpointIndex = checkpointIndex;
                }
                return;
            }

            lastCheckpointIndex = checkpointIndex;
            return;
        }
    }

    public void ResetProgress()
    {
        currentLap = 0;
        lastCheckpointIndex = -1;
        distanceToNextCheckpoint = float.MaxValue;
        distanceFromLastCheckpoint = 0f;
        hasStarted = false;
        hasFinished = false;
    }
}