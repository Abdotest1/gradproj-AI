using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Trap : MonoBehaviour
{
    [Header("Trap Settings")]
    public float lifetime = 10f;
    public float effectDuration = 5f;
    public float slowMultiplier = 0.5f;

    private bool isTriggered = false;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isTriggered) return;

        Transform root = other.transform.root;

        KartControllerQL ai = root.GetComponentInChildren<KartControllerQL>();
        if (ai != null)
        {
            Debug.Log("AI found! Applying trap effect...");
            isTriggered = true;
            StartCoroutine(ApplyTrapEffectAI(ai));
            return;
        }

        KartController player = root.GetComponentInChildren<KartController>();
        if (player != null)
        {
            Debug.Log("Player found! Applying trap effect...");
            isTriggered = true;
            StartCoroutine(ApplyTrapEffectPlayer(player));
            return;
        }
    }

    private IEnumerator ApplyTrapEffectPlayer(KartController player)
    {
        // Slow down the sphere velocity directly
        Rigidbody sphere = player.sphere;

        // Store original acceleration
        float originalAcceleration = player.acceleration;
        player.acceleration *= slowMultiplier;

        // Also slow current velocity
        if (sphere != null)
        {
            sphere.linearVelocity *= slowMultiplier;
        }

        Debug.Log("Player slowed! Acceleration: " + player.acceleration);
        ShowDarkScreen();

        // Hide trap
        HideTrap();

        // Wait for effect duration
        yield return new WaitForSeconds(effectDuration);

        // Restore
        player.acceleration = originalAcceleration;
        Debug.Log("Player speed restored to " + originalAcceleration);

        Destroy(gameObject);
    }

    private IEnumerator ApplyTrapEffectAI(KartControllerQL ai)
    {
        // Store original acceleration
        float originalAcceleration = ai.acceleration;
        ai.acceleration *= slowMultiplier;

        // Also slow current velocity
        Rigidbody sphere = ai.sphere;
        if (sphere != null)
        {
            sphere.linearVelocity *= slowMultiplier;
        }

        Debug.Log("AI slowed! Acceleration: " + ai.acceleration);

        // Hide trap
        HideTrap();

        // Wait for effect duration
        yield return new WaitForSeconds(effectDuration);

        // Restore
        ai.acceleration = originalAcceleration;
        Debug.Log("AI speed restored to " + originalAcceleration);

        Destroy(gameObject);
    }

    private void HideTrap()
    {
        ParticleSystem ps = GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ps.Stop();
        }
        else
        {
            ps = GetComponentInChildren<ParticleSystem>();
            if (ps != null) ps.Stop();
        }

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
    }

    private void ShowDarkScreen()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("No Canvas found for dark screen!");
            return;
        }

        GameObject darkOverlay = new GameObject("DarkOverlay");
        darkOverlay.transform.SetParent(canvas.transform, false);

        Image img = darkOverlay.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.7f);

        RectTransform rect = darkOverlay.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Destroy(darkOverlay, effectDuration);

        Debug.Log("Dark screen activated!");
    }
}