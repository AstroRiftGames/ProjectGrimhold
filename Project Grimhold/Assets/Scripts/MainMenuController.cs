using Fusion;
using System;
using TMPro;
using Unity.VisualScripting;
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

    [SerializeField]
    private GameObject lobbyPanel;

    [SerializeField]
    private LobbyMenuController lobbyMenu;

    [SerializeField]
    private TextMeshProUGUI _statusText;

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
        roomCodeInput.interactable = false;

        _statusText.text = "Creating room...";

        string roomCode = GenerateRoomCode();

        try
        {
            await launcher.StartSessionAsync(
                roomCode,
                GameMode.Host);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error creating room: {ex.Message}");
            _statusText.text = $"Failed to create room: {ex.Message}";
        }

        if (launcher.Runner != null)
        {
            ShowLobby();
        }
        else
        {
            createRoomButton.interactable = true;
            joinRoomButton.interactable = true;
            roomCodeInput.interactable = true;
        }
    }

    public async void JoinRoom()
    {
        if(string.IsNullOrEmpty(roomCodeInput.text))
        {
            _statusText.text = "Please enter a room code.";
            return;
        }

        if(roomCodeInput.text.Length < 6)
        {
            _statusText.text = "Invalid room code";
            return;
        }

        createRoomButton.interactable = false;
        joinRoomButton.interactable = false;
        roomCodeInput.interactable = false;

        _statusText.text = "Joining...";

        try
        {
            await launcher.StartSessionAsync(
                roomCodeInput.text,
                GameMode.Client);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error joining room: {ex.Message}");
            _statusText.text = $"Failed to join room: {ex.Message}";
        }

        if (launcher.Runner != null)
        {
            ShowLobby();
        }
        else
        {
            createRoomButton.interactable = true;
            joinRoomButton.interactable = true;
            roomCodeInput.interactable = true;

            _statusText.text = "";
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
        return UnityEngine.Random.Range(100000, 999999).ToString();
    }
}