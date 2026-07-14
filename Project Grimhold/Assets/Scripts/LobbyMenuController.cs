using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyMenuController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI sessionCodeText;
    [SerializeField] private Button _startGameButton;
    [SerializeField] private GameObject _statusText;

    [Header("Scene")]
    [SerializeField] private string gameplayScene = "Game";

    private NetworkRunner _runner;

    private void OnEnable()
    {
        _startGameButton.onClick.AddListener(StartGame);
    }

    private void OnDisable()
    {
        _startGameButton.onClick.RemoveListener(StartGame);

    }

    public void Initialize(NetworkRunner runner)
    {
        _runner = runner;

        sessionCodeText.text = _runner.SessionInfo.Name;

        _startGameButton.gameObject.SetActive(_runner.IsServer);
        _statusText.gameObject.SetActive(!_runner.IsServer);

    }

    public void RefreshSessionCode()
    {
        if (_runner == null || sessionCodeText == null)
            return;

        sessionCodeText.text = _runner.SessionInfo.Name;
    }

    public async void StartGame()
    {
        if (_runner == null)
            return;

        // Sólo el Host puede iniciar la partida
        if (!_runner.IsServer)
            return;

        await _runner.LoadScene(
            SceneRef.FromIndex(SceneUtility.GetBuildIndexByScenePath(gameplayScene)),
            LoadSceneMode.Single);
    }
}