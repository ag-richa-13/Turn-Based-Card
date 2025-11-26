using System;
using UnityEngine;

/// <summary>
/// Central client-side JSON message parser (updated).
/// Handles playLocked, revealCards, turnStart, timerTick, syncHands, scoreUpdate, gameEnd
/// </summary>
public static class ClientSideMessageHandler
{
    [Serializable] public class ScoreEntry { public string playerId; public int score; }
    [Serializable] public class ScoreMsg { public string action; public ScoreEntry[] scores; }
    [Serializable] public class TimerMsg { public string action; public int secondsLeft; }
    [Serializable] public class HandEntry { public string playerId; public int[] cardIds; }
    [Serializable] public class HandMsg { public string action; public HandEntry[] hands; }
    [Serializable] public class TurnStartMsg { public string action; public int turn; public int totalTurns; public string activePlayerId; }
    [Serializable] public class EndTurnMsg { public string action; public string playerId; }
    [Serializable] public class RevealMsg { public string action; public string playerId; public int[] cardIds; }
    [Serializable] public class PlayLockedMsg { public string action; public string playerId; }
    [Serializable] public class GameStartMsg { public string action; public string[] playerIds; public int totalTurns; public string hostIp; }
    [Serializable] public class GameEndMsg { public string action; public string winnerId; public ScoreEntry[] scores; }

    public static void HandleReceive(string json)
    {
        if (string.IsNullOrEmpty(json)) return;

        var wrapper = JsonUtility.FromJson<ActionWrapper>(json);
        if (wrapper == null || string.IsNullOrEmpty(wrapper.action)) return;

        try
        {
            switch (wrapper.action)
            {
                case "gameStart":
                    {
                        var g = JsonUtility.FromJson<GameStartMsg>(json);
                        if (!string.IsNullOrEmpty(g.hostIp))
                        {
                            if (GameUI.Instance != null) GameUI.Instance.roomCodeText.text = $"Room IP: {g.hostIp}";
                        }
                        GameEventManager.Instance.RaiseGameStart();
                        break;
                    }

                case "turnStart":
                    {
                        var ts = JsonUtility.FromJson<TurnStartMsg>(json);
                        GameEventManager.Instance.RaiseTurnStart(ts.turn);
                        if (GameUI.Instance != null) GameUI.Instance.SetActivePlayer(ts.activePlayerId, ts.turn);
                        break;
                    }

                case "playLocked":
                    {
                        var pl = JsonUtility.FromJson<PlayLockedMsg>(json);
                        // optional: show "player locked" indicator in UI (reuse existing event)
                        GameEventManager.Instance.RaisePlayerEndedTurn(pl.playerId);
                        break;
                    }

                case "revealCards":
                    {
                        // forward reveal JSON to UI
                        GameEventManager.Instance.RaiseRevealCards(json);
                        break;
                    }

                case "endTurn":
                    {
                        var et = JsonUtility.FromJson<EndTurnMsg>(json);
                        GameEventManager.Instance.RaisePlayerEndedTurn(et.playerId);
                        break;
                    }

                case "timerTick":
                    {
                        var tm = JsonUtility.FromJson<TimerMsg>(json);
                        if (GameUI.Instance != null) GameUI.Instance.OnTimerTick(tm.secondsLeft);
                        break;
                    }

                case "scoreUpdate":
                    {
                        var sc = JsonUtility.FromJson<ScoreMsg>(json);
                        if (GameUI.Instance != null) GameUI.Instance.OnScoreUpdate(sc.scores);
                        break;
                    }

                case "syncHands":
                    {
                        var hs = JsonUtility.FromJson<HandMsg>(json);
                        if (GameUI.Instance != null) GameUI.Instance.ApplyHandSync(hs.hands);
                        break;
                    }

                case "gameEnd":
                    {
                        var ge = JsonUtility.FromJson<GameEndMsg>(json);
                        // defensive: ge might be null or fields null
                        if (ge == null)
                        {
                            Debug.LogWarning("ClientSideMessageHandler: gameEnd received but could not parse payload.");
                        }
                        else
                        {
                            if (GameUI.Instance != null)
                            {
                                // pass safely (GameUI will check for null UI elements)
                                GameUI.Instance.ShowEndGame(ge.winnerId, ge.scores ?? new ScoreEntry[0]);
                            }
                            else
                            {
                                Debug.Log("ClientSideMessageHandler: GameUI.Instance is null on gameEnd - skipping UI update.");
                            }
                        }
                        GameEventManager.Instance.RaiseGameEnd();
                        break;
                    }

                default:
                    {
                        Debug.Log($"ClientSideMessageHandler: Unhandled action = {wrapper.action}");
                        break;
                    }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"ClientSideMessageHandler.HandleReceive exception: {ex}");
        }
    }

    [Serializable] class ActionWrapper { public string action; }
}