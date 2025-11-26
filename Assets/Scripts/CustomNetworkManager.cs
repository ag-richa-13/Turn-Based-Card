using System;
using System.Linq;
using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;

/// <summary>
/// Custom NetworkManager helper:
/// - HostGame() to start host (stays in LobbyScene)
/// - JoinGame(ip) to start client
/// - When server sees 2+ connections AND still in lobby scene, it will ServerChangeScene(gameSceneName)
/// - Exposes event OnPlayerConnected so Lobby UI can react (stop countdown)
/// 
/// Place this component on the same GameObject you had NetworkManager on (or replace the existing one).
/// Set "gameSceneName" to the exact name of your Game Scene (e.g. "GameScene") in the inspector.
/// </summary>
public class CustomNetworkManager : NetworkManager
{
    // Name of the game scene that contains GameManager, GameUI etc.
    [Header("Custom Settings")]
    [Tooltip("Exact name of the gameplay scene (must be added to Build Settings).")]
    public string gameSceneName = "GameScene";

    // Lobby scene name (optional, used for safety)
    [Tooltip("Exact name of the lobby scene (where LobbyManager runs).")]
    public string lobbySceneName = "LobbyScene";

    // countdown duration (seconds) shown on host lobby UI (LobbyManager may start its own)
    public int defaultLobbyTimeoutSeconds = 60;

    // static event so LobbyManager (host client) can respond when a remote client connects
    public static event Action OnPlayerConnected;

    // server: called when a client connects (includes host local connection)
    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        base.OnServerConnect(conn);

        Debug.Log($"CustomNetworkManager: OnServerConnect called. connId={conn.connectionId}. connections={NetworkServer.connections.Count}");

        OnPlayerConnected?.Invoke();

        // Only start game when exactly 2 players (host + 1 client) are connected
        int connectionCount = NetworkServer.connections.Count;
        Debug.Log($"CustomNetworkManager: connectionCount={connectionCount}");

        // FIX: Only change scene when connectionCount == 2 (host + client)
        if (connectionCount == 2)
        {
            var active = SceneManager.GetActiveScene().name;
            Debug.Log($"CustomNetworkManager: active scene = {active}");
            if (active == lobbySceneName || active == "" || active == null)
            {
                Debug.Log("CustomNetworkManager: Enough players joined. Server will change scene to game scene now.");
                ServerChangeScene(gameSceneName);
            }
            else
            {
                Debug.Log("CustomNetworkManager: Server is not in lobby scene; not changing scenes.");
            }
        }
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        base.OnServerDisconnect(conn);
        Debug.Log($"CustomNetworkManager: OnServerDisconnect connId={conn.connectionId}. connections={NetworkServer.connections.Count}");
    }

    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();
        Debug.Log("CustomNetworkManager: OnClientDisconnect called.");
    }

    // Host start helper (called by LobbyManager)
    public void HostGame()
    {
        if (!NetworkClient.isConnected && !NetworkServer.active)
        {
            Debug.Log("CustomNetworkManager: Starting Host...");
            StartHost();
        }
    }

    // Join helper (called by LobbyManager)
    public void JoinGame(string ip)
    {
        if (NetworkClient.isConnected) return;
        networkAddress = ip;
        Debug.Log($"CustomNetworkManager: Starting Client to {ip}...");
        StartClient();
    }

    // Optional: Stop host/client helpers
    public void StopHosting()
    {
        if (NetworkServer.active && NetworkClient.isConnected)
        {
            StopHost();
        }
        else if (NetworkClient.isConnected)
        {
            StopClient();
        }
        else if (NetworkServer.active)
        {
            StopServer();
        }
    }
}