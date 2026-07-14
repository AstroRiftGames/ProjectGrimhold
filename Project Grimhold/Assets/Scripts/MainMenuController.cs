using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [SerializeField]
    private FusionSessionLauncher launcher;

    [SerializeField]
    private TMP_InputField roomCodeInput;

    [SerializeField]
    private GameObject menuPanel;

    [SerializeField]
    private Button createRoomButton;

    [SerializeField]
    private Button joinRoomButton;

    private void OnEnable()
    {
        createRoomButton.onClick.AddListener(CreateRoom);
        joinRoomButton.onClick.AddListener(JoinRoom);
    }

    private void OnDisable()
    {
        createRoomButton.onClick.RemoveListener(CreateRoom);
        joinRoomButton.onClick.RemoveListener(JoinRoom);
    }

    public async void CreateRoom()
    {
        menuPanel.SetActive(false);

        string roomCode = GenerateRoomCode();

        await launcher.StartSessionAsync(
            roomCode,
            GameMode.Host);

    }

    public async void JoinRoom()
    {
        menuPanel.SetActive(false);

        await launcher.StartSessionAsync(
            roomCodeInput.text,
            GameMode.Client);
    }

    private string GenerateRoomCode()
    {
        return Random.Range(100000, 999999).ToString();
    }
}