using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UI_MainMenu : MonoBehaviour
{
    [SerializeField] private string sceneName = "SampleScene";
    [SerializeField] private GameObject continueButton;
    [SerializeField] private UI_FadeScreen fadeScreen;
    private void Start()
    {
        if (SaveManager.Instance.HasSaveGame() == false)
        {
            continueButton.SetActive(false);
        }
    }
    public void CotinueScene()
    {
        StartCoroutine(LoadSceneWithFade(1f));
    }
    public void NewGame()
    {
        SaveManager.Instance.DeleteSaveData();
        SceneManager.LoadScene(sceneName);
        StartCoroutine(LoadSceneWithFade(1f));
    }
    public void ExitGame()
    {
       // Application.Quit();
    }
    IEnumerator LoadSceneWithFade(float delay)
    {
        fadeScreen.FadeOut();
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(sceneName);
    }
}
