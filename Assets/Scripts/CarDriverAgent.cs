using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine;

public class CarDriverAgent : Agent
{
    [SerializeField] private Transform car;
    [SerializeField] private TrackCheckpoints trackCheckpoints;
    [SerializeField] private int carId; // Unique ID for the car
    private Rigidbody carRigidbody;
    private PrometeoCarController carController;

    private float lastCheckpointTime;
    private float lastMoveTime;
    private Vector3 lastPosition;
    private const float movementThreshold = 1.0f; // Distance the car must move to reset the timer
    private const float timeThreshold = 8.0f; // Seconds

    // Expose starting position and rotation in the Unity Inspector
    [SerializeField] private Vector3 startingPosition;
    [SerializeField] private Vector3 startingRotation;

    private void Awake()
    {
        carRigidbody = car.GetComponent<Rigidbody>();
        carController = car.GetComponent<PrometeoCarController>();
        if (carRigidbody == null || carController == null)
        {
            Debug.LogError("Required component is missing on the car object!");
        }
        trackCheckpoints.RegisterCar(carId);
    }

    public override void OnEpisodeBegin()
    {
        // Set car's position and rotation from the values specified in the Unity Inspector
        car.localPosition = startingPosition;
        car.localRotation = Quaternion.Euler(startingRotation);
        carRigidbody.velocity = Vector3.zero;
        carRigidbody.angularVelocity = Vector3.zero;
        lastMoveTime = Time.time;
        lastCheckpointTime = Time.time;
        lastPosition = car.position;

        trackCheckpoints.ResetCheckpoint(carId);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        CheckpointSingle nextCheckpoint = trackCheckpoints.GetNextCheckpoint(carId);
        if (nextCheckpoint != null)
        {
            Vector3 checkpointDirection = nextCheckpoint.transform.position - car.position;
            sensor.AddObservation(checkpointDirection.normalized);
            sensor.AddObservation(carRigidbody.velocity);
            AddReward(-0.01f); // Time penalty to encourage faster completion
        }
        else
        {
            // Add zero observations if no checkpoint is found to maintain observation size
            sensor.AddObservation(Vector3.zero.normalized);
            sensor.AddObservation(Vector3.zero);
            Debug.LogError("No next checkpoint found. Adding zero observations to maintain size.");
        }
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        if (Vector3.Distance(car.position, lastPosition) > movementThreshold)
        {
            lastMoveTime = Time.time;
            lastPosition = car.position;
        }

        if (Time.time - lastMoveTime > timeThreshold)
        {
            AddReward(-5.0f);
            EndEpisode();
        }

        // Handle discrete actions for car control
        switch (actionBuffers.DiscreteActions[0])
        {
            case 1:
                carController.GoForward();
                break;
            case 2:
                carController.GoReverse();
                break;
        }
        switch (actionBuffers.DiscreteActions[1])
        {
            case 1:
                carController.TurnLeft();
                break;
            case 2:
                carController.TurnRight();
                break;
        }
        if (actionBuffers.DiscreteActions[2] == 1)
        {
            carController.Handbrake();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActions = actionsOut.DiscreteActions;
        discreteActions[0] = Input.GetKey(KeyCode.W) ? 1 : Input.GetKey(KeyCode.S) ? 2 : 0;
        discreteActions[1] = Input.GetKey(KeyCode.A) ? 1 : Input.GetKey(KeyCode.D) ? 2 : 0;
        discreteActions[2] = Input.GetKey(KeyCode.Space) ? 1 : 0;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Checkpoint")) return;

        CheckpointSingle checkpoint = other.GetComponent<CheckpointSingle>();
        if (checkpoint == null)
        {
            Debug.LogError("CheckpointSingle component missing on collided object: " + other.gameObject.name);
            return;
        }

        int checkpointIndex = checkpoint.GetIndex();
        bool isCorrect = checkpoint.IsCorrectCheckpoint(carId);
        bool hasBeenPassed = trackCheckpoints.HasCheckpointBeenPassed(carId, checkpointIndex);

        if (isCorrect && !hasBeenPassed)
        {
            // Calculate the time taken to reach this checkpoint
            float currentTime = Time.time;
            float timeSinceLastCheckpoint = currentTime - lastCheckpointTime;

            // Reward the agent based on the time taken to reach this checkpoint
            // You might want to adjust this formula based on your requirements
            float timeBasedReward = timeSinceLastCheckpoint/5.0f; // Example formula
            AddReward(timeBasedReward);

            // Update last checkpoint time
            lastCheckpointTime = currentTime;

            // Increment the reward based on how many checkpoints have been passed so far.
            float reward = 1.5f * (checkpointIndex + 1); // Update reward calculation here
            AddReward(reward);
            //Debug.Log($"Checkpoint {checkpointIndex} passed, reward: {reward}, time-based reward: {timeBasedReward}");

            // Mark the checkpoint as passed and increment the checkpointsPassed count.
            trackCheckpoints.MarkCheckpointAsPassed(carId, checkpointIndex);
            trackCheckpoints.AdvanceToNextCheckpoint(carId); // Only advance if the checkpoint is correctly passed

            if (checkpoint.IsLastCheckpoint(carId))
            {
                AddReward(5.0f); // Bonus for finishing
                Debug.Log("Course completed. Ending episode.");
                EndEpisode();
            }
        }
        else // Handle the car moving back over a passed checkpoint
        {
            AddReward(-0.5f); // Penalize for moving back over a passed checkpoint.
            //Debug.Log("Car moved back over a previously passed checkpoint: " + checkpointIndex);
        }
    }


    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.TryGetComponent<Wall>(out Wall wall))
        {
            //Debug.Log("Ow, collision detected!");

            AddReward(-0.5f); // Penalty for hitting a wall
            //EndEpisode(); // End the episode on collision
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        // Check if the collision object has a tag named "Wall"
        if (collision.gameObject.TryGetComponent<Wall>(out Wall wall))
        { 
                // Apply a small penalty for each frame the car stays in contact with a wall
                AddReward(-0.01f);
        }
        
    }
}
