using System.Collections.Generic;
using UnityEngine;

public class TrackCheckpoints : MonoBehaviour
{
    [SerializeField] private List<Transform> checkpointTransforms;
    [SerializeField] private List<GameObject> cars; // List of cars to track
    private List<CheckpointSingle> checkpointList;
    private Dictionary<int, int> carNextCheckpointIndex = new Dictionary<int, int>();
    private Dictionary<int, HashSet<int>> carPassedCheckpoints = new Dictionary<int, HashSet<int>>();

    void Start()
    {
        // Initialize checkpointList from checkpointTransforms
        checkpointList = new List<CheckpointSingle>();
        foreach (var transform in checkpointTransforms)
        {
            CheckpointSingle checkpoint = transform.GetComponent<CheckpointSingle>();
            if (checkpoint != null)
            {
                checkpointList.Add(checkpoint);
            }
            else
            {
                Debug.LogError("Transform does not have a CheckpointSingle component: " + transform.name);
            }
        }

        if (checkpointList == null || checkpointList.Count == 0)
        {
            Debug.LogError("Checkpoint list is not initialized or is empty", this);
        }
        else
        {
            Debug.Log("Checkpoints loaded: " + checkpointList.Count);
        }

        // Register each car with a unique ID
        for (int i = 0; i < cars.Count; i++)
        {
            RegisterCar(i);
        }
    }

    public CheckpointSingle GetNextCheckpoint(int carId)
    {
        if (!carNextCheckpointIndex.ContainsKey(carId))
        {
            Debug.LogError("Car ID not found: " + carId);
            return null;
        }

        int nextCheckpointIndex = carNextCheckpointIndex[carId];
        if (nextCheckpointIndex >= 0 && nextCheckpointIndex < checkpointList.Count)
        {
            return checkpointList[nextCheckpointIndex];
        }
        Debug.LogError("Next checkpoint index is out of range: " + nextCheckpointIndex);
        return null;
    }

    public bool CanPassCheckpoint(int carId, int index)
    {
        if (!carNextCheckpointIndex.ContainsKey(carId))
        {
            Debug.LogError("Car ID not found: " + carId);
            return false;
        }

        // Check if the index matches the next expected checkpoint and it hasn't been passed yet
        int nextCheckpointIndex = carNextCheckpointIndex[carId];
        return index == nextCheckpointIndex && !carPassedCheckpoints[carId].Contains(index);
    }

    public void AdvanceToNextCheckpoint(int carId)
    {
        if (!carNextCheckpointIndex.ContainsKey(carId))
        {
            Debug.LogError("Car ID not found: " + carId);
            return;
        }

        carNextCheckpointIndex[carId]++;
        if (carNextCheckpointIndex[carId] >= checkpointList.Count)
        {
            carNextCheckpointIndex[carId] = 0; // Optionally reset to the first checkpoint if looping
        }
    }

    public void MarkCheckpointAsPassed(int carId, int index)
    {
        if (!carPassedCheckpoints.ContainsKey(carId))
        {
            carPassedCheckpoints[carId] = new HashSet<int>();
        }

        carPassedCheckpoints[carId].Add(index);
    }

    public bool HasCheckpointBeenPassed(int carId, int index)
    {
        if (!carPassedCheckpoints.ContainsKey(carId))
        {
            return false;
        }

        return carPassedCheckpoints[carId].Contains(index);
    }

    public bool IsLastCheckpoint(int carId, int index)
    {
        return index == checkpointList.Count - 1;
    }

    public void ResetCheckpoint(int carId)
    {
        if (!carNextCheckpointIndex.ContainsKey(carId) || !carPassedCheckpoints.ContainsKey(carId))
        {
            Debug.LogError("Car ID not found: " + carId);
            return;
        }

        carNextCheckpointIndex[carId] = 0;
        carPassedCheckpoints[carId].Clear();
    }

    public void RegisterCar(int carId)
    {
        if (!carNextCheckpointIndex.ContainsKey(carId))
        {
            carNextCheckpointIndex[carId] = 0;
            carPassedCheckpoints[carId] = new HashSet<int>();
        }
    }
}
