using RoadArchitect;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class PowerUpManager : MonoBehaviour
{
    [Header("Inventory Settings")]
    [Tooltip("UI image slot used to display the current power‑up")]
    public Image[] slotImages;    // size = 1 in Inspector
    [Tooltip("Boost icon sprite")]
    public Sprite boostIcon;
    [Tooltip("Shield bubble icon sprite")]
    public Sprite shieldIcon;
    [Tooltip("Sprite shown in the slot when player gets Fireball")]
    public Sprite fireballIcon;
    [Tooltip("Sprite shown in the slot when player gets Freezing")]
    public Sprite freezeIcon;
    public Sprite trapIcon;
    public GameObject trapPrefab;
    public float trapSpawnDistance = 3f;
    public float trapSize = 0.5f;
    public float trapHeightOffset = -0.3f;
    private KartController kartController;
    private List<string> activeItems = new List<string>();
    private float shieldDuration = 5f;
    [Header("Magic Wall Settings")]
    public Sprite wallIcon;
    public GameObject wallPrefab;
    public float wallSpawnDistance = 3f;

    public GameObject iceSpikesVFXPrefab;
    public GameObject iceCrystalPrefab;
    public float freezeWaitTime = 0f;
    public float detectionRadius = 30f;

    void Start()
    {
        kartController = this.GetComponent<KartController>();
        if (kartController != null)
        {
            UpdateUI();
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            //Debug.Log("K pressed");
            CallUseItem();
        }
    }

    public void CallUseItem()
    {
        if (activeItems.Count > 0)
        {
            string item = activeItems[0];

            // SPECIAL CASE: Position Swap just performs the swap
            if (item == "Position Swap")
            {
                SwapAbility swap = GetComponent<SwapAbility>();

                if (swap != null && swap.canSwap)
                {
                    swap.PerformSwap(); // Just swap!
                    activeItems.RemoveAt(0);
                    UpdateUI();
                }
                else
                {
                    Debug.LogWarning("⚠️ No valid target to swap with!");
                }
            }
            else
            {
                // All other items
                UseItem(item);
                activeItems.RemoveAt(0);
                UpdateUI();
            }
        }
    }

    // Called by ChestPickup when player collects a power‑up
    // Called by ChestPickup when player collects a power‑up
    // Called by ChestPickup when player collects a power‑up
    public void AddItem(string itemName)
    {
        if (activeItems.Count > 0)
            return; // only one slot

        activeItems.Add(itemName);
        Debug.Log("Added item: " + itemName);

        // SPECIAL CASE: Position Swap activates immediately!
        if (itemName == "Position Swap")
        {
            SwapAbility swap = GetComponent<SwapAbility>();
            if (swap != null)
            {
                swap.ActivateSwapAbility(); // Arrow appears NOW!
            }
        }

        if (kartController != null)
        {
            UpdateUI();
        }
    }

    // What happens when player uses a power‑up
    private void UseItem(string item)
    {
        //Debug.Log("Using item: " + item);

        switch (item)
        {
            case "Turbo Boost":
                //Debug.Log("Boost Activated from Power‑up!");
                if (kartController != null)
                {
                    kartController.driftMode = 3;
                    StartCoroutine(kartController.Boost());
                }
                    break;

            case "Shield Bubble":
                ShieldSystem shield = GetComponent<ShieldSystem>();
                if (shield != null)
                {
                    shield.ActivateShield();
                }
                else
                {
                    Debug.LogWarning("⚠️ No ShieldSystem found on " + gameObject.name);
                }
                break;

            case "Fireball":
                LaunchFireball();
                break;

            case "Freeze Attack":
                StartCoroutine(ProcessFreezeAttack());
                break;

            case "Trap":
                SpawnTrap();
                break;

            case "Magic Wall":
                SpawnMagicWall();
                break;

            default:
                Debug.LogWarning($"Item '{item}' not implemented.");
                break;
        }
    }


    // --------------------------------------------
    // expose shield state for other scripts (Fireball etc.)
    public bool IsShieldActive
    {
        get
        {
            ShieldSystem shield = GetComponent<ShieldSystem>();
            return shield != null && shield.IsActive();
        }
    }
    // --------------------------------------------

    public GameObject fireballPrefab;   // assign in Inspector
    public Transform firePoint;         // where the fireball starts (front of kart)

    private void LaunchFireball()
    {
        GameObject f = Instantiate(fireballPrefab);
        Fireball fb = f.GetComponent<Fireball>();
        fb.Initialize(this.transform);
        //fb.road = Object.FindFirstObjectByType<Road>(); // new API call
    }

    // UI update for the single slot
    private void UpdateUI()
    {
        foreach (Image img in slotImages)
        {
            if (img != null)
                img.enabled = false;
        }

        if (activeItems.Count > 0 && slotImages.Length > 0)
        {
            Image slot = slotImages[0];
            if (activeItems[0] == "Turbo Boost")
                slot.sprite = boostIcon;
            else if (activeItems[0] == "Shield Bubble")
                slot.sprite = shieldIcon;
            else if (activeItems[0] == "Fireball")
                slot.sprite = fireballIcon;
            else if (activeItems[0] == "Freeze Attack")
                slot.sprite = freezeIcon;
            else if (activeItems[0] == "Trap")
                slot.sprite = trapIcon;
            else if (activeItems[0] == "Magic Wall")
                slot.sprite = wallIcon;

            slot.enabled = true;
        }
    }

    public bool DoesHaveItem() {
        if (activeItems.Count <= 0) { 
                    return false;
        }
        return true;
    }

    public void OnHitByFireBall()
    {
        ShieldSystem shield = GetComponent<ShieldSystem>();

        if (shield != null && shield.IsActive())
        {
            shield.OnShieldHit(); // Shield blocks and disappears
            return;
        }

        GetComponent<KartController>()?.StopKart();
    }

    /*==========================*/
    /*         Freezing         */
    /*==========================*/

    [Header("Freeze Settings")]
    public float freezeDuration = 3f;

    private IEnumerator ProcessFreezeAttack()
    {
        Debug.Log("🧊 Freeze Attack Started!");
        Debug.Log("⏱️ freezeWaitTime = " + freezeWaitTime);
        GameObject target = FindClosestEnemy();
        if (target == null)
        {
            Debug.LogWarning("❌ No enemy found within radius: " + detectionRadius);
            yield break;
        }
        Debug.Log("✅ Found target: " + target.name);
        if (iceSpikesVFXPrefab != null)
        {
            Vector3 behindTarget = target.transform.position - target.transform.forward * 3f;
            Quaternion lookAtTarget = Quaternion.LookRotation(target.transform.position - behindTarget);
            GameObject vfxInstance = Instantiate(iceSpikesVFXPrefab, behindTarget, lookAtTarget);
            Debug.Log("✅ VFX Spawned behind target");
            Destroy(vfxInstance, 3f);
        }
        else
        {
            Debug.LogWarning("❌ VFX Prefab is NULL!");
        }
        yield return new WaitForSeconds(freezeWaitTime);
        if (iceCrystalPrefab != null)
        {
            GameObject crystal = Instantiate(iceCrystalPrefab, target.transform.position, Quaternion.identity);
            crystal.transform.SetParent(target.transform);
            crystal.transform.localPosition = Vector3.zero;
            Debug.Log("✅ Crystal spawned on target");
            StartCoroutine(FreezeTarget(target, crystal));
        }
        else
        {
            Debug.LogWarning("❌ Ice Crystal Prefab is NULL!");
        }
    }
    private IEnumerator FreezeTarget(GameObject target, GameObject crystal)
    {
        ShieldSystem targetShield = target.GetComponent<ShieldSystem>();

        if (targetShield != null && targetShield.IsActive())
        {
            Debug.Log("🛡️ Shield blocked the freeze attack!");
            targetShield.OnShieldHit(); 

            if (crystal != null)
            {
                Destroy(crystal);
            }

            yield break;
        }

        KartControllerQL ai = target.GetComponent<KartControllerQL>();

        if (ai != null)
        {
            Debug.Log("🧊 Freezing: " + target.name);
            ai.SetFrozen(true);
        }
        else
        {
            Debug.LogWarning("❌ No KartControllerQL on target!");
        }
        yield return new WaitForSeconds(freezeDuration);
        if (ai != null)
        {
            Debug.Log("🔥 Unfreezing: " + target.name);
            ai.SetFrozen(false);
        }
        if (crystal != null)
        {
            Destroy(crystal);
            Debug.Log("✅ Crystal destroyed");
        }
    }
    private GameObject FindClosestEnemy()
    {
        KartControllerQL[] allAI = FindObjectsByType<KartControllerQL>(FindObjectsSortMode.None);
        Debug.Log("Found " + allAI.Length + " AI karts in scene");

        GameObject closest = null;
        float closestDist = detectionRadius * detectionRadius;

        foreach (var ai in allAI)
        {
            if (ai.gameObject == gameObject) continue;
            float dist = Vector3.SqrMagnitude(ai.transform.position - transform.position);
            Debug.Log("AI: " + ai.name + " | Distance: " + Mathf.Sqrt(dist));
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = ai.gameObject;
            }
        }
        return closest;
    }

    /*========================================================================*/

    /*==========================*/
    /*          Trap            */
    /*==========================*/

    private void SpawnTrap()
    {
        if (trapPrefab == null)
        {
            Debug.LogWarning("Trap Prefab is NULL!");
            return;
        }

        Vector3 behindKart = transform.position - transform.forward * trapSpawnDistance;
        behindKart.y = transform.position.y + trapHeightOffset;

        GameObject trap = Instantiate(trapPrefab, behindKart, Quaternion.identity);

        Debug.Log("Trap spawned behind kart");
    }

    /*========================================================================*/

    /*==========================*/
    /*       Magic Wall         */
    /*==========================*/

    private void SpawnMagicWall()
    {
        if (wallPrefab == null)
        {
            Debug.LogWarning("Wall Prefab is NULL!");
            return;
        }

        Vector3 behindKart = transform.position - transform.forward * wallSpawnDistance;
        behindKart.y -= 0.3f; 

        Quaternion wallRotation = transform.rotation;

        Instantiate(wallPrefab, behindKart, wallRotation);

        Debug.Log("Magic Wall spawned behind kart");
    }

    /*========================================================================*/
    public bool HasItem()
    {
        return activeItems.Count == 0 ? false : true;
    }
}