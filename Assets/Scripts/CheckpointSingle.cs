using UnityEngine;

public class CheckpointSingle : MonoBehaviour
{
    [SerializeField]
    private int checkpointIndex;  // Index to set manually in the Unity Inspector for each checkpoint

    [SerializeField]
    private TrackCheckpoints trackCheckpoints; // Reference to the TrackCheckpoints manager

    // Check if this checkpoint is the correct one based on its index
    public bool IsCorrectCheckpoint(int carId)
    {
        // Ensure there is a valid reference to TrackCheckpoints before attempting to check
        if (trackCheckpoints == null)
        {
            Debug.LogError("TrackCheckpoints reference not set on " + gameObject.name, this);
            return false;
        }
        return trackCheckpoints.CanPassCheckpoint(carId, checkpointIndex);
    }

    // Determine if this is the last checkpoint
    public bool IsLastCheckpoint(int carId)
    {
        if (trackCheckpoints == null)
        {
            Debug.LogError("TrackCheckpoints reference not set on " + gameObject.name, this);
            return false;
        }
        return trackCheckpoints.IsLastCheckpoint(carId, checkpointIndex);
    }

    // Accessor for the index, if needed externally
    public int GetIndex()
    {
        return checkpointIndex;
    }
}
