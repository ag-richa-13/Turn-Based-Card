// (Full file — replace your existing GameManager.cs with this version)
// Minor parts unchanged; only the end-of-game branch and safety tweaks are shown in full here.

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    [Header("Match Settings")]
    public int totalTurns = 6;
    public float turnTime = 30f;

    private int currentTurn = 0;

    // stored when clients press Play (not revealed to other player yet)
    private Dictionary<string, int[]> playLockBuffer = new Dictionary<string, int[]>();

    // temporary buffer used for resolution (copied from playLockBuffer when revealing)
    private Dictionary<string, int[]> revealBuffer = new Dictionary<string, int[]>();

    // who pressed EndTurn this turn
    private HashSet<string> endedPlayersThisTurn = new HashSet<string>();

    private Coroutine turnTimerCoroutine;

    private void Awake()
    {
        Instance = this;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        StartCoroutine(WaitForPlayersThenStart());
    }

    IEnumerator WaitForPlayersThenStart()
    {
        Debug.Log("Waiting for players...");
        while (true)
        {
            var players = FindObjectsByType<PlayerScript>(FindObjectsSortMode.None);
            if (players.Length >= 2) break;
            yield return new WaitForSeconds(0.25f);
        }

        Debug.Log("Players present. Starting match.");
        DealInitialHands();
        yield return new WaitForSeconds(0.12f);
        BroadcastHands();

        var gs = new GameStartMsg()
        {
            action = "gameStart",
            playerIds = GetPlayerIds(),
            totalTurns = totalTurns,
            hostIp = GetLocalIPAddress()
        };
        RpcSendJsonToClients(JsonUtility.ToJson(gs));
        GameEventManager.Instance.RaiseGameStart();

        currentTurn = 1;
        BroadcastTurnStart();
        turnTimerCoroutine = StartCoroutine(ServerTurnTimer());
    }

    #region Deck & hands

    void DealInitialHands()
    {
        var players = FindObjectsByType<PlayerScript>(FindObjectsSortMode.None);
        foreach (var p in players)
        {
            p.hand.Clear();
            for (int i = 0; i < 3; i++)
            {
                var card = CardDatabase.Instance.cards[UnityEngine.Random.Range(0, CardDatabase.Instance.cards.Count)];
                p.hand.Add(card.id);
            }
            p.score = 0;
        }
    }

    void DrawForAllPlayers(int count)
    {
        var players = FindObjectsByType<PlayerScript>(FindObjectsSortMode.None);
        foreach (var p in players)
        {
            for (int i = 0; i < count; i++)
            {
                var card = CardDatabase.Instance.cards[UnityEngine.Random.Range(0, CardDatabase.Instance.cards.Count)];
                p.hand.Add(card.id);
            }
        }
    }

    void BroadcastHands()
    {
        var players = FindObjectsByType<PlayerScript>(FindObjectsSortMode.None);
        List<HandSyncEntry> all = new List<HandSyncEntry>();
        foreach (var p in players)
        {
            all.Add(new HandSyncEntry() { playerId = p.playerId, cardIds = p.hand.ToArray() });
        }
        var msg = new HandSyncMessage() { action = "syncHands", hands = all.ToArray() };
        RpcSendJsonToClients(JsonUtility.ToJson(msg));
    }

    string[] GetPlayerIds()
    {
        var players = FindObjectsByType<PlayerScript>(FindObjectsSortMode.None);
        List<string> ids = new List<string>();
        foreach (var p in players) ids.Add(p.playerId);
        return ids.ToArray();
    }

    #endregion

    #region Server JSON receiver

    public void ServerReceiveJson(NetworkConnection conn, string jsonPayload)
    {
        if (string.IsNullOrEmpty(jsonPayload)) return;
        var wrapper = JsonUtility.FromJson<ActionWrapper>(jsonPayload);
        if (wrapper == null || string.IsNullOrEmpty(wrapper.action)) return;

        switch (wrapper.action)
        {
            case "playLock":
                {
                    var pl = JsonUtility.FromJson<PlayLockMsg>(jsonPayload);
                    HandlePlayLock(conn, pl.cardIds);
                    break;
                }
            case "endTurn":
                {
                    var end = JsonUtility.FromJson<EndTurnMsg>(jsonPayload);
                    HandleEndTurn(end.playerId);
                    break;
                }
            default:
                Debug.Log($"ServerReceiveJson: unhandled action: {wrapper.action}");
                break;
        }
    }

    void HandlePlayLock(NetworkConnection conn, int[] cardIds)
    {
        var ps = conn.identity?.GetComponent<PlayerScript>();
        if (ps == null)
        {
            Debug.LogWarning("HandlePlayLock: missing PlayerScript on connection");
            return;
        }

        // store player's chosen cards (server-side lock)
        playLockBuffer[ps.playerId] = cardIds ?? new int[0];

        // optionally inform clients that this player has locked selection (not revealing the cards)
        RpcSendJsonToClients(JsonUtility.ToJson(new NetworkGeneric() { action = "playLocked", playerId = ps.playerId }));
    }

    void HandleEndTurn(string playerId)
    {
        if (!endedPlayersThisTurn.Contains(playerId))
            endedPlayersThisTurn.Add(playerId);

        // notify clients which player pressed EndTurn
        RpcSendJsonToClients(JsonUtility.ToJson(new NetworkGeneric() { action = "endTurn", playerId = playerId }));
        GameEventManager.Instance.RaisePlayerEndedTurn(playerId);

        var players = FindObjectsByType<PlayerScript>(FindObjectsSortMode.None);
        if (players == null || players.Length == 0) return;

        // If all players ended -> reveal simultaneously and resolve
        if (endedPlayersThisTurn.Count >= players.Length)
        {
            BroadcastRevealsAndResolve();
            return;
        }

        // If not all ended: activate the other player for their turn (host-first flow)
        foreach (var p in players)
        {
            if (p.playerId != playerId)
            {
                var msg = new TurnStartMessage()
                {
                    action = "turnStart",
                    turn = currentTurn,
                    totalTurns = totalTurns,
                    activePlayerId = p.playerId
                };
                RpcSendJsonToClients(JsonUtility.ToJson(msg));
                GameEventManager.Instance.RaiseTurnStart(currentTurn);
                break;
            }
        }
    }

    void BroadcastRevealsAndResolve()
    {
        var players = FindObjectsByType<PlayerScript>(FindObjectsSortMode.None);
        // Ensure every player has an entry (empty if none locked)
        foreach (var p in players)
        {
            if (!playLockBuffer.ContainsKey(p.playerId))
                playLockBuffer[p.playerId] = new int[0];
        }

        // Broadcast revealCards for all players (simultaneous from server's perspective)
        foreach (var kv in playLockBuffer)
        {
            var revealMsg = new RevealBroadcast() { action = "revealCards", playerId = kv.Key, cardIds = kv.Value };
            RpcSendJsonToClients(JsonUtility.ToJson(revealMsg));
            GameEventManager.Instance.RaiseRevealCards(JsonUtility.ToJson(revealMsg));
        }

        // copy to revealBuffer for resolution
        revealBuffer = new Dictionary<string, int[]>(playLockBuffer);

        // stop timer if running
        if (turnTimerCoroutine != null) { StopCoroutine(turnTimerCoroutine); turnTimerCoroutine = null; }

        ResolveCombat();

        // clear per-turn trackers
        playLockBuffer.Clear();
        revealBuffer.Clear();
        endedPlayersThisTurn.Clear();
    }

    #endregion

    #region Resolve & scoring

    void ResolveCombat()
    {
        var players = FindObjectsByType<PlayerScript>(FindObjectsSortMode.None);
        var gain = new Dictionary<string, int>();
        foreach (var p in players) gain[p.playerId] = 0;

        foreach (var entry in revealBuffer)
        {
            foreach (var cid in entry.Value)
            {
                var card = CardDatabase.Instance.GetById(cid);
                if (card == null) continue;
                int power = card.power;
                if (card.ability != null && card.ability.type == "DoublePower")
                    power *= card.ability.value;

                gain[entry.Key] += power;

                if (card.ability != null && card.ability.type == "GainPoints")
                    gain[entry.Key] += card.ability.value;
            }
        }

        // StealPoints resolution
        foreach (var entry in revealBuffer)
        {
            foreach (var cid in entry.Value)
            {
                var card = CardDatabase.Instance.GetById(cid);
                if (card == null) continue;
                if (card.ability != null && card.ability.type == "StealPoints")
                {
                    foreach (var other in new List<string>(gain.Keys))
                    {
                        if (other == entry.Key) continue;
                        int steal = card.ability.value;
                        gain[other] = Mathf.Max(0, gain[other] - steal);
                        gain[entry.Key] += steal;
                    }
                }
            }
        }

        var map = new Dictionary<string, PlayerScript>();
        foreach (var p in players) map[p.playerId] = p;

        foreach (var kv in gain)
        {
            if (map.TryGetValue(kv.Key, out var ps)) ps.score += kv.Value;
        }

        BroadcastScores();

        // advance turn
        currentTurn++;
        if (currentTurn > totalTurns)
        {
            // compute winner and send full gameEnd message (avoid sending minimal JSON)
            string winnerId = "";
            int best = int.MinValue;
            List<ScoreEntry> scoresList = new List<ScoreEntry>();
            foreach (var p in players)
            {
                scoresList.Add(new ScoreEntry() { playerId = p.playerId, score = p.score });
                if (p.score > best) { best = p.score; winnerId = p.playerId; }
                else if (p.score == best) { /* tie: keep first */ }
            }

            var ge = new GameEndMsg() { action = "gameEnd", winnerId = winnerId, scores = scoresList.ToArray() };
            RpcSendJsonToClients(JsonUtility.ToJson(ge));
            GameEventManager.Instance.RaiseGameEnd();
            return;
        }

        // draw for next turn
        DrawForAllPlayers(1);
        BroadcastHands();

        // start next turn (host-first)
        BroadcastTurnStart();
        turnTimerCoroutine = StartCoroutine(ServerTurnTimer());
    }

    void BroadcastScores()
    {
        var players = FindObjectsByType<PlayerScript>(FindObjectsSortMode.None);
        List<ScoreEntry> list = new List<ScoreEntry>();
        foreach (var p in players) list.Add(new ScoreEntry() { playerId = p.playerId, score = p.score });

        RpcSendJsonToClients(JsonUtility.ToJson(new ScoreMessage() { action = "scoreUpdate", scores = list.ToArray() }));
    }

    #endregion

    #region Timer / Turn Start

    IEnumerator ServerTurnTimer()
    {
        float t = turnTime;
        while (t > 0f)
        {
            yield return new WaitForSeconds(1f);
            t -= 1f;
            RpcSendJsonToClients(JsonUtility.ToJson(new TimerMsg() { action = "timerTick", secondsLeft = (int)t }));
        }

        // timer expired -> mark all players who didn't end as ended
        var players = FindObjectsByType<PlayerScript>(FindObjectsSortMode.None);
        foreach (var p in players)
        {
            if (!endedPlayersThisTurn.Contains(p.playerId))
                endedPlayersThisTurn.Add(p.playerId);
        }

        // now broadcast reveals and resolve (BroadcastRevealsAndResolve checks playLockBuffer and fills missing)
        BroadcastRevealsAndResolve();
        yield break;
    }

    void BroadcastTurnStart()
    {
        // clear per-turn trackers
        playLockBuffer.Clear();
        revealBuffer.Clear();
        endedPlayersThisTurn.Clear();

        var players = FindObjectsByType<PlayerScript>(FindObjectsSortMode.None);
        if (players == null || players.Length == 0) return;

        // pick host as first (connectionId sort)
        var list = new List<(int connId, PlayerScript player)>();
        foreach (var p in players)
        {
            int connId = -1;
            if (p.connectionToClient != null) connId = p.connectionToClient.connectionId;
            else connId = (int)p.netId;
            list.Add((connId, p));
        }
        list.Sort((a, b) => a.connId.CompareTo(b.connId));

        string hostPlayerId = list[0].player.playerId;

        var msg = new TurnStartMessage()
        {
            action = "turnStart",
            turn = currentTurn,
            totalTurns = totalTurns,
            activePlayerId = hostPlayerId
        };

        RpcSendJsonToClients(JsonUtility.ToJson(msg));
        GameEventManager.Instance.RaiseTurnStart(currentTurn);
    }

    #endregion

    #region RPC helper & JSON classes

    [ClientRpc]
    void RpcSendJsonToClients(string json)
    {
        ClientSideMessageHandler.HandleReceive(json);
    }

    [Serializable] class ActionWrapper { public string action; }
    [Serializable] class PlayLockMsg { public string action; public int[] cardIds; }
    [Serializable] class EndTurnMsg { public string action; public string playerId; }
    [Serializable] class RevealBroadcast { public string action; public string playerId; public int[] cardIds; }
    [Serializable] public class TurnStartMessage { public string action; public int turn; public int totalTurns; public string activePlayerId; }
    [Serializable] public class GameStartMsg { public string action; public string[] playerIds; public int totalTurns; public string hostIp; }

    [Serializable] public class ScoreEntry { public string playerId; public int score; }
    [Serializable] class ScoreMessage { public string action; public ScoreEntry[] scores; }

    [Serializable] class TimerMsg { public string action; public int secondsLeft; }
    [Serializable] class NetworkGeneric { public string action; public string playerId; }

    [Serializable] public class HandSyncEntry { public string playerId; public int[] cardIds; }
    [Serializable] public class HandSyncMessage { public string action; public HandSyncEntry[] hands; }

    [Serializable] public class GameEndMsg { public string action; public string winnerId; public ScoreEntry[] scores; }

    #endregion

    string GetLocalIPAddress()
    {
        try
        {
            string localIP = "127.0.0.1";
            var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    localIP = ip.ToString();
                    break;
                }
            }
            return localIP;
        }
        catch { return "127.0.0.1"; }
    }
}