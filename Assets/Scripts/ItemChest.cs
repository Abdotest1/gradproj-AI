using UnityEngine;
using System.Collections;
public class ChestPickup : MonoBehaviour
{
    //public GameObject collectEffect;
    public float respawnTime = 3f;
    private Renderer bottomRenderer;
    private Renderer upRenderer;
    private string[] items = new string[]
    {
        "Turbo Boost",
        "Shield Bubble",
        "Fireball",
        "Freeze Attack",
        "Trap",
        "Magic Wall"
    };
    private void Awake()
    {
        Transform animated = transform.Find("Chest_Animated");
        if (animated != null)
        {
            Transform bottom = animated.Find("Chest_Bottom");
            Transform up = animated.Find("Chest_Up");
            if (bottom != null) { 
                bottomRenderer = bottom.GetComponent<Renderer>();
            }

            if (up != null)
            {
                upRenderer = up.GetComponent<Renderer>();
            }
            //else
                //Debug.LogError("Chest_Up not found under Chest_Animated");
        }
        //else
            //Debug.LogError("Chest_Animated not found under ChestParent");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            /*if (collectEffect != null)
                Instantiate(collectEffect, transform.position, Quaternion.identity);
            */
            string randomItem = items[Random.Range(0, items.Length)];
            //Debug.Log($"{other.name} got: {randomItem}");
            PowerUpManager manager = other.transform.parent.gameObject.GetComponentInChildren<PowerUpManager>();
            if (manager != null)
            {
                manager.AddItem(randomItem);
            }
            else
            {
                //Debug.LogWarning("No PowerUpManager found in scene");
            }
            if (bottomRenderer != null) bottomRenderer.enabled = false;
            if (upRenderer != null) upRenderer.enabled = false;
            StartCoroutine(Respawn());
        }
    }

    private IEnumerator Respawn()
    {
        yield return new WaitForSeconds(respawnTime);
        if (bottomRenderer != null)
            bottomRenderer.enabled = true;
        if (upRenderer != null)
            upRenderer.enabled = true;
    }
}
