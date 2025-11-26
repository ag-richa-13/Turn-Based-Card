# TurnBasedCard — 1v1 Turn-Based Multiplayer Card Game (Prototype)

A 1v1, 6-turn, simultaneously-reveal, turn-based card game prototype built in Unity.  
Target: **Android**  
Networking: **Mirror**  
Cards & gameplay messages: **JSON-driven**  
Architecture: **Event-based**, modular, decoupled.

---

## 📂 Deliverables in This Repository

- `UnityProject/` — full project (excluding Library/Temp)
- `Assets/Scripts/` — all core C# scripts
- `Assets/Data/cards.json` — card definitions
- `Builds/TurnBasedCard_v1.apk` — Android build (optional)
- `Media/demo.mp4` — demo video (full 6-turn match)
- `README.md`

---

## 🔌 Networking Solution

**Mirror**

- JSON messages only (no raw RPC ints/strings for gameplay).
- CustomNetworkManager handles room creation, room codes, matchmaking.
- ClientSideMessageHandler serializes gameplay events → JSON → sends to other clients.

---

## 🧩 JSON-Driven Cards

Cards are defined in `Assets/Data/cards.json`.  
Example:

```json
{
  "id": 1,
  "name": "Shield Bearer",
  "cost": 2,
  "power": 3,
  "ability": {
    "type": "GainPoints",
    "value": 2
  }
}
```

Ability types implemented:

- `GainPoints`
- `StealPoints`
- `DoublePower`
- `DrawExtraCard`
- `DiscardOpponentRandomCard`
- `DestroyOpponentCardInPlay`

Abilities are modular via resolver in GameManager.

---

## 🎮 Gameplay Rules Implemented

- Match length: **6 turns**
- Deck size: **12 cards** (shuffled)
- Starting hand: **3 cards**
- Each turn: draw **+1 card**
- Turn cost:
  - Turn 1 → 1 cost
  - Turn 2 → 2 cost
  - …
  - Turn 6 → 6 cost
- Player can play **multiple cards** as long as total cost ≤ turn cost
- Turn timer: **30 seconds**
  - Auto-end on timeout
  - Manual **End Turn** button
- Cards from both players are revealed **simultaneously**
- Winner: highest score after 6 turns

---

## 🔔 Event-Driven Architecture

Internal events:

```
GameStart
TurnStart
PlayerEndedTurn
RevealCards
GameEnd
```

Networking sends JSON messages like:

### Game Start

```json
{ "action": "gameStart", "playerIds": ["P1", "P2"], "totalTurns": 6 }
```

### End Turn

```json
{ "action": "endTurn", "playerId": "P1" }
```

### Reveal Cards

```json
{ "action": "revealCards", "playerId": "P1", "cardIds": [2, 5] }
```

### Game End

```json
{ "action": "gameEnd", "scores": { "P1": 18, "P2": 15 }, "winner": "P1" }
```

---

## 🏗 Project Structure

```
Assets/
  Scripts/
    CardData.cs
    CardDatabase.cs
    CardUI.cs
    ClientSideMessageHandler.cs
    CustomNetworkManager.cs
    GameEventManager.cs
    GameManager.cs
    GameUI.cs
    LobbyManager.cs
    PlayerScript.cs
  Data/
    cards.json
  Prefabs/
    CardPrefab.prefab
    PlayerPrefab.prefab
  Scenes/
    LobbyScene.unity
    GameScene.unity
Builds/
Media/
README.md
```

---

## ▶️ Running in Unity (Editor)

1. Open project in **Unity 6000.2**
2. Open `LobbyScene`
3. Start two instances:
   - Instance 1 → **Host**
   - Instance 2 → **Join** (enter room code)
4. When both players join → match starts
5. Play full 6-turn cycle

---

## 📱 Running on Android

1. Install APK on two devices
2. Player A → Host
3. Player B → Join using room code
4. Play match

---

## 📦 Building APK

1. File → Build Settings → Switch to Android
2. Add:
   - LobbyScene
   - GameScene
3. Build → `Builds/TurnBasedCard_v1.apk`

---

## ✔️ Testing Checklist

- [ ] Two players connect via Host/Join
- [ ] Both receive gameStart JSON
- [ ] Deck = 12, Hand = 3
- [ ] TurnStart triggers +1 card, cost increments
- [ ] Timer = 30s; auto-end works
- [ ] Player can play multiple cards ≤ cost
- [ ] RevealCards JSON sent/received
- [ ] Abilities apply correctly
- [ ] After 6 turns → gameEnd event + winner popup

---

## 📝 Notes

- JSON messages only (no raw RPC arguments)
- Modular ability system
- Event-driven architecture
- Basic reconnection supported

---

## 👤 Developer

Richa Agrawal  
Unity | C# | Multiplayer Systems
