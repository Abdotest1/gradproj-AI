using System.Collections;
using UnityEngine;

public class SwapAbility : MonoBehaviour
{
    [Header("Arrow Settings")]
    [Tooltip("Drag your arrow prefab or leave empty to create default")]
    public GameObject arrowPrefab;
    [Tooltip("How big the arrow should be")]
    public float arrowScale = 1f;
    [Tooltip("Color of the arrow")]
    public Color arrowColor = Color.yellow;

    [Header("Detection Settings")]
    [Tooltip("Maximum distance to detect karts (like freeze radius)")]
    public float detectionRadius = 40f;
    [Tooltip("How high above the kart should the arrow appear")]
    public float arrowHeight = 3f;
    [Tooltip("Only swap with karts in front (0.3 = roughly forward)")]
    public float forwardDotThreshold = 0.3f;

    [Header("Status (Read Only)")]
    public bool hasSwapAbility = false;
    public bool canSwap = false;

    // Private variables
    private GameObject currentArrow;
    private GameObject targetKart;
    private Transform ownerKart;

    void Start()
    {
        ownerKart = transform;
        CreateArrow();
    }

    void Update()
    {
        // CONTINUOUSLY search for target when we have the ability
        if (hasSwapAbility)
        {
            FindTargetKart();
            UpdateArrowVisual();
        }
        else
        {
            HideArrow();
        }
    }

    /// <summary>
    /// Creates the arrow visual indicator
    /// </summary>
    void CreateArrow()
    {
        if (arrowPrefab != null)
        {
            // Use custom prefab
            currentArrow = Instantiate(arrowPrefab);
            currentArrow.transform.localScale = Vector3.one * arrowScale;
        }
        else
        {
            // Create default arrow (cone pointing down)
            currentArrow = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            currentArrow.name = "Swap Arrow Indicator";

            // Remove collider
            Destroy(currentArrow.GetComponent<Collider>());

            // Set rotation to point down
            currentArrow.transform.localScale = new Vector3(0.5f, 1f, 0.5f) * arrowScale;

            // Create material
            Material arrowMat = new Material(Shader.Find("Standard"));
            arrowMat.color = arrowColor;
            arrowMat.SetFloat("_Mode", 3); // Transparent
            arrowMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            arrowMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            arrowMat.SetInt("_ZWrite", 0);
            arrowMat.EnableKeyword("_ALPHABLEND_ON");
            arrowMat.renderQueue = 3000;

            currentArrow.GetComponent<MeshRenderer>().material = arrowMat;
        }

        currentArrow.SetActive(false);
    }

    /// <summary>
    /// Finds the closest enemy IN FRONT - SAME LOGIC AS FREEZE!
    /// </summary>
    void FindTargetKart()
    {
        // Use the EXACT same logic as PowerUpManager.FindClosestEnemy()
        KartControllerQL[] allAI = FindObjectsByType<KartControllerQL>(FindObjectsSortMode.None);

        GameObject closest = null;
        float closestDist = detectionRadius * detectionRadius; // Use squared distance for performance

        foreach (var ai in allAI)
        {
            // Skip ourselves
            if (ai.gameObject == gameObject) continue;

            Vector3 directionToTarget = ai.transform.position - ownerKart.position;
            float dist = directionToTarget.sqrMagnitude;

            // Check if target is IN FRONT (not behind)
            float dotProduct = Vector3.Dot(ownerKart.forward, directionToTarget.normalized);

            // Only consider targets in front and within range
            if (dotProduct > forwardDotThreshold && dist < closestDist)
            {
                closestDist = dist;
                closest = ai.gameObject;
            }
        }

        targetKart = closest;
        canSwap = (targetKart != null);
    }

    /// <summary>
    /// Updates the arrow position and visibility
    /// </summary>
    void UpdateArrowVisual()
    {
        if (targetKart != null && currentArrow != null)
        {
            currentArrow.SetActive(true);

            // Position above target kart
            Vector3 arrowPosition = targetKart.transform.position + Vector3.up * arrowHeight;
            currentArrow.transform.position = arrowPosition;

            // Point down
            currentArrow.transform.rotation = Quaternion.Euler(180, 0, 0);

            // Add bouncing animation
            float bounce = Mathf.Sin(Time.time * 4f) * 0.3f;
            currentArrow.transform.position += Vector3.up * bounce;

            // Optional: Add rotation
            currentArrow.transform.Rotate(0, Time.deltaTime * 90f, 0, Space.World);
        }
        else
        {
            HideArrow();
        }
    }

    /// <summary>
    /// Hides the arrow indicator
    /// </summary>
    void HideArrow()
    {
        if (currentArrow != null)
            currentArrow.SetActive(false);

        canSwap = false;
    }

    /// <summary>
    /// Performs the swap with the target kart
    /// </summary>
    public void PerformSwap()
    {
        if (!canSwap || targetKart == null)
        {
            Debug.LogWarning("⚠️ Cannot swap - no valid target!");
            return;
        }

        Debug.Log($"🔄 Swapping with: {targetKart.name}");

        Transform targetTransform = targetKart.transform;

        // Store positions and rotations
        Vector3 myPosition = ownerKart.position;
        Quaternion myRotation = ownerKart.rotation;

        Vector3 targetPosition = targetTransform.position;
        Quaternion targetRotation = targetTransform.rotation;

        // Get rigidbodies (handle sphere-based physics)
        Rigidbody myRb = GetKartRigidbody(ownerKart);
        Rigidbody targetRb = GetKartRigidbody(targetTransform);

        Vector3 myVelocity = myRb != null ? myRb.linearVelocity : Vector3.zero;
        Vector3 targetVelocity = targetRb != null ? targetRb.linearVelocity : Vector3.zero;

        // PERFORM THE SWAP
        ownerKart.position = targetPosition;
        ownerKart.rotation = targetRotation;

        targetTransform.position = myPosition;
        targetTransform.rotation = myRotation;

        // Swap velocities
        if (myRb != null) myRb.linearVelocity = targetVelocity;
        if (targetRb != null) targetRb.linearVelocity = myVelocity;

        // Flash effect
        StartCoroutine(SwapFlashEffect());

        // Use up the ability
        hasSwapAbility = false;
        targetKart = null;
        HideArrow();

        Debug.Log("✅ Swap complete!");
    }

    /// <summary>
    /// Helper to find the correct rigidbody (handles different kart types)
    /// </summary>
    Rigidbody GetKartRigidbody(Transform kart)
    {
        // Try direct rigidbody
        Rigidbody rb = kart.GetComponent<Rigidbody>();
        if (rb != null) return rb;

        // Try KartController's sphere
        KartController kartCtrl = kart.GetComponent<KartController>();
        if (kartCtrl != null && kartCtrl.sphere != null)
            return kartCtrl.sphere;
        /*
        // Try AIKartController's sphere
        AIKartController aiCtrl = kart.GetComponent<AIKartController>();
        if (aiCtrl != null && aiCtrl.sphere != null)
            return aiCtrl.sphere;
        */
        // Try KartControllerQL's sphere (YOUR AI KARTS!)
        KartControllerQL qlCtrl = kart.GetComponent<KartControllerQL>();
        if (qlCtrl != null && qlCtrl.sphere != null)
            return qlCtrl.sphere;

        // Fallback: search children
        return kart.GetComponentInChildren<Rigidbody>();
    }

    /// <summary>
    /// Visual feedback when swap happens
    /// </summary>
    IEnumerator SwapFlashEffect()
    {
        Debug.Log("✨ SWAP COMPLETE!");

        // Optional: Add particle effects, sound, etc. here

        yield return null;
    }

    /// <summary>
    /// Call this when player picks up the swap item
    /// </summary>
    public void ActivateSwapAbility()
    {
        hasSwapAbility = true;
        Debug.Log("🔄 Swap ability activated! Arrow will appear on target.");
    }

    /// <summary>
    /// Check if we have the ability
    /// </summary>
    public bool HasSwapAbility()
    {
        return hasSwapAbility;
    }

    // Debug visualization in Scene view
    void OnDrawGizmosSelected()
    {
        if (targetKart != null && ownerKart != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(ownerKart.position, targetKart.transform.position);
            Gizmos.DrawWireSphere(targetKart.transform.position, 1f);
        }

        // Draw detection radius
        if (ownerKart != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(ownerKart.position, Mathf.Sqrt(detectionRadius));
        }
    }
}