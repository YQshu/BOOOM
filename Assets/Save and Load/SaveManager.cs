using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class SaveManager : MonoBehaviour
{
    private GameData gameData;
    [SerializeField] private string fileName;
    public static SaveManager Instance;
    private List<ISaveManager> IsaveManager;
    private FileDataHandler dataHandler;

    [ContextMenu("Delete Save Data")]
    public void DeleteSaveData()
    {
        dataHandler = new FileDataHandler(Application.persistentDataPath, fileName);
        dataHandler.DeleteData();
    }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(Instance.gameObject);
        }
        else
        {
            Instance = this;
        }
    }
    private void Start()
    {
        dataHandler = new FileDataHandler(Application.persistentDataPath, fileName);
        IsaveManager = FindAllSaveManagers();
        LoadGame();
    }
    public void NewGame()
    {
        gameData = new GameData();
    }
    public void LoadGame()
    {
        gameData = dataHandler.Load();
        if (this.gameData == null)
        {
            Debug.LogError("No game data to load");
            NewGame();
        }
        foreach(ISaveManager saveManager in IsaveManager)
        {
            saveManager.LoadGame(gameData);
        }
    }
    public void SaveGame()
    {
        foreach (ISaveManager saveManager in IsaveManager)
        {
            saveManager.SaveGame(ref gameData);
        }
        dataHandler.Save(gameData);
    }
    private void OnApplicationQuit()
    {
        SaveGame();
    }
    private List<ISaveManager> FindAllSaveManagers()
    {
        IEnumerable<ISaveManager> saveManagers = FindObjectsOfType<MonoBehaviour>().OfType<ISaveManager>();
        return new List<ISaveManager>(saveManagers);
    }
    public bool HasSaveGame()
    {
        if(dataHandler.Load() != null)
        {
            return true;
        }
        return false;
    }
}
