---
name: unity-lobby-panel
description: Build a complete multiplayer lobby UI for Unity using UI Toolkit (UXML + USS + C#). Combines room settings (map/mode/player count/time/score), slot-based dual team player list (Red/Blue), ready/cancel buttons, public/private toggle with password, map preview, and host-only start button. Includes a UI Builder right-click path copier tool. Use when creating game lobby/room UI, multiplayer pre-game screens, or slot-based team selection panels. Trigger for "联机大厅", "房间面板", "lobby UI", "room settings", "team slots", "准备按钮", "game lobby panel".
---

# Unity Lobby Panel (UI Toolkit)

Complete multiplayer lobby panel built with Unity UI Toolkit. Combines room configuration, dual-team slot-based player list, and action buttons into one unified panel.

## Architecture

| File | Location | Purpose |
|---|---|---|
| `LobbyPanel.uxml` | `Assets/UI/` | Layout: header + left panel (visibility, map preview, settings, description) + right panel (red/blue team slots) + action bar |
| `LobbyPanel.uss` | `Assets/UI/` | Dark theme styles, slot states, button styles, scrollbar hiding |
| `LobbyPanel.cs` | `Assets/Scripts/` | MonoBehaviour: slot system, settings rows, map data, ready/start, public toggle |
| `UIHierarchyPathCopier.cs` | `Assets/Editor/` | Editor tool: right-click in UI Builder to copy element path / Q() query |

## Layout Overview

```
┌─────────────────────────────────────────────────────────────┐
│ 房间名 [Room 2294]                                          │ Header
├──────────────────────────┬──────────────────────────────────┤
│ 公开 [●] 密码 [******]    │  红队 2/3        蓝队 1/3         │ Visibility + Teams
├──────────────────────────┤                                  │
│  ┌────────────────────┐  │  #1 LocalPlayer  #1 空位         │
│  │   地图预览(彩色)   │  │  #2 ShadowKnight #2 IceFalcon   │
│  └────────────────────┘  │  #3 空位         #3 空位         │
│  地图    ◀ DESERT ▶      │                                  │
│  模式    ◀ TDM ▶         │                                  │
│  人数    ◀ 6 ▶           │                                  │
│  时间    ◀ 15min ▶       │                                  │
│  分数    ◀ 50 ▶          │                                  │
│  ── 地图描述 ──           │                                  │
│  "广阔的沙漠地形..."     │                                  │
├──────────────────────────┴──────────────────────────────────┤
│ 点击空槽位切换队伍   [准备]            [开始游戏(房主)]       │ Action
└─────────────────────────────────────────────────────────────┘
```

## Key Design Decisions

### 1. Slot-Based Team System
- Each team has `slotsPerTeam` fixed slots (default derived from max player setting / 2)
- Empty slots show "空位" and are clickable
- Local player (when not ready) clicks any empty slot in either team to move there
- Ready players are locked — must cancel ready before switching teams
- Slot count dynamically updates when "最大玩家" setting changes

### 2. Settings as Arrow-Cycle Rows
- 10 settings: 地图, 游戏模式, 最大玩家, 比赛时间, 目标分数, 最大延迟, 比赛后, 队伍选择, 友军伤害, 机器人
- Each row: label + left arrow ◀ + value + right arrow ▶
- Click left/right area of row also cycles (in addition to arrow buttons)
- Changing map updates preview color + description
- Changing max players updates team slot count

### 3. Map Preview + Description
- 4 maps with unique hex preview colors and Chinese descriptions
- Preview area shows colored placeholder (future: replace with actual map screenshots)
- Description text updates when map changes

### 4. Public/Private Toggle
- Implemented as Button (not Toggle) — Unity Toggle has click issues inside ScrollView
- Button text: `●` (on/green) / `○` (off/grey)
- Password field is always visible; password only takes effect when public is off
- Password maxLength = 20, fixed width 180px

### 5. Action Buttons
- **准备** (green): visible when local player exists and not ready
- **取消准备** (orange): visible when local player is ready
- **开始游戏** (blue): visible only when `isHost == true`
- Buttons toggle via `DisplayStyle.Flex/None`

### 6. Scrollbar Hiding
- UXML attribute: `vertical-scroller-visibility="Hidden"`
- Scrolling still works via mouse wheel, scrollbar visual is hidden

### 7. Room Visibility Group Outside ScrollView
- Public toggle and password field placed OUTSIDE the left ScrollView
- This prevents ScrollView from intercepting click events on the toggle button

## Build Steps

### 1. Create Files
- Copy `assets/LobbyPanel.uxml` to `Assets/UI/`
- Copy `assets/LobbyPanel.uss` to `Assets/UI/`
- Copy `assets/LobbyPanel.cs` to `Assets/Scripts/`
- Copy `assets/UIHierarchyPathCopier.cs` to `Assets/Editor/`

### 2. Scene Setup
1. Create GameObject "LobbyPanel"
2. Add `UIDocument` component → set `sourceAsset` to `LobbyPanel.uxml`, `panelSettings` to existing `PanelSettings.asset`
3. Add `LobbyPanel` MonoBehaviour component

### 3. Test with Play Mode
```csharp
var lobby = UnityEngine.Object.FindObjectOfType<LobbyPanel>();
lobby.ClearPlayers();
lobby.AddPlayer("LocalPlayer", LobbyPanel.Team.Red, 0);
lobby.SetLocalPlayer("LocalPlayer");
lobby.AddPlayer("ShadowKnight", LobbyPanel.Team.Red, 2);
lobby.SetReady("ShadowKnight", true);
lobby.AddPlayer("IceFalcon", LobbyPanel.Team.Blue, 1);
lobby.SetReady("IceFalcon", true);
```

### 4. Save as Prefab
```csharp
UnityEditor.PrefabUtility.SaveAsPrefabAsset(panelObj, "Assets/Prefab/LobbyPanel.prefab");
```

## C# API

```csharp
// Player management
lobby.AddPlayer("name", Team.Red, slotIndex);  // -1 = auto-find free slot
lobby.RemovePlayer("name");
lobby.SetReady("name", true);
lobby.SetLocalPlayer("LocalPlayer");  // marks which player is local
lobby.SetHost(true);                    // controls start button visibility
lobby.ClearPlayers();

// Internally managed
// - slotsPerTeam: auto-updated from "最大玩家" setting (maxPlayers / 2)
// - _isPublic: toggled by public-toggle button
// - Map/description: auto-updated when map setting changes
```

## UI Builder Path Copier Tool

The `UIHierarchyPathCopier.cs` adds right-click context menu items in UI Builder:

- **复制层级路径** — copies `main-window/content-row/left-panel/settings-container`
- **复制 Q() 查询代码** — copies `root.Q<VisualElement>("settings-container")`
- **复制路径 + Q() 代码** — copies both

Also accessible via `Ctrl+Shift+C` or `Tools → 复制 UI Builder 选中元素路径`.

Uses reflection to access `BuilderSelection.selection[0]` and `Builder.documentRootElement` to resolve the actual UXML element, filtering out internal control children (e.g. `unity-checkmark`, `unity-toggle__input`).

## USS Pitfalls

- **No `:last-child`** — Unity USS rejects unknown pseudo-classes. Use explicit class names (e.g. `.team-blue { border-right-width: 0px; }`).
- **`rgba()` works** — Unity UI Toolkit supports `rgba(r,g,b,a)` for semi-transparent backgrounds.
- **ScrollView scrollbar** — Use UXML attribute `vertical-scroller-visibility="Hidden"` (USS `display: none` on scroller is unreliable).
- **Toggle click issues** — Unity Toggle inside ScrollView may not receive clicks. Use Button with class toggle instead.

## Integration Notes

- For multiplayer (Mirror/Netcode), replace `AddPlayer`/`RemovePlayer`/`SetReady` with RPC/network message handlers.
- `PlayerInfo` class is public, ready for serialization/sync.
- `localPlayerName` should be set on client connect.
- `isHost` should be set based on room ownership.
- Map preview currently uses solid colors — replace with actual Texture2D when map screenshots are available.
