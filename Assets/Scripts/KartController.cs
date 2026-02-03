using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.Rendering.PostProcessing;
using Cinemachine;

public class KartController : MonoBehaviour
{
    private PostProcessVolume postVolume;
    private PostProcessProfile postProfile;

    public Transform kartModel;
    public Transform kartNormal;
    public Rigidbody sphere;

    public List<ParticleSystem> primaryParticles = new List<ParticleSystem>();
    public List<ParticleSystem> secondaryParticles = new List<ParticleSystem>();

    float speed, currentSpeed;
    float rotate, currentRotate;
    int driftDirection;
    float driftPower;
    public int driftMode = 0;
    bool first, second, third;
    Color c;
    private bool isFrozen = false;


    [Header("Bools")]
    public bool drifting = false;
    public bool boosting = false;

    [Header("Parameters")]

    public float acceleration = 30f;
    public float decceleration = -45f;
    public float steering = 80f;
    public float gravity = 10f;
    public LayerMask layerMask;

    [Header("Particles")]
    public Transform driftParticle1;
    public Transform driftParticle2;
    public Transform driftParticle3;
    public Transform boostParticle;

    public Collider colliderToIgnore;

    void Start()
    {
        Collider myCollider = GetComponent<Collider>();
        if (myCollider != null && colliderToIgnore != null)
        {
            Physics.IgnoreCollision(myCollider, colliderToIgnore, true);
        }

        postVolume = Camera.main.GetComponent<PostProcessVolume>();
        postProfile = postVolume.profile;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) {
            /*float time = Time.timeScale == 1 ? .2f : 1;
            Time.timeScale = time;*/
            
        }

        //Follow Collider
        transform.position = sphere.transform.position - new Vector3(0, 0.4f, 0);

        //Accelerate
        if (Input.GetButton("Fire1")) {
            speed = acceleration;
        }

        //Deccelerate
        if (Input.GetButton("Fire2")) {
            if (currentSpeed > 0) { 
                currentSpeed = 0;
            }
            speed = -acceleration;
        }

        //Steer
        if (Input.GetAxis("Horizontal") != 0)
        {
            //Debug.Log(Input.GetAxis("Horizontal"));
            int dir = Input.GetAxis("Horizontal") > 0 ? 1 : -1;
            float amount = Mathf.Abs((Input.GetAxis("Horizontal")));
            Steer(dir, amount);
        }

        //Drift
        if (Input.GetButtonDown("Jump") && !drifting && Input.GetAxis("Horizontal") != 0 && currentSpeed > 0)
        {
            drifting = true;
            driftDirection = Input.GetAxis("Horizontal") > 0 ? 1 : -1;

            foreach (ParticleSystem p in primaryParticles)
            {
                p.startColor = Color.clear;
                p.Play();
            }

            kartModel.parent.DOComplete();
            kartModel.parent.DOPunchPosition(transform.up * .2f, .3f, 5, 1);

        }

        if (drifting)
        {
            currentSpeed -= 0.0007f;

            float control = (driftDirection == 1) ? ExtensionMethods.Remap(Input.GetAxis("Horizontal"), -1, 1, 0, 2) : ExtensionMethods.Remap(Input.GetAxis("Horizontal"), -1, 1, 2, 0);
            float powerControl = (driftDirection == 1) ? ExtensionMethods.Remap(Input.GetAxis("Horizontal"), -1, 1, .2f, 1) : ExtensionMethods.Remap(Input.GetAxis("Horizontal"), -1, 1, 1, .2f);
            Steer(driftDirection, control);
            driftPower += (powerControl * 0.85f);

            ColorDrift();
        }

        if (Input.GetButtonUp("Jump") && drifting)
        {
            StartCoroutine(Boost());
        }

        currentSpeed = Mathf.SmoothStep(currentSpeed, speed, Time.deltaTime * 12f); speed = 0f;
        currentRotate = Mathf.Lerp(currentRotate, rotate, Time.deltaTime * 4f); rotate = 0f;



        //Animations    

        //a) Kart
        if (!drifting)
        {
            kartModel.localEulerAngles = Vector3.Lerp(kartModel.localEulerAngles, new Vector3(0, 90 + (Input.GetAxis("Horizontal") * 15), kartModel.localEulerAngles.z), .2f);
        }
        else
        {
            float control = (driftDirection == 1) ? ExtensionMethods.Remap(Input.GetAxis("Horizontal"), -1, 1, .5f, 2) : ExtensionMethods.Remap(Input.GetAxis("Horizontal"), -1, 1, 2, .5f);
            kartModel.parent.localRotation = Quaternion.Euler(0, Mathf.LerpAngle(kartModel.parent.localEulerAngles.y, (control * 15) * driftDirection, .2f), 0);
        }

        //broom animation
        if (sphere.linearVelocity.magnitude > 0.1)
        {
            kartModel.GetComponentInChildren<Animation>().Stop("idle");
            if (kartModel.GetComponentInChildren<Animation>()["Animation"].time == 0.0f)
            {
                Transform particle_parent = kartModel.Find("the_travelers_broomstick").Find("Sketchfab_model").Find("root").Find("GLTF_SceneRootNode");
                foreach (Transform particle in particle_parent)
                {
                    particle.gameObject.SetActive(true);
                }
            }
            kartModel.GetComponentInChildren<Animation>().Play("Animation");
        }
        else
        {
            kartModel.GetComponentInChildren<Animation>().Stop("Animation");
            kartModel.GetComponentInChildren<Animation>()["Animation"].time = 0.0f;
            Transform particle_parent = kartModel.Find("the_travelers_broomstick").Find("Sketchfab_model").Find("root").Find("GLTF_SceneRootNode");
            foreach (Transform particle in particle_parent) {
                particle.gameObject.SetActive(false);
            }
            particle_parent.Find("BroomRig_442").gameObject.SetActive(true);

            kartModel.GetComponentInChildren<Animation>().Play("idle");
        }
    }

    private void FixedUpdate()
    {
        //Forward Acceleration
        if(!drifting)
            sphere.AddForce(-kartModel.transform.right * currentSpeed, ForceMode.Acceleration);
        else
            sphere.AddForce(transform.forward * currentSpeed, ForceMode.Acceleration);

        //Gravity
        sphere.AddForce(Vector3.down * gravity, ForceMode.Acceleration);

        //Steering
        if (sphere.linearVelocity.magnitude > 0.1)
        {
            if (Input.GetButton("Fire1")) {
                transform.eulerAngles = Vector3.Lerp(transform.eulerAngles, new Vector3(0, transform.eulerAngles.y + currentRotate, 0), Time.deltaTime * 5f);
            }
            else if (Input.GetButton("Fire2")) {
                transform.eulerAngles = Vector3.Lerp(transform.eulerAngles, new Vector3(0, transform.eulerAngles.y - currentRotate, 0), Time.deltaTime * 5f);
            }
        }
        RaycastHit hitOn;
        RaycastHit hitNear;

        Physics.Raycast(transform.position + (transform.up*.1f), Vector3.down, out hitOn, 1.1f,layerMask);
        Physics.Raycast(transform.position + (transform.up * .1f)   , Vector3.down, out hitNear, 2.0f, layerMask);

        //Normal Rotation
        kartNormal.up = Vector3.Lerp(kartNormal.up, hitNear.normal, Time.deltaTime * 8.0f);
        kartNormal.Rotate(0, transform.eulerAngles.y, 0);

        if (sphere.linearVelocity.magnitude < 0.1) {
            sphere.linearVelocity = Vector3.zero;
            sphere.angularVelocity = Vector3.zero;
        }
    }

    public IEnumerator Boost() {
        if (boosting)
        {
            driftPower = 0;
            driftMode = 0;
            first = false; second = false; third = false;

            kartModel.parent.DOLocalRotate(Vector3.zero, .5f).SetEase(Ease.OutBack);
            yield return new WaitForSeconds(1);
        }
        else {
            boosting = true;
            drifting = false;

            if (driftMode > 0)
            {
                StartCoroutine(MoveCameraOnBoost());
                DOVirtual.Float(currentSpeed * 3, currentSpeed, .3f * driftMode, Speed);
                DOVirtual.Float(0, 1, .5f, ChromaticAmount).OnComplete(() => DOVirtual.Float(1, 0, .5f, ChromaticAmount));
                StartCoroutine(BoostEffect());
            }

            driftPower = 0;
            driftMode = 0;
            first = false; second = false; third = false;

            driftParticle1.gameObject.SetActive(false);
            driftParticle2.gameObject.SetActive(false);
            driftParticle3.gameObject.SetActive(false);

            kartModel.parent.DOLocalRotate(Vector3.zero, .5f).SetEase(Ease.OutBack);
            yield return new WaitForSeconds(1);
        }
        boosting = false;
    }
    public IEnumerator BoostEffect() {
        boostParticle.gameObject.SetActive(true);
        yield return new WaitForSeconds(2);
        boostParticle.gameObject.SetActive(false);
    }


    public void Steer(int direction, float amount)
    {
        rotate = (steering * direction) * amount * 0.7f;
    }

    public void ColorDrift()
    {

        if (driftPower > 25 && driftPower < 250-1 && !first)
        {
            driftParticle1.gameObject.SetActive(true);
            driftMode = 1;
            first = true;
            second = false;
            third = false;
        }

        if (driftPower > 250 && driftPower < 450-1 && !second)
        {
            driftParticle1.gameObject.SetActive(false);
            driftParticle2.gameObject.SetActive(true);
            driftMode = 2;
            first = false;
            second = true;
            third = false;
        }

        if (driftPower > 450 && !third)
        {
            driftParticle1.gameObject.SetActive(false);
            driftParticle2.gameObject.SetActive(false);
            driftParticle3.gameObject.SetActive(true);
            driftMode = 3;
            first = false;
            second = false;
            third = true;
        }

    }

    private void Speed(float x)
    {
        currentSpeed = x;
    }

    void ChromaticAmount(float x)
    {
        postProfile.GetSetting<ChromaticAberration>().intensity.value = x;
    }

    private IEnumerator MoveCameraOnBoost() {
        
        GameObject.Find("CM vcam1 (1)").GetComponent<CinemachineVirtualCamera>().m_Priority = 15;

        yield return new WaitForSeconds(1);

        GameObject.Find("CM vcam1 (1)").GetComponent<CinemachineVirtualCamera>().m_Priority = 5;

    }

    public void StopKart() { 
        sphere.linearVelocity = Vector3.zero;
    }

    public void SetFrozen(bool frozen)
    {
        isFrozen = frozen;

        if (frozen)
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }

    /*private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position + transform.up, transform.position - (transform.up * 2));
    }*/
}
