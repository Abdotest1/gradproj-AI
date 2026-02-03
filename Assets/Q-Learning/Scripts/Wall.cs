    using UnityEngine;

    public class Wall : MonoBehaviour
    {
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            
        }

        // Update is called once per frame
        void Update()
        {
            
        }

        public void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.GetComponent<WallManager>() != null) {
                collision.gameObject.GetComponent<WallManager>().WallHit();
            }
        }
    }
