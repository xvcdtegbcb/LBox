---
name: unity-multiplayer-lobby
description: Complete multiplayer lobby system for Unity using UI Toolkit (UXML + USS + C#). Includes room list panel (browse/join/create rooms), lobby panel (slot-based team selection, room settings, ready/start), and LobbyManager for panel switching. Ready for Mirror/network framework integration. Use when building game lobby, room browser, multiplayer pre-game flow, team selection UI. Trigger for "联机大厅", "房间列表", "lobby system", "room browser", "multiplayer lobby", "team slots", "准备/开始游戏".
---

# Unity Multiplayer Lobby System (UI Toolkit)

Complete lobby system: room list → join/create → lobby → back. Built with UI Toolkit, ready for Mirror integration.

## Architecture

### Files

| File | Location | Purpose |
|---|---|---|
| `RoomListPanel.uxml` | `Assets/UI/` | Room list layout: header + table + footer |
| `RoomListPanel.uss` | `Assets/UI/` | Dark theme, room row stripes, status badges |
| `RoomListPanel.cs` | `Assets/Scripts/` | Room list controller: data, rows, events |
| `LobbyPanel.uxml` | `Assets/UI/` | Lobby layout: header + left panel + right panel + actions |
| `LobbyPanel.uss` | `Assets/UI/` | Dark theme, slots, settings, buttons |
| `LobbyPanel.cs` | `Assets/Scripts/` | Lobby controller: slots, settings, map data, ready |
| `LobbyManager.cs` | `Assets/Scripts/` | Panel switcher + data bridge between panels |
| `UIHierarchyPathCopier.cs` | `Assets/Editor/` | UI Builder right-click path copier tool |

### Flow

```
RoomListPanel ──[加入]──→ LobbyManager.JoinRoom(roomInfo) ──→ LobbyPanel
RoomListPanel ──[创建房间]──→ LobbyManager.CreateRoom() ──→ LobbyPanel (host mode)
LobbyPanel ──[返回]──→ LobbyManager.BackToRoomList() ──→ RoomListPanel
```

## Layout

### Room List Panel
```
┌─────────────────────────────────────────────────────────────┐
│ 房间列表                              [创建房间]              │ Header
├─────────────────────────────────────────────────────────────┤
│ 房间名          地图         模式      人数    状态    操作    │ Column header
├─────────────────────────────────────────────────────────────┤
│ Room 2294    GUANACO DESERT  TDM      6/16   公开   [加入]  │ Room row
│ Room 3301    URBAN WARFARE    CTF      4/8    私密   [加入]  │ Room row
│ ...                                                         │ (scrollable)
├─────────────────────────────────────────────────────────────┤
│ 共 4 个房间                             [刷新]              │ Footer
└─────────────────────────────────────────────────────────────┘
```

### Lobby Panel
```
┌─────────────────────────────────────────────────────────────┐
│ 房间名 [Room 2294]                                          │ Header
├──────────────────────────┬──────────────────────────────────┤
│ 公开 [●] 密码 [******]    │  红队 2/8        蓝队 1/8         │
├──────────────────────────┤                                  │
│  ┌────────────────────┐  │  #1 LocalPlayer  #1 空位         │
│  │   地图预览(彩色)   │  │  #2 ShadowKnight #2 IceFalcon   │
│  └────────────────────┘  │  #3 空位         #3 空位         │
│  地图    ◀ DESERT ▶      │                                  │
│  模式    ◀ TDM ▶         │                                  │
│  人数    ◀ 16 ▶          │                                  │
│  ── 地图描述 ──           │                                  │
│  "广阔的沙漠地形..."     │                                  │
├──────────────────────────┴──────────────────────────────────┤
│ 点击空槽位切换队伍  [返回] [准备]        [开始游戏(房主)]      │ Actions
└─────────────────────────────────────────────────────────────┘
```

## Build Steps

### 1. Copy Files
```
Assets/UI/RoomListPanel.uxml
Assets/UI/RoomListPanel.uss
Assets/UI/LobbyPanel.uxml
Assets/UI/LobbyPanel.uss
Assets/Scripts/RoomListPanel.cs
Assets/Scripts/LobbyPanel.cs
Assets/Scripts/LobbyManager.cs
Assets/Editor/UIHierarchyPathCopier.cs
```

### 2. Scene Setup
1. Create GameObject "RoomListPanel" → add UIDocument (sourceAsset=RoomListPanel.uxml, panelSettings=existing) + RoomListPanel script
2. Create GameObject "LobbyPanel" → add UIDocument (sourceAsset=LobbyPanel.uxml, panelSettings=existing) + LobbyPanel script
3. Create GameObject "LobbyManager" → add LobbyManager script
4. In LobbyManager Inspector: assign RoomListPanel and LobbyPanel GameObjects to `roomListPanelGO` and `lobbyPanelGO`

### 3. Test
```csharp
// Populate room list
var roomList = FindObjectOfType<RoomListPanel>();
roomList.SetRooms(new List<RoomListPanel.RoomInfo> {
    new RoomListPanel.RoomInfo { roomName = "Room 2294", map = "GUANACO DESERT",
        mode = "TEAM DEATH MATCH", currentPlayers = 6, maxPlayers = 16, isPublic = true },
    // ...
});
```
Then in Game View: click "加入" → switches to lobby with room data; click "返回" → back to list.

## C# API

### RoomListPanel
```csharp
roomList.SetRooms(rooms);                    // bulk update
roomList.AddRoom(room);                      // add single room
roomList.RemoveRoom("roomName");
roomList.ClearRooms();
roomList.OnJoinRoom += (info) => { ... };
roomList.OnCreateRoom += () => { ... };
roomList.OnRefresh += () => { ... };
```

### LobbyPanel
```csharp
lobby.AddPlayer("name", Team.Red, slotIndex);
lobby.SetReady("name", true);
lobby.SetLocalPlayer("LocalPlayer");
lobby.SetHost(true);                    // host can edit room name + see start button
lobby.SetRoomName("Room 2294");
lobby.SetMapByName("URBAN WARFARE");
lobby.SetModeByName("CAPTURE THE FLAG");
lobby.SetMaxPlayers(16);
lobby.ClearPlayers();
lobby.OnBack += () => { ... };
```

### LobbyManager
```csharp
manager.JoinRoom(roomInfo);   // join room → lobby (non-host)
manager.CreateRoom();         // create room → lobby (host)
manager.BackToRoomList();     // back to room list
manager.ShowRoomList();
manager.ShowLobby();
manager.CurrentRoom            // current RoomInfo
manager.IsHost                 // is current player host
```

## Key Design Decisions

1. **Slot-based team system** — `slotsPerTeam = maxPlayers / 2`, always divisible by 2
2. **Click empty slots to switch teams** — locked when player is ready
3. **Button-based public toggle** — Unity Toggle has click issues inside ScrollView; Button is reliable
4. **Password always visible** — password only takes effect when public is off
5. **Room name read-only for non-host** — `SetHost(false)` disables room name editing
6. **Data bridge via LobbyManager** — JoinRoom/CreateRoom pass room info to LobbyPanel before switching
7. **Scrollbar hiding** — `vertical-scroller-visibility="Hidden"` in UXML (USS display:none unreliable)
8. **Map preview** — solid color placeholder per map, replace with Texture2D later
9. **Max players dynamic** — changing "最大玩家" setting updates team slot count live

## USS Pitfalls

- **No `:last-child`** — use explicit class names
- **`rgba()` works** for semi-transparent backgrounds
- **ScrollView scrollbar** — use UXML `vertical-scroller-visibility="Hidden"`, not USS
- **Toggle inside ScrollView** — clicks may not register; use Button instead
- **`overflow: hidden`** on cells — prevents long text from breaking layout

## Mirror Integration (Future)

When integrating with Mirror:
1. Replace `LobbyManager.JoinRoom/CreateRoom` with NetworkClient/NetworkManager calls
2. Replace `RoomListPanel.SetRooms` with RPC/sync from server
3. Replace `LobbyPanel.AddPlayer/RemovePlayer/SetReady` with SyncList/SyncVar
4. `PlayerInfo` class → add `[SyncVar]` to fields, make it a NetworkBehaviour
5. `localPlayerName` → set from NetworkClient.connection identity
6. `isHost` → `NetworkServer.active && NetworkClient.localPlayer.isServer`

## UI Builder Path Copier

Right-click in UI Builder hierarchy → "复制层级路径" / "复制 Q() 查询代码" / "复制路径 + Q() 代码". Also `Ctrl+Shift+C`.
