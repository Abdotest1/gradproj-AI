using System.Collections;
using UnityEngine;

public class ShieldSystem : MonoBehaviour
{
    [Header("Shield Settings")]
    [Tooltip("How long the shield lasts (seconds)")]
    public float shieldDuration = 5f;

    [Tooltip("Drag your Force Field material here")]
    public Material shieldMaterial;

    [Tooltip("How big the shield bubble is")]
    public float shieldSize = 2.5f;

    [Tooltip("Shield color (RGBA)")]
    public Color shieldColor = new Color(0f, 0.7f, 1f, 0.3f);

    [Header("Optional: Custom Shield Prefab")]
    [Tooltip("Leave empty to use default sphere, or drag a custom shield prefab")]
    public GameObject customShieldPrefab;

    [Header("Status (Read Only)")]
    [Tooltip("Is the shield currently active?")]
    public bool isShieldActive = false;

    // Private variables
    private GameObject activeShieldObject;
    private Coroutine shieldCoroutine;
    private float remainingShieldTime;

    /// <summary>
    /// Activates the shield. If already active, extends the duration.
    /// </summary>
    public void ActivateShield()
    {
        // If shield is already running, just add more time
        if (shieldCoroutine != null)
        {
            remainingShieldTime += shieldDuration;
            Debug.Log("🛡️ Shield time extended! New time: " + remainingShieldTime + "s");
            return;
        }

        // Start new shield
        shieldCoroutine = StartCoroutine(RunShield());
    }

    /// <summary>
    /// Deactivates the shield immediately
    /// </summary>
    public void DeactivateShield()
    {
        if (shieldCoroutine != null)
        {
            StopCoroutine(shieldCoroutine);
            shieldCoroutine = null;
        }

        if (activeShieldObject != null)
        {
            Destroy(activeShieldObject);
            activeShieldObject = null;
        }

        isShieldActive = false;
        Debug.Log("🛡️ Shield deactivated");
    }

    /// <summary>
    /// Main shield coroutine - handles creation, timing, and destruction
    /// </summary>
    private IEnumerator RunShield()
    {
        // Create the visual shield
        CreateShieldVisual();

        isShieldActive = true;
        remainingShieldTime = shieldDuration;

        Debug.Log("🛡️ Shield activated! Duration: " + shieldDuration + "s");

        // Count down
        while (remainingShieldTime > 0)
        {
            remainingShieldTime -= Time.deltaTime;

            // Optional: Make shield blink when almost expired
            if (remainingShieldTime < 1f && activeShieldObject != null)
            {
                bool visible = Mathf.Sin(Time.time * 10f) > 0;
                activeShieldObject.SetActive(visible);
            }

            yield return null;
        }

        // Time's up - remove shield
        DeactivateShield();
    }

    /// <summary>
    /// Creates the visual shield bubble
    /// </summary>
    private void CreateShieldVisual()
    {
        if (customShieldPrefab != null)
        {
            activeShieldObject = Instantiate(customShieldPrefab, transform);
            activeShieldObject.transform.localPosition = Vector3.zero;
            activeShieldObject.transform.localScale = Vector3.one * shieldSize;
        }
        else
        {
            activeShieldObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            activeShieldObject.name = "Shield Bubble";
            activeShieldObject.transform.SetParent(transform);
            activeShieldObject.transform.localPosition = Vector3.zero;
            activeShieldObject.transform.localScale = Vector3.one * shieldSize;

            Destroy(activeShieldObject.GetComponent<Collider>());

            // Use Force Field material
            if (shieldMaterial != null)
            {
                activeShieldObject.GetComponent<MeshRenderer>().material = shieldMaterial;
            }
            else
            {
                Debug.LogWarning("⚠️ No shield material assigned! Using default.");
                Material fallback = new Material(Shader.Find("Standard"));
                fallback.color = shieldColor;
                activeShieldObject.GetComponent<MeshRenderer>().material = fallback;
            }
        }
    }

    /// <summary>
    /// Call this when shield blocks an attack
    /// </summary>
    public void OnShieldHit()
    {
        Debug.Log("💥 Shield was hit!");

        // Optional: Add hit effect here (flash, particle burst, etc.)
        StartCoroutine(ShieldHitEffect());

        // Destroy shield after being hit once
        DeactivateShield();
    }

    /// <summary>
    /// Visual feedback when shield is hit
    /// </summary>
    private IEnumerator ShieldHitEffect()
    {
        if (activeShieldObject != null)
        {
            MeshRenderer renderer = activeShieldObject.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                // Flash white
                Color originalColor = shieldColor;
                renderer.material.color = Color.white;
                yield return new WaitForSeconds(0.1f);
                renderer.material.color = originalColor;
            }
        }
    }

    /// <summary>
    /// Public property to check if shield is active
    /// </summary>
    public bool IsActive()
    {
        return isShieldActive;
    }

    /// <summary>
    /// Get remaining shield time
    /// </summary>
    public float GetRemainingTime()
    {
        return remainingShieldTime;
    }

    // Optional: Show shield in Scene view for debugging
    private void OnDrawGizmosSelected()
    {
        if (isShieldActive)
        {
            Gizmos.color = new Color(0f, 0.7f, 1f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, shieldSize / 2f);
        }
    }
}