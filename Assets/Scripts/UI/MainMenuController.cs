using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 主菜单控制器。挂在 MainMenu 场景的 Canvas 根节点上。
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("按钮")]
    [SerializeField] private Button _startButton;
    [SerializeField] private Button _quitButton;

    [Header("场景")]
    [SerializeField] private string _gameSceneName = "SampleScene";

    [Header("淡入效果（可选）")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private float _fadeInDuration = 1f;

    private void Start()
    {
        _startButton?.onClick.AddListener(StartGame);
        _quitButton?.onClick.AddListener(QuitGame);

        if (_canvasGroup != null && _fadeInDuration > 0f)
            StartCoroutine(FadeIn());
    }

    private void StartGame()
    {
        SceneManager.LoadScene(_gameSceneName);
    }

    private void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private IEnumerator FadeIn()
    {
        _canvasGroup.alpha = 0f;
        float elapsed = 0f;
        while (elapsed < _fadeInDuration)
        {
            elapsed += Time.deltaTime;
            _canvasGroup.alpha = Mathf.Clamp01(elapsed / _fadeInDuration);
            yield return null;
        }
        _canvasGroup.alpha = 1f;
    }
}
