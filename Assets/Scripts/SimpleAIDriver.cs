using UnityEngine;
using RoadArchitect;

public class SimpleAIDriver : MonoBehaviour
{
    [Header("RoadArchitect AI Settings")]
    public Road road;                 // Drag your Road1 object here
    public int startNodeIndex = 11;   // which node to start from (1‑based index as in your Hierarchy)
    public float travelRate = 0.01f;  // speed along spline (0.005–0.02 is typical)
    public float heightOffset = 0.3f; // lift the kart above the road
    public float turnSmooth = 5f;     // rotation smoothing
    private float t;                  // current position along the spline (0–1)

    void Start()
    {
        if (road == null || road.spline == null)
        {
            Debug.LogWarning($"{name}: No road assigned");
            return;
        }

        // Clamp the index (there might be fewer nodes)
        int nodeCount = Mathf.Max(road.spline.nodes.Count, 1);
        int idx = Mathf.Clamp(startNodeIndex - 1, 0, nodeCount - 1);

        // Each SplineN (node) has a 'time' parameter representing its location along the spline (0–1)
        t = road.spline.nodes[idx].time;

        // Immediately place the AI at that location
        Vector3 pos, fwd;
        road.spline.GetSplineValueBoth(t, out pos, out fwd);
        pos.y += heightOffset;
        transform.position = pos;
        transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);
    }

    void Update()
    {
        if (road == null || road.spline == null) return;

        Vector3 pos, fwd;
        road.spline.GetSplineValueBoth(t, out pos, out fwd);
        pos.y += heightOffset;

        transform.position = pos;
        Quaternion targetRot = Quaternion.LookRotation(fwd, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * turnSmooth);

        t += travelRate * Time.deltaTime;
        if (t > 1f) t = 0f; // loop back to start when reaching the end
    }
}