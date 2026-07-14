using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
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

    [SerializeField]
    private GameObject lobbyPanel;

    [SerializeField]
    private LobbyMenuController lobbyMenu;

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
        createRoomButton.interactable = false;
        joinRoomButton.interactable = false;

        string roomCode = GenerateRoomCode();

        await launcher.StartSessionAsync(
            roomCode,
            GameMode.Host);

        if (launcher.Runner != null)
        {
            ShowLobby();
        }
        else
        {
            createRoomButton.interactable = true;
            joinRoomButton.interactable = true;
        }
    }

    public async void JoinRoom()
    {
        createRoomButton.interactable = false;
        joinRoomButton.interactable = false;

        await launcher.StartSessionAsync(
            roomCodeInput.text,
            GameMode.Client);

        if (launcher.Runner != null)
        {
            ShowLobby();
        }
        else
        {
            createRoomButton.interactable = true;
            joinRoomButton.interactable = true;
        }
    }

    private void ShowLobby()
    {
        menuPanel.SetActive(false);
        lobbyPanel.SetActive(true);

        lobbyMenu.Initialize(launcher.Runner);
    }

    private string GenerateRoomCode()
    {
        return Random.Range(100000, 999999).ToString();
    }
}