using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameData
{
    public Serializabledictionary<string, bool> checkpoints;
    public string currentCheckpointID;

    public GameData()
    {
        currentCheckpointID = string.Empty;
        checkpoints = new Serializabledictionary<string, bool>();
    }
}
