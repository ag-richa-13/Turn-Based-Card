using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using TMPro;

/// <summary>
/// Updated LobbyManager: safe-guards against missing inspector assignment by finding the CustomNetworkManager at runtime.
/// Keeps previous countdown logic intact.
/// </summary>
public class LobbyManager : MonoBehaviour
{
    [Header("Network Manager")]
    public CustomNetworkManager networkManager;

    [Header("UI References")]
    public Button hostButton;
    public Button joinButton;
    public TMP_InputField ipInput;
    public TMP_Text statusText;
    public TMP_Text roomCodeText;
    public TMP_Text countdownText;
    public Button exitAppButton;

    [Header("Lobby Settings")]
    public int lobbyWaitSeconds = 60;

    Coroutine lobbyCountdownCoroutine;

    private void OnEnable()
    {
        CustomNetworkManager.OnPlayerConnected += OnAnyPlayerConnected;
    }

    private void OnDisable()
    {
        CustomNetworkManager.OnPlayerConnected -= OnAnyPlayerConnected;
    }

    private void Start()
    {
        // if inspector wasn't assigned (or was destroyed), try to find it at runtime
        if (networkManager == null)
            networkManager = FindObjectOfType<CustomNetworkManager>();

        if (hostButton != null) hostButton.onClick.AddListener(OnHostClicked);
        if (joinButton != null) joinButton.onClick.AddListener(OnJoinClicked);
        if (exitAppButton != null) exitAppButton.onClick.AddListener(OnExitAppClicked);

        if (countdownText != null) countdownText.text = "";
        UpdateStatus();
        UpdateRoomCode();
    }

    private void Update()
    {
        // keep room code updated (IP shown when host)
        UpdateRoomCode();
    }

    void OnHostClicked()
    {
        // try-find at runtime if null (avoid null error)
        if (networkManager == null)
        {
            networkManager = FindObjectOfType<CustomNetworkManager>();
            if (networkManager == null)
            {
                Debug.LogError("NetworkManager not found in scene. Make sure a CustomNetworkManager exists in the Lobby scene.");
                if (statusText != null) statusText.text = "NetworkManager missing!";
                return;
            }
        }

        networkManager.HostGame();

        if (statusText != null)
            statusText.text = "Hosting... Waiting for player";

        StartLobbyCountdown();
    }

    void OnJoinClicked()
    {
        if (networkManager == null)
        {
            networkManager = FindObjectOfType<CustomNetworkManager>();
            if (networkManager == null)
            {
                Debug.LogError("NetworkManager not found in scene. Make sure a CustomNetworkManager exists in the Lobby scene.");
                if (statusText != null) statusText.text = "NetworkManager missing!";
                return;
            }
        }

        string ip = ipInput != null ? ipInput.text.Trim() : "";
        if (string.IsNullOrEmpty(ip))
            ip = "localhost";

        networkManager.JoinGame(ip);

        if (statusText != null)
            statusText.text = "Connecting to host...";
    }

    void OnExitAppClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void StartLobbyCountdown()
    {
        if (lobbyCountdownCoroutine != null)
            StopCoroutine(lobbyCountdownCoroutine);

        int t = lobbyWaitSeconds > 0 ? lobbyWaitSeconds : 60;
        lobbyCountdownCoroutine = StartCoroutine(LobbyCountdown(t));
    }

    IEnumerator LobbyCountdown(int seconds)
    {
        int t = seconds;

        while (t > 0)
        {
            if (countdownText != null)
                countdownText.text = "Waiting for opponent: " + t + "s";

            yield return new WaitForSeconds(1f);
            t--;
        }

        lobbyCountdownCoroutine = null;

        if (statusText != null)
            statusText.text = "No opponent joined. Host stopped.";

        if (countdownText != null)
            countdownText.text = "No opponent joined.";

        if (networkManager != null)
            networkManager.StopHosting();
    }

    void OnAnyPlayerConnected()
    {
        if (lobbyCountdownCoroutine != null)
        {
            StopCoroutine(lobbyCountdownCoroutine);
            lobbyCountdownCoroutine = null;
        }

        if (statusText != null)
            statusText.text = "Opponent joined. Starting game...";

        if (countdownText != null)
            countdownText.text = "";
    }

    void UpdateRoomCode()
    {
        if (networkManager == null || roomCodeText == null)
            return;

        if (NetworkServer.active)
        {
            roomCodeText.text = "Room Code (IP): " + GetLocalIP();
        }
        else
        {
            roomCodeText.text = "Room Code (IP): -";
        }
    }

    void UpdateStatus()
    {
        if (statusText == null) return;

        if (NetworkServer.active && NetworkClient.isConnected)
            statusText.text = "Hosting... Waiting for opponent";
        else if (NetworkClient.isConnected)
            statusText.text = "Connecting...";
        else
            statusText.text = "Not connected";
    }

    string GetLocalIP()
    {
        try
        {
            string localIP = "127.0.0.1";
            var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    return ip.ToString();
            }
            return localIP;
        }
        catch { return "127.0.0.1"; }
    }
}
