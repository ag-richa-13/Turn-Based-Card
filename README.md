# TurnBasedCard — 1v1 Turn-Based Multiplayer Card Game (Prototype)

🃏 Unity Networking Evaluation Task
🎯 Goal
Build a turn‑based multiplayer card game prototype where two players connect online, play
multiple cards per turn, and resolve abilities based on simple rules.
Focus areas: networking, reconnection, event‑driven architecture, and clean modular
code.
⚙️ Environment & Platform
● Unity version: 6000.2
● Target platform: Android
✅ Core Requirements

1. Multiplayer Setup
   ● Use any Unity‑friendly networking solution (Mirror, Netcode for GameObjects,
   WebSockets, Socket.IO, etc.).
   ● Support 1v1 matchmaking (quick join or room‑based).
   ● Synchronize game state across clients.
2. Game Flow
   ● Match length: 6 turns (fixed).
   ● Each player starts with a deck of 12 cards.
   ● Initial hand: 3 cards.
   ● At the start of each turn: draw +1 card.
   ● Each turn has a 30‑second timer.
   ● Players can end their turn early using an End Turn button.
   ● If the timer expires before the player presses the End Turn button, the turn will end
   automatically.
3. Cost and Power System
   ● At the 1st turn, each player has 1 cost available. At the 2nd turn, each player has 2
   cost available.
   ● This continues until the 6th turn, where each player has 6 cost available.
   ● Players may play multiple cards per turn, as long as the total cost of chosen cards ≤
   available cost for that turn.
   ● Cards are revealed simultaneously and resolved.
   ● Card power contributes to player score.
   ● Winner = highest score after 6 turns.
   🗂 Card System
   Cards are defined in JSON:
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
   Fields:
   ● id: unique identifier
   ● name: card name
   ● cost: cost required to play the card
   ● power: base strength
   ● ability: object with:
   ○ type: keyword for the ability (GainPoints, StealPoints, etc.)
   ○ value: integer parameter (points gained, cards discarded, etc.)
   Example abilities:
   ● GainPoints → Add value points to player score
   ● StealPoints → Take value points from opponent and add them to your score
   ● DoublePower → Multiply this card’s power by value (usually 2)
   ● DrawExtraCard → Draw value extra cards into hand
   ● DiscardOpponentRandomCard → Randomly remove value cards from opponent’s
   hand (cannot be played later)
   ● DestroyOpponentCardInPlay → Remove value cards from opponent’s revealed set
   before resolution (its power and ability are cancelled)
   🔔 Event‑Driven Messaging
   ● Use events internally:
   ○ GameStart, TurnStart,PlayerEndedTurn, RevealCards,GameEnd
   ● The networking layer must send JSON messages (not raw arguments, not
   HashTables).
   ● ❌ Do NOT send raw values directly as RPC arguments (e.g., int, string, float, bool, etc.)
   🔹 Example: Game Start
   { "action": "gameStart", "playerIds": ["P1", "P2"],
   "totalTurns": 6 }
   ● Sent when a new match begins.
   ● Includes the participating players and the fixed number of turns.
   🔹 Example: Reveal Cards
   { "action": "revealCards", "cardIds": [2, 5] }
   ● Sent when both players’ chosen cards are revealed simultaneously at the end of a turn.
   ● cardIds lists the cards being revealed for that player.
   🔹 Example: End Turn
   { "action": "endTurn", "playerId": "P1" }
   ● Sent when a player manually ends their turn before the timer expires.
   ● Includes the player identifier so the system knows which player has finished their turn
   📦 Deliverables
   ● Unity Project with:
   ○ Multiplayer lobby/room join
   ○ Basic UI: hand, card selection, cost, score
   ○ JSON‑driven card definitions
   ○ Event‑based messaging for gameplay actions
   ● Video recording of demo gameplay (showing a full 6‑turn match)
   ● APK build file (Android)
   ● Source code in a public GitHub repository
   ● Short README:
   ○ Networking solution chosen
   ○ How JSON is used for cards & abilities
   ○ Instructions to run & test
   📝 Evaluation Criteria
   ● ✅ Working multiplayer flow (two clients can play a 6‑turn match)
   ● ✅ Clean, modular code (networking, game logic, UI separated)
   ● ✅ JSON‑driven card system (easy to extend)
   ● ✅ Event‑based architecture (decoupled, reusable)
   ● ✅ Clear documentation
   ● ✅ Correct deliverables (video, APK, GitHub repo)

UI of Game: LobbyScene | |**\_** NatworkManager |**\_**LobbyManager(LobbyUI Script) |**\_**GameEvent | |**\_**Canvas |**\_** Host button | |**\_**Join button | |**\_**Room COde Input Field for client who willl join | |**\_**Room Code(When host create room, here romm code will appear) | |**\_**Status Text |**\_\_**ExitGame GameScene | |**\_** GameManager |**\_**GameUI(GameUIScript) |**\_**CardDatabase | |**\_**Canvas |**\_\_**Opponent Panel (Name, Score of opponent, RevealCards, timer) |**\_\_**TurnText |**\_**CostText |**\_**Room Code (When both host and client enter the room, Room code will appear for both) |**\_\_**MessageText |**\_\_**Player's Own Panel |(Name, Score, RevealCards, Timer) |**\_**HandPanel |\_**\_Card Prefab will show here |\_\_\_**PlayButton(Only Appear when Player Select card to draw, and when player draw card by pressing play, it will disappear) |**\_**EndTurn Button (after draw card, they will end their turn) **\_\_\_\_** |**\_\_**Back Button and End Game button(Game End apne ap hoga jaise hi are turn complete hoga or ek popup show hoga jisme who won the game text show hoga or do button honge, Play Again and End Room) PlayerPrefab | |**\_**PlayerScript |**\_\_**add required component CardPrefab button
