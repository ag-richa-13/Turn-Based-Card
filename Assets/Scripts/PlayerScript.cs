using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PlayerScript : NetworkBehaviour
{
    [SyncVar] public string playerId;
    public List<int> hand = new List<int>();
    public List<int> selectedThisTurn = new List<int>();
    public int score = 0;

    private GameManager gm;
    private GameEventManager events;

    private void Awake()
    {
        gm = FindFirstObjectByType<GameManager>();
        events = GameEventManager.Instance;
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        playerId = $"P{UnityEngine.Random.Range(1000, 9999)}";
        CmdRegisterPlayer(playerId);
    }

    [Command]
    void CmdRegisterPlayer(string jsonPlayerId)
    {
        playerId = jsonPlayerId;
        Debug.Log($"Server registered player {playerId}");
    }

    public void ToggleSelectCard(int cardId)
    {
        if (!isLocalPlayer) return;
        if (selectedThisTurn.Contains(cardId)) selectedThisTurn.Remove(cardId);
        else selectedThisTurn.Add(cardId);
    }

    // Player pressed Play -> send playLock to server (selected cards), but do NOT end turn
    public void SendPlayLock()
    {
        if (!isLocalPlayer) return;

        var msg = new NetworkPlayLock()
        {
            action = "playLock",
            cardIds = selectedThisTurn.ToArray()
        };
        string json = JsonUtility.ToJson(msg);
        CmdSendNetworkJson(json);
    }

    // End Turn action (sends endTurn)
    public void SendEndTurn()
    {
        if (!isLocalPlayer) return;

        var msg = new NetworkAction()
        {
            action = "endTurn",
            playerId = playerId
        };
        string json = JsonUtility.ToJson(msg);
        CmdSendNetworkJson(json);
    }

    // If you still want a direct reveal-from-client (not used in Option A) keep for fallback
    public void RevealSelectedCardsImmediate()
    {
        if (!isLocalPlayer) return;
        var msg = new NetworkReveal()
        {
            action = "revealCards",
            cardIds = selectedThisTurn.ToArray()
        };
        string json = JsonUtility.ToJson(msg);
        CmdSendNetworkJson(json);
    }

    [Command]
    void CmdSendNetworkJson(string jsonPayload)
    {
        GameManager.Instance.ServerReceiveJson(connectionToClient, jsonPayload);
    }

    [Serializable]
    public class NetworkAction { public string action; public string playerId; }

    [Serializable]
    public class NetworkReveal { public string action; public int[] cardIds; }

    [Serializable]
    public class NetworkPlayLock { public string action; public int[] cardIds; }
}