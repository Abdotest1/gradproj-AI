using GLTFast.Schema;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class KartAgent : Agent
{
    public CheckpointManager _checkpointManager;
    public RaceProgress _raceProgress;
    private KartControllerQL _kartController;

    float actionHoldDuration = 0f;
    float actionHoldTimer = 0;

    //called once at the start
    public override void Initialize()
    {
        _kartController = GetComponent<KartControllerQL>();
    }

    //Called each time it has timed-out or has reached the goal
    public override void OnEpisodeBegin()
    {
        _checkpointManager.ResetCheckpoints();
        _kartController.Respawn();
    }

    #region Edit this region!

    //Collecting extra Information that isn't picked up by the RaycastSensors
    public override void CollectObservations(VectorSensor sensor)
    {
        // Vector between Kart and next checkpoint
        Vector3 diff = _checkpointManager.nextCheckPointToReach.transform.position - transform.position;
        sensor.AddObservation(diff / 20f); // Divide by 20 to normalize
        
        AddReward(-0.0003f); // Promote faster driving

        sensor.AddObservation(_kartController.currentSpeed);
        sensor.AddObservation(_kartController.sphere.linearVelocity.magnitude);

        // Steering
        sensor.AddObservation(_kartController.lastSteeringSignal); // store steering signal

        // Drift state
        sensor.AddObservation(_kartController.drifting ? 1f : 0f);
        sensor.AddObservation(_kartController.driftMode);
        //sensor.AddObservation(_raceProgress.currentLap);
    }

    //Processing the actions received
    public override void OnActionReceived(ActionBuffers actions)
    {
        var input = actions.ContinuousActions;
        var discreatInput = actions.DiscreteActions;
        /*
        _kartController.lastSteeringSignal = input[0];
        _kartController.lastAccelerationSignal = input[1];
        _kartController.Steer(input[0]);
        _kartController.DriftStart((input[2] > 0f), input[0]);
        _kartController.DriftUpdate(input[0]);
        _kartController.DriftEnd((input[2] <= 0f));
        _kartController.ApplyAcceleration(input[1]);
        //_kartController.AnimateKart(input[0]);
        */
        Vector3 toCheckpoint = (_checkpointManager.nextCheckPointToReach.transform.position - transform.position);
        toCheckpoint.y = 0;
        toCheckpoint = toCheckpoint.normalized;
        Vector3 velocityDir = _kartController.sphere.linearVelocity;
        velocityDir.y = 0;
        float velocityTowardsTarget = Vector3.Dot(velocityDir, toCheckpoint);
        AddReward(velocityTowardsTarget * 0.0000000005f);

        //yarab
        actionHoldTimer -= Time.fixedDeltaTime;

        if (actionHoldTimer <= 0f)
        {
            actionHoldTimer = actionHoldDuration;
            if (discreatInput[0] == 0)
            {
                _kartController.Steer(1, 1);
                if (_kartController.lastSteeringSignal == 1 && (velocityTowardsTarget * 0.0001f) > 0.25f)
                {
                    AddReward(0.0000000005f);
                }
                _kartController.lastSteeringSignal = 1;
                _kartController.DriftEnd();
            }
            else if (discreatInput[0] == 1)
            {
                _kartController.Steer(-1, 1);
                if (_kartController.lastSteeringSignal == -1 && (velocityTowardsTarget * 0.0001f) > 0.25f)
                {
                    AddReward(0.0000000005f);
                }
                _kartController.lastSteeringSignal = -1;
                _kartController.DriftEnd();
            }
            else if (discreatInput[0] == 2)
            {
                _kartController.DriftStart(1);
                if (_kartController.lastSteeringSignal == 1 && (velocityTowardsTarget * 0.0001f) > 0.25f)
                {
                    AddReward(0.0000000005f);
                }
                _kartController.lastSteeringSignal = 1;
                _kartController.DriftUpdate(1);
            }
            else if (discreatInput[0] == 3)
            {
                _kartController.DriftStart(-1);
                if (_kartController.lastSteeringSignal == -1 && (velocityTowardsTarget * 0.0001f) > 0.25f)
                {
                    AddReward(0.0000000005f);
                }
                _kartController.lastSteeringSignal = -1;
                _kartController.DriftUpdate(-1);
            }
            else if (discreatInput[0] == 4)
            {
                _kartController.Steer(0);
                if (_kartController.lastSteeringSignal == 0 && (velocityTowardsTarget * 0.0001f) > 0.25f)
                {
                    AddReward(0.0000000005f);
                }
                _kartController.lastSteeringSignal = 0;
                _kartController.DriftEnd();
            }
            else if (discreatInput[0] == 5)
            {
                //_kartController.Steer(0);
                _kartController.lastSteeringSignal = 0;
                if (_kartController.lastSteeringSignal == 0 && (velocityTowardsTarget * 0.0001f) > 0.25f)
                {
                    AddReward(0.0000000005f);
                }
                _kartController.lastSteeringSignal = 0;
                _kartController.DriftUpdate(0);
            }
        }
        else {
            _kartController.DriftUpdate(_kartController.lastSteeringSignal);
        }
           
        _kartController.ApplyAcceleration(input[0]);
        _kartController.lastAccelerationSignal = input[0];

    }

    //For manual testing with human input, the actionsOut defined here will be sent to OnActionReceived
    
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var action = actionsOut.ContinuousActions;
        var discreatActions = actionsOut.DiscreteActions;
        /*
        action[0] = Input.GetAxis("Horizontal"); // Steering
        action[1] = Input.GetAxis("Vertical"); // Acceleration / decceleration
        action[2] = Input.GetKey(KeyCode.Space) ? 1f : 0f; // drift
        */

        action[0] = Input.GetAxis("Vertical"); // Acceleration / decceleration
        if (Input.GetAxis("Horizontal") >= 0.3f && !(Input.GetKey(KeyCode.Space)))
        {
            discreatActions[0] = 0;
        }
        else if (Input.GetAxis("Horizontal") <= -0.3f && !(Input.GetKey(KeyCode.Space))) {
            discreatActions[0] = 1;
        }
        else if (Input.GetAxis("Horizontal") >= 0.3f && (Input.GetKey(KeyCode.Space)))
        {
            discreatActions[0] = 2;
        }
        else if (Input.GetAxis("Horizontal") <= -0.3f && (Input.GetKey(KeyCode.Space)))
        {
            discreatActions[0] = 3;
        }
        else if ((Input.GetAxis("Horizontal") < 0.3f && Input.GetAxis("Horizontal") > -0.3f) && (!(Input.GetKey(KeyCode.Space)))) {
            discreatActions[0] = 4;
        }
        else if ((Input.GetAxis("Horizontal") < 0.3f && Input.GetAxis("Horizontal") > -0.3f) && (Input.GetKey(KeyCode.Space))) {
            discreatActions[0] = 5;
        }
    }

    #endregion
}
