using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using Mirror;
using System.Collections;


public class GameUI : MonoBehaviour
{
    public static GameUI Instance;

    [Header("Top bar / Opponent")]
    public TextMeshProUGUI opponentNameText;
    public TextMeshProUGUI opponentScoreText;
    public TextMeshProUGUI opponentTimerText;
    public RectTransform opponentRevealPanel; // shows opponent revealed cards (horizontal)

    [Header("Middle")]
    public TextMeshProUGUI turnText;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI roomCodeText;
    public TextMeshProUGUI messageText;

    [Header("Player area")]
    public TextMeshProUGUI playerNameText;
    public TextMeshProUGUI playerScoreText;
    public TextMeshProUGUI playerTimerText;
    public RectTransform playerRevealPanel;   // shows local player's revealed cards
    public RectTransform handPanel;
    public GameObject cardPrefab;
    public Button playButton;      // appears when player has selection to play
    public Button endTurnButton;

    [Header("Footer")]
    public Button backButton;
    public Button endRoomButton;

    [Header("End Game Popup")]
    public GameObject popupEndGame;           // panel GameObject to enable/disable
    public TextMeshProUGUI popupWinnerText;
    public Button popupPlayAgainButton;
    public Button popupEndRoomButton;

    PlayerScript localPlayer;
    string activePlayerId;
    int currentTurn;
    int availableCost;

    // local flag to indicate player has locked selection this turn
    bool localPlayLocked = false;

    private void Awake() => Instance = this;

    private async void Start()
    {
        SubscribeEvents();
        SetupInitialUI();

        // button bindings
        if (endTurnButton != null) endTurnButton.onClick.AddListener(OnEndTurnClicked);
        if (playButton != null) playButton.onClick.AddListener(OnPlayClicked);
        if (backButton != null) backButton.onClick.AddListener(OnBackClicked);
        if (endRoomButton != null) endRoomButton.onClick.AddListener(OnEndRoomClicked);
        if (popupPlayAgainButton != null) popupPlayAgainButton.onClick.AddListener(OnPopupPlayAgainClicked);
        if (popupEndRoomButton != null) popupEndRoomButton.onClick.AddListener(OnPopupEndRoomClicked);

        if (playButton != null) playButton.gameObject.SetActive(false);
        if (endTurnButton != null) endTurnButton.interactable = false;

        await FindLocalPlayerAsync();
        UpdateNames();
        UpdateRoomIP();
    }

    void SetupInitialUI()
    {
        if (turnText != null) turnText.text = "Turn: -";
        if (costText != null) costText.text = "Cost: -";
        if (opponentScoreText != null) opponentScoreText.text = "Score: -";
        if (playerScoreText != null) playerScoreText.text = "Score: -";
        if (opponentTimerText != null) opponentTimerText.text = "";
        if (playerTimerText != null) playerTimerText.text = "";
        if (messageText != null) messageText.text = "Waiting for opponent...";
        if (popupEndGame != null) popupEndGame.SetActive(false);
    }

    void SubscribeEvents()
    {
        if (GameEventManager.Instance != null)
        {
            GameEventManager.Instance.OnGameStart += () =>
            {
                if (messageText != null) messageText.text = "Game Started";
                UpdateNames(); // <-- ADD THIS LINE
            };
            GameEventManager.Instance.OnTurnStart += (t) =>
            {
                UpdateNames(); // <-- ADD THIS LINE
                /* handled by SetActivePlayer via message */
            };
            GameEventManager.Instance.OnRevealCards += (json) => { HandleRevealJson(json); };
            GameEventManager.Instance.OnPlayerEndedTurn += (pId) => { /* optional */ };
            GameEventManager.Instance.OnGameEnd += () => { /* handled by ShowEndGame */ };
        }
    }

    async System.Threading.Tasks.Task FindLocalPlayerAsync()
    {
        while (localPlayer == null)
        {
            await System.Threading.Tasks.Task.Delay(100);
            var players = FindObjectsByType<PlayerScript>(FindObjectsSortMode.None);
            foreach (var p in players) if (p.isLocalPlayer) localPlayer = p;
        }
    }

    void UpdateNames()
    {
        var players = FindObjectsByType<PlayerScript>(FindObjectsSortMode.None);
        if (players.Length >= 1)
        {
            // find opponent
            foreach (var p in players)
            {
                if (!p.isLocalPlayer)
                {
                    if (opponentNameText != null) opponentNameText.text = p.playerId;
                }
                else
                {
                    if (playerNameText != null) playerNameText.text = p.playerId;
                }
            }
        }
    }

    void UpdateRoomIP()
    {
        var lobby = FindFirstObjectByType<LobbyManager>();
        if (lobby != null && roomCodeText != null) roomCodeText.text = lobby.roomCodeText.text;
    }

    // Called by ClientSideMessageHandler when turnStart arrives
    public void SetActivePlayer(string playerId, int turn)
    {
        activePlayerId = playerId;
        currentTurn = turn;
        availableCost = Mathf.Clamp(turn, 1, 6);

        if (turnText != null) turnText.text = $"Turn: {turn} / 6";
        if (costText != null) costText.text = $"Cost: {availableCost}";

        bool amIActive = (localPlayer != null && localPlayer.playerId == activePlayerId);
        if (endTurnButton != null) endTurnButton.interactable = amIActive;
        if (playButton != null) playButton.gameObject.SetActive(amIActive && localPlayer != null && localPlayer.selectedThisTurn.Count > 0 && !localPlayLocked);

        if (messageText != null) messageText.text = amIActive ? "Your turn" : $"Waiting for {activePlayerId}";

        // clear local selection when new turn begins
        if (localPlayer != null) localPlayer.selectedThisTurn.Clear();
        localPlayLocked = false;

        RefreshHandUI();
        ClearRevealPanelsForNewTurn();
    }

    public void ApplyHandSync(ClientSideMessageHandler.HandEntry[] hands)
    {
        if (localPlayer == null) return;
        foreach (var h in hands)
        {
            if (h.playerId == localPlayer.playerId)
            {
                localPlayer.hand = h.cardIds.ToList();
                localPlayer.selectedThisTurn.Clear();
                RefreshHandUI();
            }
        }
    }

    public void OnScoreUpdate(ClientSideMessageHandler.ScoreEntry[] scores)
    {
        // update both player score text
        foreach (var s in scores)
        {
            if (localPlayer != null && s.playerId == localPlayer.playerId)
            {
                if (playerScoreText != null) playerScoreText.text = $"Score: {s.score}";
            }
            else
            {
                if (opponentScoreText != null) opponentScoreText.text = $"Score: {s.score}";
            }
        }
    }

    // Timer tick arrives from server; handle UI and auto-end logic
    public void OnTimerTick(int sec)
    {
        bool amIActive = (localPlayer != null && localPlayer.playerId == activePlayerId);
        if (amIActive)
        {
            if (playerTimerText != null) playerTimerText.text = $"Time: {sec}s";
        }
        else
        {
            if (opponentTimerText != null) opponentTimerText.text = $"Time: {sec}s";
        }

        // When timer reaches 0, automatically end turn for the active player on each client
        if (sec <= 0 && amIActive && localPlayer != null)
        {
            if (endTurnButton != null && endTurnButton.interactable)
            {
                if (messageText != null) messageText.text = "Time up — ending your turn";
                // If player had not locked via Play, they auto-lock and then end
                if (!localPlayLocked && localPlayer.selectedThisTurn.Count > 0)
                {
                    // send play lock (server will receive selected cards)
                    localPlayer.SendPlayLock();
                    localPlayLocked = true;
                    // disable further selection
                    SetHandInteractable(false);
                }

                // send end turn
                localPlayer.SendEndTurn();

                if (endTurnButton != null) endTurnButton.interactable = false;
                if (playButton != null) playButton.gameObject.SetActive(false);
            }
        }
    }

    // Reveal JSON -> show cards in correct reveal panel
    // =========================
    //  FIXED REVEAL HANDLING
    // =========================

    // Public entry point from ClientSideMessageHandler
    public void HandleRevealJson(string json)
    {
        StartCoroutine(ProcessReveal(json));
    }

    IEnumerator ProcessReveal(string json)
    {
        // WAIT FOR LOCAL PLAYER
        while (localPlayer == null)
        {
            var players = FindObjectsByType<PlayerScript>(FindObjectsSortMode.None);
            foreach (var p in players)
                if (p.isLocalPlayer) localPlayer = p;

            yield return null;
        }

        var reveal = JsonUtility.FromJson<RevealTmp>(json);

        bool isLocal = reveal.playerId == localPlayer.playerId;
        RectTransform panel = isLocal ? playerRevealPanel : opponentRevealPanel;

        Debug.Log("PROCESS REVEAL: "
            + reveal.playerId + " | local=" + isLocal
            + " | cards=" + string.Join(",", reveal.cardIds));

        // Clear only that player's panel
        foreach (Transform t in panel)
            Destroy(t.gameObject);

        // Create UI cards
        foreach (int cid in reveal.cardIds)
        {
            var card = CardDatabase.Instance.GetById(cid);
            if (card == null) continue;

            var go = Instantiate(cardPrefab, panel);
            var ui = go.GetComponent<CardUI>();
            ui.Initialize(card, null);
        }
    }

    // Keeps both panels clean at start of new turn
    void ClearRevealPanelsForNewTurn()
    {
        if (playerRevealPanel != null)
            foreach (Transform t in playerRevealPanel)
                Destroy(t.gameObject);

        if (opponentRevealPanel != null)
            foreach (Transform t in opponentRevealPanel)
                Destroy(t.gameObject);
    }

    [System.Serializable]
    class RevealTmp
    {
        public string playerId;
        public int[] cardIds;
    }

    // Hand UI
    void RefreshHandUI()
    {
        if (handPanel != null)
        {
            foreach (Transform t in handPanel) Destroy(t.gameObject);
        }
        if (localPlayer == null) return;

        foreach (int id in localPlayer.hand)
        {
            var card = CardDatabase.Instance.GetById(id);
            var go = Instantiate(cardPrefab, handPanel);
            var ui = go.GetComponent<CardUI>();

            ui.Initialize(card, () => OnCardClicked(id, ui));

            if (localPlayer.selectedThisTurn.Contains(id))
                ui.ToggleSelected();
        }

        // after re-creating hand, apply interactivity state if locked
        SetHandInteractable(!localPlayLocked);
    }

    void OnCardClicked(int id, CardUI ui)
    {
        if (localPlayer == null) return;

        if (localPlayer.playerId != activePlayerId)
        {
            if (messageText != null) messageText.text = "Not your turn";
            return;
        }

        if (localPlayLocked)
        {
            if (messageText != null) messageText.text = "Selection locked. Press End Turn.";
            return;
        }

        // Toggle selection
        localPlayer.ToggleSelectCard(id);
        ui.ToggleSelected();

        // Show play button only if there's selection, it's player's turn, and not locked
        if (playButton != null) playButton.gameObject.SetActive(localPlayer.selectedThisTurn.Count > 0 && localPlayer.playerId == activePlayerId && !localPlayLocked);
    }

    // Play pressed: validate cost first, then send playLock to server and lock selection locally
    void OnPlayClicked()
    {
        if (localPlayer == null) return;
        if (localPlayer.selectedThisTurn.Count == 0) return;

        // Calculate total cost
        int used = 0;
        foreach (var id in localPlayer.selectedThisTurn)
        {
            var c = CardDatabase.Instance.GetById(id);
            if (c != null) used += c.cost;
        }

        int allowed = Mathf.Clamp(currentTurn, 1, 6);

        if (used > allowed)
        {
            if (messageText != null) messageText.text = $"Not enough cost. Used: {used}, Available: {allowed}";
            return;
        }

        // send playLock to server (server will store but not reveal)
        localPlayer.SendPlayLock();

        // lock selection locally
        localPlayLocked = true;
        SetHandInteractable(false);

        // hide play button (locked)
        if (playButton != null) playButton.gameObject.SetActive(false);

        if (messageText != null) messageText.text = "Selection locked. Press End Turn when ready.";
    }

    // End Turn pressed: send endTurn to server (server will wait for both players)
    void OnEndTurnClicked()
    {
        if (localPlayer == null) return;
        if (localPlayer.playerId != activePlayerId)
        {
            if (messageText != null) messageText.text = "Not your turn";
            return;
        }

        // if not locked yet but has selection, lock first (so server has cardIds)
        if (!localPlayLocked && localPlayer.selectedThisTurn.Count > 0)
        {
            localPlayer.SendPlayLock();
            localPlayLocked = true;
            SetHandInteractable(false);
        }

        // send end turn to server
        localPlayer.SendEndTurn();

        // disable local controls to avoid duplicate sends
        if (endTurnButton != null) endTurnButton.interactable = false;
        if (playButton != null) playButton.gameObject.SetActive(false);
    }

    void SetHandInteractable(bool on)
    {
        if (handPanel == null) return;
        foreach (Transform t in handPanel)
        {
            var btn = t.GetComponent<Button>();
            if (btn != null) btn.interactable = on;
        }
    }

    // Back/End Room/Popup actions
    void OnBackClicked()
    {
        if (NetworkClient.isConnected && NetworkServer.active)
        {
            NetworkManager.singleton.StopHost();
        }
        else if (NetworkClient.isConnected)
        {
            NetworkManager.singleton.StopClient();
        }
        SceneManager.LoadScene("LobbyScene");
    }

    void OnEndRoomClicked()
    {
        if (NetworkServer.active)
        {
            NetworkManager.singleton.StopHost();
        }
        else if (NetworkClient.isConnected)
        {
            NetworkManager.singleton.StopClient();
        }
        SceneManager.LoadScene("LobbyScene");
    }

    void OnPopupPlayAgainClicked()
    {
        if (NetworkServer.active) NetworkManager.singleton.StopHost();
        if (NetworkClient.isConnected) NetworkManager.singleton.StopClient();
        SceneManager.LoadScene("LobbyScene");
    }

    void OnPopupEndRoomClicked()
    {
        OnEndRoomClicked();
    }

    // Called by ClientSideMessageHandler when game end received
    public void ShowEndGame(string winnerId, ClientSideMessageHandler.ScoreEntry[] scores)
    {
        // Defensive null checks to avoid NullReferenceException during RPC handling
        if (popupEndGame != null)
        {
            popupEndGame.SetActive(true);
        }
        else
        {
            Debug.LogWarning("GameUI.ShowEndGame: popupEndGame is not assigned in inspector.");
        }

        if (popupWinnerText != null)
        {
            if (localPlayer != null && localPlayer.playerId == winnerId)
                popupWinnerText.text = "You Win!";
            else if (string.IsNullOrEmpty(winnerId))
                popupWinnerText.text = "Tie!";
            else
                popupWinnerText.text = $"{winnerId} Wins!";
        }
        else
        {
            Debug.LogWarning("GameUI.ShowEndGame: popupWinnerText is not assigned in inspector.");
        }

        // update final scores on UI (defensive)
        if (scores != null)
        {
            foreach (var s in scores)
            {
                if (localPlayer != null && s.playerId == localPlayer.playerId)
                {
                    if (playerScoreText != null) playerScoreText.text = $"Score: {s.score}";
                }
                else
                {
                    if (opponentScoreText != null) opponentScoreText.text = $"Score: {s.score}";
                }
            }
        }
        else
        {
            Debug.LogWarning("GameUI.ShowEndGame: scores array is null.");
        }

        if (messageText != null) messageText.text = "Game Ended";
    }
}