using UnityEngine;

public class WallManager : MonoBehaviour
{
    public KartAgent kartAgent;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void WallHit() {
        kartAgent.AddReward(-0.3f);
    }
}
