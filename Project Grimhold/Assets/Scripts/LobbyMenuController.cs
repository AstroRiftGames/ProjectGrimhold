using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyMenu : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI sessionCodeText;

    [Header("Scene")]
    [SerializeField] private string gameplayScene = "Game";

    private NetworkRunner _runner;

    public void Initialize(NetworkRunner runner)
    {
        _runner = runner;
        RefreshSessionCode();
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