using Cinemachine;
using DG.Tweening;
using GLTFast.Schema;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using static Unity.MLAgents.Sensors.RayPerceptionOutput;

public class KartControllerQL : MonoBehaviour
{
    private SpawnPointManager _spawnPointManager;

    private PostProcessVolume postVolume;
    private PostProcessProfile postProfile;

    public Transform kartModel;
    public Transform kartNormal;
    public Rigidbody sphere;

    public float speed, currentSpeed;
    public float rotate, currentRotate;

    [Header("Parameters")]
    public float acceleration = 30f;
    public float drift_modifier = 25f;
    public float steering = 80f;
    public float gravity = 10f;
    public LayerMask layerMask;

    public bool drifting = false;
    public bool boosting = false;
    public int driftDirection = 0;
    public float driftPower;
    public int driftMode = 0;
    bool first, second, third;

    [Header("Drift Particles")]
    public Transform driftParticle1;
    public Transform driftParticle2;
    public Transform driftParticle3;
    public Transform boostParticle;

    private bool isFrozen = false;

    public bool boostingActive = false;

    public RayPerceptionSensorComponent3D frontRays;
    public Collider colliderToIgnore;

    public float lastSteeringSignal = 0;
    public float lastAccelerationSignal = 0;

    KartAgent agent;

    public void Awake()
    {
        _spawnPointManager = FindObjectOfType<SpawnPointManager>();
        agent = this.GetComponent<KartAgent>();
    }

    void Start()
    {
        Collider myCollider = GetComponent<Collider>();
        if (myCollider != null && colliderToIgnore != null)
        {
            Physics.IgnoreCollision(myCollider, colliderToIgnore, true);
        }

        postVolume = UnityEngine.Camera.main.GetComponent<PostProcessVolume>();
        postProfile = postVolume.profile;
    }


    public void ApplyAcceleration(float input)
    {
        if (isFrozen) return;
        if (input > 0f)
        {
            if (currentSpeed < 0f)
            {
                currentSpeed = 0f;                
            }
            speed += acceleration;
        }
        else if(input < 0f){
            
            if (currentSpeed > 0f) {
                currentSpeed = 0;
            }
            speed += -acceleration;
            //agent.AddReward(-0.0001f);
            
        }
        currentSpeed = Mathf.SmoothStep(currentSpeed, speed, Time.deltaTime * 12f);
        speed = 0f;
        currentRotate = Mathf.Lerp(currentRotate, rotate, Time.deltaTime * 4f);
        rotate = 0f;
    }

    public void AnimateKart(float input)
    {
        //Animations    

        //a) Kart
        if (!drifting)
        {
            kartModel.localEulerAngles = Vector3.Lerp(kartModel.localEulerAngles, new Vector3(0, 90 + (input * 15), kartModel.localEulerAngles.z), .2f);
        }
        else
        {
            float control = (driftDirection == 1) ? ExtensionMethods.Remap(input, -1, 1, .5f, 2) : ExtensionMethods.Remap(input, -1, 1, 2, .5f);
            kartModel.parent.localRotation = Quaternion.Euler(0, Mathf.LerpAngle(kartModel.parent.localEulerAngles.y, (control * 15) * driftDirection, .2f), 0);
            //Debug.Log(control);
        }

        //broom animation
        if (sphere.linearVelocity.magnitude > 0.1)
        {
            kartModel.GetComponentInChildren<UnityEngine.Animation>().Stop("idle");
            if (kartModel.GetComponentInChildren<UnityEngine.Animation>()["Animation"].time == 0.0f)
            {
                Transform particle_parent = kartModel.Find("the_travelers_broomstick").Find("Sketchfab_model").Find("root").Find("GLTF_SceneRootNode");
                foreach (Transform particle in particle_parent)
                {
                    particle.gameObject.SetActive(true);
                }
            }
            kartModel.GetComponentInChildren<UnityEngine.Animation>().Play("Animation");
        }
        else
        {
            kartModel.GetComponentInChildren<UnityEngine.Animation>().Stop("Animation");
            kartModel.GetComponentInChildren<UnityEngine.Animation>()["Animation"].time = 0.0f;
            Transform particle_parent = kartModel.Find("the_travelers_broomstick").Find("Sketchfab_model").Find("root").Find("GLTF_SceneRootNode");
            foreach (Transform particle in particle_parent)
            {
                particle.gameObject.SetActive(false);
            }
            particle_parent.Find("BroomRig_442").gameObject.SetActive(true);

            kartModel.GetComponentInChildren<UnityEngine.Animation>().Play("idle");
        }
    }
    private void Update()
    {
        
    }

    public void Respawn()
    {
        Transform spawnpoint = _spawnPointManager.SelectRandomSpawnpoint();
        Vector3 pos = spawnpoint.position;
        sphere.linearVelocity = Vector3.zero;
        sphere.angularVelocity = Vector3.zero;
        ResetDrift();
        currentSpeed = 0.0f;
        currentRotate = 0.0f;
        sphere.MovePosition(pos);
        transform.position = pos - new Vector3(0, 0.4f, 0);
        transform.rotation = spawnpoint.rotation;
    }

    public void FixedUpdate()
    {
        if (isFrozen) return;
        AnimateKart(lastSteeringSignal);
        if (!drifting)
        {
            sphere.AddForce(-kartModel.transform.right * currentSpeed, ForceMode.Acceleration);
        }
        else
        {
            sphere.AddForce(transform.forward * currentSpeed, ForceMode.Acceleration);
        }

        //Gravity
        sphere.AddForce(Vector3.down * gravity, ForceMode.Acceleration);

        //Follow Collider
        transform.position = sphere.transform.position - new Vector3(0, 0.4f, 0);

        //Steering
        if (sphere.linearVelocity.magnitude > 0.1)
        {
            if (lastAccelerationSignal > 0)
            {
                transform.eulerAngles = Vector3.Lerp(transform.eulerAngles, new Vector3(0, transform.eulerAngles.y + currentRotate, 0), Time.deltaTime * 5f);
            }
            else if (lastAccelerationSignal < 0)
            {
                transform.eulerAngles = Vector3.Lerp(transform.eulerAngles, new Vector3(0, transform.eulerAngles.y - currentRotate, 0), Time.deltaTime * 5f);
            }
        }

        Physics.Raycast(transform.position + (transform.up * .1f), Vector3.down, out RaycastHit hitOn, 1.1f, layerMask);
        Physics.Raycast(transform.position + (transform.up * .1f), Vector3.down, out RaycastHit hitNear, 2.0f, layerMask);

        //Normal Rotation
        /*kartNormal.up = Vector3.Lerp(kartNormal.up, hitNear.normal, Time.deltaTime * 8.0f);
        kartNormal.Rotate(0, transform.eulerAngles.y, 0);*/

        //Prevent micro movments when input = 0
        if (sphere.linearVelocity.magnitude < 0.1)
        {
            sphere.linearVelocity = Vector3.zero;
            sphere.angularVelocity = Vector3.zero;
        }

        /*
          // dont look backwards while drifting modifier
          RayPerceptionInput rayPerceptionInput = frontRays.GetRayPerceptionInput();

          // Perform the raycasts and get the output results
          RayOutput []rayOutputs = RayPerceptionSensor.Perceive(rayPerceptionInput).RayOutputs;
          foreach (RayOutput rayOutput in rayOutputs)
          {
              //Debug.Log(rayOutput.HitGameObject.gameObject);
                  if (rayOutput.HitGameObject.gameObject.Equals(sphere.GetComponent<CheckpointManager>().lastCheckpoint.gameObject))
                  {
                      agent.AddReward(-0.005f);
                      //Debug.Log("looked backwards punishment");
                  }
          }

        */
    }

    public void Steer(float steeringSignal)
    {
        //if (drifting) return;

        if (isFrozen) return;
        int steerDirection = steeringSignal > 0 ? 1 : -1;
        float steeringStrength = Mathf.Abs(steeringSignal);
        //float steeringStrength = steerDirection;
        rotate = (steering * steerDirection) * steeringStrength * 0.7f;
    }
    public void Steer(int direction, float amount)
    {
        rotate = (steering * direction) * amount * 0.7f;
    }


    public void DriftStart(float steeringSignal) {

        if (!drifting && steeringSignal != 0f && currentSpeed > 20f && !boosting)
        {
            if (boostingActive  || boosting) {
                return;
            }
            driftDirection = steeringSignal > 0 ? 1 : -1;
            drifting = true;

            //acceleration -= acceleration_drift_modifier;

            kartModel.parent.DOComplete();
            kartModel.parent.DOPunchPosition(transform.up * .2f, .3f, 5, 1);
        }
    }
    public void DriftUpdate(float steeringSignal) {
        if (!drifting || boosting || boostingActive) return;

        //speed -= drift_modifier;

        float control = (driftDirection == 1) ? ExtensionMethods.Remap(steeringSignal, -1, 1, 0, 2) : ExtensionMethods.Remap(steeringSignal, -1, 1, 2, 0);
        float powerControl = (driftDirection == 1) ? ExtensionMethods.Remap(steeringSignal, -1, 1, .2f, 1) : ExtensionMethods.Remap(steeringSignal, -1, 1, 1, .2f);
        Steer(driftDirection, control);
        driftPower += (powerControl * 0.85f);

        ColorDrift();
    }
    public void ColorDrift()
    {
        if (driftPower > 20 && driftPower < 100 - 1 && !first)
        {
            driftParticle1.gameObject.SetActive(true);
            driftMode = 1;
            first = true;
            second = false;
            third = false;
        }

        if (driftPower > 150 && driftPower < 250 - 1 && !second)
        {
            driftParticle1.gameObject.SetActive(false);
            driftParticle2.gameObject.SetActive(true);
            driftMode = 2;
            first = false;
            second = true;
            third = false;
        }

        if (driftPower > 250 && !third)
        {
            driftParticle1.gameObject.SetActive(false);
            driftParticle2.gameObject.SetActive(false);
            driftParticle3.gameObject.SetActive(true);
            driftMode = 3;
            first = false;
            second = false;
            third = true;
        }

        if (driftPower > 350) {
            agent.AddReward(-0.000001f);
        }

    }
    public void DriftEnd() {
        if (!drifting) return;
        if (drifting)
        {
            //Debug.Log("driftendeddd");
            driftParticle1.gameObject.SetActive(false);
            driftParticle2.gameObject.SetActive(false);
            driftParticle3.gameObject.SetActive(false);
            StartCoroutine(Boost());
        }
    }
    public IEnumerator Boost()
    {
        if (boosting)
        {
            ResetDrift();
            yield return new WaitForSeconds(1);
            //Debug.Log("Drifting Failed");
        }
        else
        {
            boosting = true;
            drifting = false;

            if (driftMode > 0)
            {
                if (driftMode == 1)
                {
                    //agent.AddReward(0.0001f);
                }
                if (driftMode == 2)
                {
                    //agent.AddReward(0.0002f);
                }
                if (driftMode == 3)
                {
                    //agent.AddReward(0.0003f);
                }
                DOVirtual.Float(currentSpeed * 3, currentSpeed, .3f * driftMode, Speed);
                DOVirtual.Float(0, 1, .5f, ChromaticAmount).OnComplete(() => DOVirtual.Float(1, 0, .5f, ChromaticAmount));
                StartCoroutine(BoostEffect());
                //Debug.Log("Boosting !!!!!!!!!!");
            }
            else {
                DOVirtual.Float(currentSpeed / 3, currentSpeed, .3f * 2, Speed);
                //Debug.Log("Drifting Failed");
            }

                ResetDrift();
            yield return new WaitForSeconds(1);
        }
        boosting = false;
    }
    void ChromaticAmount(float x)
    {
        postProfile.GetSetting<ChromaticAberration>().intensity.value = x;
    }
    public IEnumerator BoostEffect()
    {
        boostParticle.gameObject.SetActive(true);
        yield return new WaitForSeconds(2);
        boostParticle.gameObject.SetActive(false);
        boostingActive = false;
    }
    private void Speed(float x)
    {
        currentSpeed = x;
    }
    void ResetDrift()
    {
        boosting = false;
        drifting = false;
        driftPower = 0;
        driftMode = 0;
        first = second = third = false;
        driftDirection = 0;
        kartModel.parent.DOLocalRotate(Vector3.zero, .5f).SetEase(Ease.OutBack);
    }



    public void SetFrozen(bool frozen)
    {
        isFrozen = frozen;
        Debug.Log(gameObject.name + " frozen = " + frozen);

        if (frozen)
        {
            // Use 'sphere' instead of GetComponent<Rigidbody>()
            if (sphere != null)
            {
                sphere.linearVelocity = Vector3.zero;
                sphere.angularVelocity = Vector3.zero;
            }
        }
    }

}
