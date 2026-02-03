using System.Collections.Generic;
using UnityEngine;
using RoadArchitect;

public class Fireball : MonoBehaviour
{
    public float heightOffset = 3f;
    public float travelRate = 0.012f;
    public float homingSpeed = 12f;
    public float detectRadius = 12f;
    public float rotationSpeed = 6f;
    public float lifetime = 8f;

    private Road road;
    private float t;

    private Transform ownerKart;
    private Transform currentTarget;
    private bool homingMode = false;

    private bool goingToSpline = true;
    private Vector3 targetSplinePos;
    public float splineSnapSpeed = 20f;


    public float initialForwardSpeed = 18f;
    public float maxForwardSearchTime = 1.2f;
    private bool searchingForSpline = true;
    private float forwardSearchTimer = 0f;

    // Call this immediately when the item is used
    public void Initialize(Transform owner)
    {
        ownerKart = owner;
    }

    void Start()
{
        road = FindFirstObjectByType<Road>();
        Destroy(gameObject, lifetime);

        // spawn at owner
        if (ownerKart != null)
        {
            transform.position = ownerKart.position + ownerKart.forward * 1.2f + Vector3.up * heightOffset;
            transform.rotation = Quaternion.LookRotation(ownerKart.forward, Vector3.up);
        }
}

    void Update()
    {
        // If still searching for a spline, do forward movement with momentum
        if (searchingForSpline)
        {
            ForwardMomentumPhase();
            return;
        }

        // Once spline is found, use your existing movement logic
        if (homingMode)
        {
            HomeTowardsTarget();
            return;
        }

        FollowRoad();
        TryLockOnTarget();
    }

    // ------------------------
    // ROAD FOLLOWING MODE
    // ------------------------
    void FollowRoad()
    {
        if (road == null || road.spline == null) return;

        Vector3 pos, fwd;
        road.spline.GetSplineValueBoth(t, out pos, out fwd);

        pos.y += heightOffset;
        transform.position = pos;

        Quaternion targetRot = Quaternion.LookRotation(fwd, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSpeed);

        t += travelRate * Time.deltaTime;
        if (t > 1f) t = 0f;
    }

    // ------------------------
    // HOMING MODE
    // ------------------------
    void HomeTowardsTarget()
    {
        if (currentTarget == null)
        {
            homingMode = false;
            return;
        }

        Vector3 dir = (currentTarget.position - transform.position).normalized;
        Vector3 move = dir * homingSpeed * Time.deltaTime;

        transform.position += move;

        Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSpeed);
    }

    // ------------------------
    // TARGET DETECTION
    // ------------------------
    void TryLockOnTarget()
    {
        // Get all karts
        KartControllerQL[] aiKarts = FindObjectsOfType<KartControllerQL>();
        KartController[] karts = FindObjectsOfType<KartController>();

        Transform bestTarget = null;
        float bestDist = Mathf.Infinity;

        foreach (var kart in aiKarts)
        {
            Transform tr = kart.transform;

            if (tr == ownerKart) continue; // Ignore the shooter

            float d = Vector3.Distance(transform.position, tr.position);
            if (d < detectRadius && d < bestDist)
            {
                bestDist = d;
                bestTarget = tr;
            }
        }

        foreach (var kart in karts)
        {
            Transform tr = kart.transform;

            if (tr == ownerKart) continue; // Ignore the shooter

            float d = Vector3.Distance(transform.position, tr.position);
            if (d < detectRadius && d < bestDist)
            {
                bestDist = d;
                bestTarget = tr;
            }
        }

        if (bestTarget != null)
        {
            currentTarget = bestTarget;
            homingMode = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Skip if we hit ourselves
        if (other.transform == ownerKart || other.transform.parent == ownerKart)
            return;

        // Try to find the kart - could be the object itself or a parent
        GameObject targetKart = null;

        // Check if the collider itself is a kart
        if (other.CompareTag("Player") || other.CompareTag("Kart"))
        {
            targetKart = other.gameObject;
        }
        // Check if parent is a kart
        else if (other.transform.parent != null)
        {
            Transform parent = other.transform.parent;

            if (parent.CompareTag("Player") || parent.CompareTag("Kart"))
            {
                targetKart = parent.gameObject;
            }
            // Try to find "Kart" child in parent
            else
            {
                Transform kartTransform = parent.Find("Kart");
                if (kartTransform != null)
                {
                    targetKart = kartTransform.gameObject;
                }
            }
        }

        // If we found a kart, try to damage it
        if (targetKart != null)
        {
            // Check for shield first
            ShieldSystem shield = targetKart.GetComponent<ShieldSystem>();
            if (shield != null && shield.IsActive())
            {
                Debug.Log("🛡️ Fireball blocked by shield!");
                shield.OnShieldHit();
                Destroy(gameObject);
                return;
            }

            // No shield - hit the kart
            PowerUpManager powerUp = targetKart.GetComponent<PowerUpManager>();
            if (powerUp != null)
            {
                powerUp.OnHitByFireBall();
                Destroy(gameObject);
            }
            else
            {
                Debug.LogWarning("⚠️ Hit a kart but no PowerUpManager found!");
            }
        }
    }


    void MoveToSplineSmoothly()
    {
        // Rotate smoothly toward the spline direction
        Vector3 dir = (targetSplinePos - transform.position).normalized;
        Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSpeed);

        // Move toward the spline node smoothly
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetSplinePos,
            splineSnapSpeed * Time.deltaTime
        );

        // When close enough → switch to normal mode
        if (Vector3.Distance(transform.position, targetSplinePos) < 0.3f)
        {
            goingToSpline = false;
        }
    }

    bool TryFindForwardSplineNode(out float outT, out Vector3 outPos)
    {
        outT = 0f;
        outPos = Vector3.zero;

        if (road == null || road.spline == null) return false;

        float step = 0.01f;

        // We search forward along spline
        for (float tt = 0; tt < 1f; tt += step)
        {
            Vector3 pos, fwd;
            road.spline.GetSplineValueBoth(tt, out pos, out fwd);
            pos.y += heightOffset;

            Vector3 toPoint = (pos - transform.position).normalized;

            // only accept spline points IN FRONT of fireball
            if (Vector3.Dot(transform.forward, toPoint) > 0.25f) // slightly forward
            {
                outT = tt;
                outPos = pos;
                return true;
            }
        }

        return false;
    }

        void ForwardMomentumPhase()
        {
            // Step 1: Move forward like a projectile
            transform.position += transform.forward * initialForwardSpeed * Time.deltaTime;

            forwardSearchTimer += Time.deltaTime;

            // Step 2: Try to find a spline point IN FRONT
            if (TryFindForwardSplineNode(out t, out targetSplinePos))
            {
                searchingForSpline = false;
                goingToSpline = true;   // existing smooth move-to-spline behavior
                return;
            }

            // Step 3: Timeout failsafe (after X seconds, just start normal behavior)
            if (forwardSearchTimer > maxForwardSearchTime)
            {
                searchingForSpline = false;

                // fallback: nearest spline param
                float nearest = road.spline.GetClosestParam(transform.position);
                Vector3 pos, fwd;
                road.spline.GetSplineValueBoth(nearest, out pos, out fwd);

                targetSplinePos = pos + Vector3.up * heightOffset;
                t = nearest;
                goingToSpline = true;
            }
        }

    }
