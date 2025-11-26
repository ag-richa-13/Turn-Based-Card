using System;
using UnityEngine;

public class GameEventManager : MonoBehaviour
{
    public static GameEventManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public event Action OnGameStart;
    public event Action<int> OnTurnStart;
    public event Action<string> OnPlayerEndedTurn;
    public event Action<string> OnRevealCards;
    public event Action OnGameEnd;

    public void RaiseGameStart() => OnGameStart?.Invoke();
    public void RaiseTurnStart(int turn) => OnTurnStart?.Invoke(turn);
    public void RaisePlayerEndedTurn(string playerId) => OnPlayerEndedTurn?.Invoke(playerId);
    public void RaiseRevealCards(string json) => OnRevealCards?.Invoke(json);
    public void RaiseGameEnd() => OnGameEnd?.Invoke();
}
