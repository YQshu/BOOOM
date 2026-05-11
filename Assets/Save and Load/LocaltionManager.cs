using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LocaltionManager : MonoBehaviour,ISaveManager
{
    public static LocaltionManager Instance;
    [SerializeField] private Checkpoint[] checkpoints;
    [SerializeField] private string closestCheckpointId;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }
    private void Start()
    {
        checkpoints = FindObjectsOfType<Checkpoint>();
    }
    public void RestartScene()
    {
        SaveManager.Instance.SaveGame();
        Scene scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.name);
    }

    public void LoadGame(GameData _data)
    {
        foreach (KeyValuePair<string, bool> pair in _data.checkpoints)
        {
            foreach (Checkpoint checkpoint in checkpoints)
            {
                if (checkpoint.name == pair.Key && checkpoint.activationStatus != pair.Value)
                {
                    checkpoint.ActivateCheckpoint();
                }
            }
        }
        closestCheckpointId = _data.currentCheckpointID;
        Invoke("PlacePlayerAtCloestCheckPoint", 0.1f);
    }

    private void PlacePlayerAtCloestCheckPoint()
    {
        foreach (Checkpoint checkpoint in checkpoints)
        {
            if (closestCheckpointId == checkpoint.checkpointId)
            {
                Player.Instance.transform.position = checkpoint.transform.position;
            }
        }
    }

    public void SaveGame(ref GameData _data)
    {
        _data.currentCheckpointID = FindClosestCheckpoint().checkpointId;
        _data.checkpoints.Clear();
        foreach (Checkpoint checkpoint in checkpoints)
        {
            Player.Instance.transform.position = checkpoint.transform.position;
        }
    }

    private Checkpoint FindClosestCheckpoint()
    {
        float closestDistance = Mathf.Infinity;
        Checkpoint ClosestCheckpoint = null;
        foreach (Checkpoint _checkpoint in checkpoints)
        {
            float distance = Vector2.Distance(transform.position, _checkpoint.transform.position);
            if(distance < closestDistance && _checkpoint.activationStatus == true)
            {
                closestDistance = distance;
                ClosestCheckpoint = _checkpoint;
            }
        }
        return ClosestCheckpoint;
    }
}
