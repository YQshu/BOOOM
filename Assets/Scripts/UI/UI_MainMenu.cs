using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UI_MainMenu : MonoBehaviour
{
    [SerializeField] private string _gameSceneName = "SampleScene";

    public void ContinueGame()
    {
        SceneManager.LoadScene(_gameSceneName);
    }
    public void NewGame()
    {
        //增加savemanager的删除存档功能
        SceneManager.LoadScene(_gameSceneName);
    }
    public void ExitGame()
    {
        Application.Quit();
    }
}
