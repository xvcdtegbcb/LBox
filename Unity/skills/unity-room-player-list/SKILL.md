---
name: unity-room-player-list
description: Build a slot-based player dual-list (Red/Blue teams) for Unity game rooms using UI Toolkit (UXML + USS + C#). Use when creating a room/lobby player list with team switching via clickable slots, ready/cancel-ready buttons, and local player highlighting. Trigger for requests like "制作房间玩家列表", "红蓝双队列表", "room player list", "lobby slot UI", "team switch slots", "准备/取消准备按钮".
---

# Unity Room Player List (Slot-Based Dual Team)

Build a slot-based player dual-list for Unity game rooms. Left column = Red team, right column = Blue team. Each team has N fixed slots. Players click empty slots to switch teams. Ready/cancel-ready buttons at the bottom. Local player is highlighted.

## Architecture

Three files, following UI Toolkit conventions:

| File | Location | Purpose |
|---|---|---|
| `PlayerListPanel.uxml` | `Assets/UI/` | Layout: header + two team columns + action bar |
| `PlayerListPanel.uss` | `Assets/UI/` | Dark theme styles, slot states, button styles |
| `PlayerListPanel.cs` | `Assets/Scripts/` | MonoBehaviour: slot management, team switching, ready toggle |

## Key Design Decisions

1. **Fixed slots per team** — Each team renders `slotsPerTeam` (default 8) slots. Empty slots show "空位" and are clickable. Occupied slots show player name + ready status.

2. **Click-to-switch** — Local player (when not ready) clicks any empty slot in either team to move there. `MoveToSlot()` handles the transfer. Ready players are locked.

3. **Ready/Cancel toggle** — Bottom action bar shows one button at a time: green "准备" when not ready, orange "取消准备" when ready. Button visibility toggles via `DisplayStyle.Flex/None`.

4. **Local player highlight** — Green left border + tinted background on the local player's slot for instant identification.

5. **USS `:last-child` unsupported** — Unity UI Toolkit USS does not support `:last-child` pseudo-class. Use a dedicated class (e.g. `.team-blue`) for border removal instead.

## Build Steps

### 1. Create UXML

Copy `assets/PlayerListPanel.uxml` to `Assets/UI/`. Structure:

```
main-window
├── header-row (title + total count)
├── teams-container (row)
│   ├── team-column team-red
│   │   ├── team-header-red (name + count)
│   │   └── ScrollView → red-list
│   └── team-column team-blue
│       ├── team-header-blue (name + count)
│       └── ScrollView → blue-list
└── action-bar (hint + ready-btn + cancel-btn)
```

### 2. Create USS

Copy `assets/PlayerListPanel.uss` to `Assets/UI/`. Key style classes:

- `.slot` — base row style (44px height, flex-row)
- `.slot-empty` / `.slot-empty:hover` — clickable empty slots
- `.slot-occupied` — player-filled slots
- `.slot-local` — green border highlight for local player
- `.player-status-ready` / `.player-status-not-ready` — green/orange status badges
- `.ready-btn` (green) / `.cancel-btn` (orange) — action buttons

### 3. Create C# Script

Copy `assets/PlayerListPanel.cs` to `Assets/Scripts/`. Key API:

```csharp
// Setup
panel.AddPlayer("name", Team.Red, slotIndex);  // -1 = auto-find free slot
panel.SetLocalPlayer("LocalPlayer");            // marks which player is local
panel.SetReady("name", true);                   // toggle ready state

// Queries
panel.ClearPlayers();
panel.RemovePlayer("name");
```

Internal flow:
- `RefreshUI()` rebuilds all slots, updates counts, toggles button visibility
- `CreateSlot(player, team, index)` renders empty (clickable) or occupied slot
- `MoveToSlot(team, slot)` moves local player if not ready and target is free
- `FindFreeSlot(team)` scans for first unoccupied slot index

### 4. Scene Setup

1. Create GameObject "PlayerListPanel"
2. Add `UIDocument` component → set `sourceAsset` to `PlayerListPanel.uxml`, `panelSettings` to existing `PanelSettings.asset`
3. Add `PlayerListPanel` MonoBehaviour component
4. Adjust `slotsPerTeam` and `localPlayerName` in Inspector if needed

### 5. Test with Play Mode

Enter Play Mode, run this C# script to populate test data:

```csharp
var panel = UnityEngine.Object.FindObjectOfType<PlayerListPanel>();
panel.ClearPlayers();
panel.AddPlayer("LocalPlayer", PlayerListPanel.Team.Red, 0);
panel.SetLocalPlayer("LocalPlayer");
panel.AddPlayer("ShadowKnight", PlayerListPanel.Team.Red, 2);
panel.SetReady("ShadowKnight", true);
panel.AddPlayer("IceFalcon", PlayerListPanel.Team.Blue, 1);
panel.SetReady("IceFalcon", true);
```

Then interact directly in Game View: click empty slots to switch teams, click ready/cancel buttons.

## USS Pitfalls

- **No `:last-child`** — Unity USS rejects unknown pseudo-classes. Use explicit class names.
- **`rgba()` works** — Unity UI Toolkit supports `rgba(r,g,b,a)` for semi-transparent backgrounds.
- **`border-radius`** — Supported, use `px` units.
- **Hover states** — `:hover` and `:active` work on Buttons and VisualElements.

## Integration Notes

- For multiplayer (Mirror/Netcode), replace `AddPlayer`/`RemovePlayer`/`SetReady` calls with RPC/network message handlers.
- `PlayerInfo` class is public and serializable-ready for network state sync.
- `localPlayerName` should be set to the local client's player name on connection.
