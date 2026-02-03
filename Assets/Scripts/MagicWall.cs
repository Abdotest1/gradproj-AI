using UnityEngine;
using System.Collections;

public class MagicWall : MonoBehaviour
{
    [Header("Growth Settings")]
    public float growDuration = 0.5f;
    public float finalHeight = 3f;
    public float finalWidth = 5f;
    public float wallThickness = 0.5f;

    [Header("Lifetime")]
    public float lifetime = 3f;

    [Header("Visuals")]
    public Material wallMaterial;
    public Color wallColor = new Color(0.5f, 0.2f, 0.8f);

    [Header("Crack Settings")]
    public float startCrackAmount = 0f;
    public float endCrackAmount = 1f;

    [Header("Particles")]
    public GameObject growParticlesPrefab;
    public GameObject[] breakVFXPrefabs;  // Array for multiple VFX
    public float vfxDestroyDelay = 3f;    // How long before VFX gets destroyed

    private Vector3 targetScale;
    private float growTimer;
    private float lifetimeTimer;
    private bool fullyGrown;
    private bool isBreaking;
    private BoxCollider wallCollider;
    private GameObject wallVisual;
    private GameObject activeGrowParticles;
    private Material wallMaterialInstance;

    void Start()
    {
        wallVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wallVisual.transform.SetParent(transform);
        wallVisual.transform.localPosition = Vector3.zero;
        wallVisual.transform.localRotation = Quaternion.identity;

        Destroy(wallVisual.GetComponent<BoxCollider>());

        Renderer rend = wallVisual.GetComponent<Renderer>();
        if (rend != null)
        {
            if (wallMaterial != null)
            {
                wallMaterialInstance = new Material(wallMaterial);
                rend.material = wallMaterialInstance;
                wallMaterialInstance.SetFloat("_Cracks_Amount", startCrackAmount);
            }
            else
            {
                rend.material.color = wallColor;
            }
        }

        wallCollider = gameObject.AddComponent<BoxCollider>();
        wallCollider.size = new Vector3(finalWidth, finalHeight, wallThickness);
        wallCollider.center = new Vector3(0, finalHeight / 2f, 0);
        wallCollider.enabled = false;

        targetScale = new Vector3(finalWidth, finalHeight, wallThickness);
        wallVisual.transform.localScale = new Vector3(finalWidth, 0.01f, wallThickness);
        wallVisual.transform.localPosition = new Vector3(0, 0.005f, 0);

        lifetimeTimer = lifetime;

        SpawnGrowParticles();
    }

    void Update()
    {
        if (isBreaking)
            return;

        if (!fullyGrown)
        {
            GrowWall();
        }
        else
        {
            lifetimeTimer -= Time.deltaTime;

            UpdateCracks();

            if (lifetimeTimer <= 0f)
            {
                StartCoroutine(BreakWall());
            }
        }
    }

    void UpdateCracks()
    {
        if (wallMaterialInstance == null)
            return;

        float progress = 1f - (lifetimeTimer / lifetime);
        float crackAmount = Mathf.Lerp(startCrackAmount, endCrackAmount, progress);

        wallMaterialInstance.SetFloat("_Cracks_Amount", crackAmount);
    }

    void SpawnGrowParticles()
    {
        if (growParticlesPrefab != null)
        {
            activeGrowParticles = Instantiate(growParticlesPrefab, transform.position, Quaternion.identity);
            activeGrowParticles.transform.SetParent(transform);
            Destroy(activeGrowParticles, growDuration + 0.5f);
        }
    }

    void GrowWall()
    {
        growTimer += Time.deltaTime;
        float progress = growTimer / growDuration;

        if (progress >= 1f)
        {
            wallVisual.transform.localScale = targetScale;
            wallVisual.transform.localPosition = new Vector3(0, finalHeight / 2f, 0);
            wallCollider.enabled = true;
            fullyGrown = true;
        }
        else
        {
            float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
            float currentHeight = Mathf.Lerp(0.01f, finalHeight, easedProgress);
            wallVisual.transform.localScale = new Vector3(finalWidth, currentHeight, wallThickness);
            wallVisual.transform.localPosition = new Vector3(0, currentHeight / 2f, 0);
        }
    }

    IEnumerator BreakWall()
    {
        isBreaking = true;
        wallCollider.enabled = false;

        SpawnBreakVFX();

        yield return new WaitForSeconds(0.05f);

        Destroy(gameObject);
    }

    void SpawnBreakVFX()
    {
        if (breakVFXPrefabs == null || breakVFXPrefabs.Length == 0)
            return;

        Vector3 vfxPosition = transform.position + Vector3.up * (finalHeight / 2f);

        foreach (GameObject vfxPrefab in breakVFXPrefabs)
        {
            if (vfxPrefab != null)
            {
                GameObject vfx = Instantiate(vfxPrefab, vfxPosition, transform.rotation);
                Destroy(vfx, vfxDestroyDelay);
            }
        }
    }
}