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

    [Header("Class Selection UI")]
    [SerializeField]
    private Button meleeClassButton;

    [SerializeField]
    private Button rangedClassButton;

    private PlayerClassId _selectedClass = PlayerClassId.None;

    private void OnEnable()
    {
        createRoomButton.onClick.AddListener(CreateRoom);
        joinRoomButton.onClick.AddListener(JoinRoom);
        meleeClassButton.onClick.AddListener(SelectMelee);
        rangedClassButton.onClick.AddListener(SelectRanged);

        RefreshConnectionButtons();
    }

    private void OnDisable()
    {
        createRoomButton.onClick.RemoveListener(CreateRoom);
        joinRoomButton.onClick.RemoveListener(JoinRoom);
        meleeClassButton.onClick.RemoveListener(SelectMelee);
        rangedClassButton.onClick.RemoveListener(SelectRanged);
    }

    private void SelectMelee()
    {
        _selectedClass = PlayerClassId.Melee;
        RefreshConnectionButtons();
    }

    private void SelectRanged()
    {
        _selectedClass = PlayerClassId.Ranged;
        RefreshConnectionButtons();
    }

    private void RefreshConnectionButtons()
    {
        bool hasValidClass = PlayerJoinDataCodec.IsSupported(_selectedClass);
        createRoomButton.interactable = hasValidClass;
        joinRoomButton.interactable = hasValidClass;
    }

    private void SetUIInteractable(bool interactable)
    {
        roomCodeInput.interactable = interactable;
        meleeClassButton.interactable = interactable;
        rangedClassButton.interactable = interactable;

        if (interactable)
        {
            RefreshConnectionButtons();
        }
        else
        {
            createRoomButton.interactable = false;
            joinRoomButton.interactable = false;
        }
    }

    public async void CreateRoom()
    {
        if (!PlayerJoinDataCodec.IsSupported(_selectedClass))
        {
            _statusText.text = "Please select a class first.";
            return;
        }

        SetUIInteractable(false);
        _statusText.text = "Creating room...";

        string roomCode = GenerateRoomCode();

        try
        {
            await launcher.StartSessionAsync(
                roomCode,
                GameMode.Host,
                _selectedClass);
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
            SetUIInteractable(true);
        }
    }

    public async void JoinRoom()
    {
        if (!PlayerJoinDataCodec.IsSupported(_selectedClass))
        {
            _statusText.text = "Please select a class first.";
            return;
        }

        if (string.IsNullOrEmpty(roomCodeInput.text))
        {
            _statusText.text = "Please enter a room code.";
            return;
        }

        if (roomCodeInput.text.Length < 6)
        {
            _statusText.text = "Invalid room code";
            return;
        }

        SetUIInteractable(false);
        _statusText.text = "Joining...";

        try
        {
            await launcher.StartSessionAsync(
                roomCodeInput.text,
                GameMode.Client,
                _selectedClass);
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
            SetUIInteractable(true);
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