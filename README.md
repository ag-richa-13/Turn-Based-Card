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
- `Assets/Resources/cards.json` — card definitions
- `Builds/TurnBasedCard.apk` — Android build (optional)
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

## 🖥️ UI Structure

### LobbyScene

- **NetworkManager**
- **LobbyManager** (with LobbyUI Script)
- **GameEvent**
- **Canvas**
  - Host Button
  - Join Button
  - Room Code Input Field (for client joining)
  - Room Code Display (shows code when host creates room)
  - Status Text
- **ExitGame**

### GameScene

- **GameManager**
- **GameUI** (with GameUIScript)
- **CardDatabase**
- **Canvas**
  - Opponent Panel (Name, Score, RevealCards, Timer)
  - TurnText
  - CostText
  - Room Code (displayed for both players after joining)
  - MessageText
  - Player's Own Panel
    - Name, Score, RevealCards, Timer
    - HandPanel
    - CardPrefab (cards shown here)
    - PlayButton (appears when selecting card to play, disappears after playing)
    - EndTurn Button (used after playing cards)
  - Back Button & End Game Button
    - Game ends automatically after 6 turns, popup shows winner and two buttons: Play Again, End Room

### Prefabs

- **PlayerPrefab**
  - PlayerScript
  - Required components
- **CardPrefab**
  - Button for card interaction

---

## 🏗 Project Structure

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

## 🔔 Event-Driven Architecture

Internal events:

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

## 📝 Notes

- JSON messages only (no raw RPC arguments)
- Modular ability system
- Event-driven architecture
- Basic reconnection supported

---

## 🃏 Unity Networking Evaluation Task

**Goal:**  
Build a turn‑based multiplayer card game prototype where two players connect online, play multiple cards per turn, and resolve abilities based on simple rules.

**Focus areas:**  
Networking, reconnection, event‑driven architecture, and clean modular code.

**Environment & Platform:**

- Unity version: 6000.2
- Target platform: Android

### Core Requirements

1. **Multiplayer Setup**

   - Use any Unity‑friendly networking solution (Mirror, Netcode for GameObjects, WebSockets, Socket.IO, etc.).
   - Support 1v1 matchmaking (quick join or room‑based).
   - Synchronize game state across clients.

2. **Game Flow**

   - Match length: 6 turns (fixed).
   - Each player starts with a deck of 12 cards.
   - Initial hand: 3 cards.
   - At the start of each turn: draw +1 card.
   - Each turn has a 30‑second timer.
   - Players can end their turn early using an End Turn button.
   - If the timer expires before the player presses the End Turn button, the turn will end automatically.

3. **Cost and Power System**
   - At the 1st turn, each player has 1 cost available. At the 2nd turn, each player has 2 cost available.
   - This continues until the 6th turn, where each player has 6 cost available.
   - Players may play multiple cards per turn, as long as the total cost of chosen cards ≤ available cost for that turn.
   - Cards are revealed simultaneously and resolved.
   - Card power contributes to player score.
   - Winner = highest score after 6 turns.

### Card System

Cards are defined in JSON:

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

Fields:

- id: unique identifier
- name: card name
- cost: cost required to play the card
- power: base strength
- ability: object with:
  - type: keyword for the ability (GainPoints, StealPoints, etc.)
  - value: integer parameter (points gained, cards discarded, etc.)

Example abilities:

- GainPoints → Add value points to player score
- StealPoints → Take value points from opponent and add them to your score
- DoublePower → Multiply this card’s power by value (usually 2)
- DrawExtraCard → Draw value extra cards into hand
- DiscardOpponentRandomCard → Randomly remove value cards from opponent’s hand (cannot be played later)
- DestroyOpponentCardInPlay → Remove value cards from opponent’s revealed set before resolution (its power and ability are cancelled)

### Event‑Driven Messaging

- Use events internally:
  - GameStart, TurnStart, PlayerEndedTurn, RevealCards, GameEnd
- The networking layer must send JSON messages (not raw arguments, not HashTables).
- ❌ Do NOT send raw values directly as RPC arguments (e.g., int, string, float, bool, etc.)

#### Example: Game Start

```json
{ "action": "gameStart", "playerIds": ["P1", "P2"], "totalTurns": 6 }
```

Sent when a new match begins. Includes the participating players and the fixed number of turns.

#### Example: Reveal Cards

```json
{ "action": "revealCards", "cardIds": [2, 5] }
```

Sent when both players’ chosen cards are revealed simultaneously at the end of a turn. cardIds lists the cards being revealed for that player.

#### Example: End Turn

```json
{ "action": "endTurn", "playerId": "P1" }
```

Sent when a player manually ends their turn before the timer expires. Includes the player identifier so the system knows which player has finished their turn.

---

## 📦 Deliverables

- Unity Project with:
  - Multiplayer lobby/room join
  - Basic UI: hand, card selection, cost, score
  - JSON‑driven card definitions
  - Event‑based messaging for gameplay actions
- Video recording of demo gameplay (showing a full 6‑turn match)
- APK build file (Android)
- Source code in a public GitHub repository
- Short README:
  - Networking solution chosen
  - How JSON is used for cards & abilities
  - Instructions to run & test

---

## 📝 Evaluation Criteria

- ✅ Working multiplayer flow (two clients can play a 6‑turn match)
- ✅ Clean, modular code (networking, game logic, UI separated)
- ✅ JSON‑driven card system (easy to extend)
- ✅ Event‑based architecture (decoupled, reusable)
- ✅ Clear documentation
- ✅ Correct deliverables (video, APK, GitHub repo)

---

## 👤 Developer

Richa Agrawal  
Unity | C# | Multiplayer Systems
