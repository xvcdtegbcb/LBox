---
name: unity-room-list
description: Build a scrollable server room list panel for Unity using UI Toolkit (UXML + USS + C#). Shows all server rooms with name, map, mode, player count, public/private status, and join button. Includes LobbyManager for switching between room list and lobby panel. Use when creating a multiplayer room browser, server browser, or room list screen. Trigger for "房间列表", "服务器列表", "room list", "server browser", "join room", "创建房间", "game lobby navigation".
---

# Unity Room List Panel (UI Toolkit)

Scrollable room list showing all server rooms. Pairs with `unity-lobby-panel` skill for full lobby flow: room list → join → lobby → back.

## Architecture

| File | Location | Purpose |
|---|---|---|
| `RoomListPanel.uxml` | `Assets/UI/` | Layout: header + column header + scrollable room list + footer |
| `RoomListPanel.uss` | `Assets/UI/` | Dark theme styles, room row stripes, status badges, buttons |
| `RoomListPanel.cs` | `Assets/Scripts/` | Controller: room data, row rendering, join/create/refresh events |
| `LobbyManager.cs` | `Assets/Scripts/` | Panel switcher: toggles between RoomListPanel and LobbyPanel |

## Layout

```
┌─────────────────────────────────────────────────────────────┐
│ 房间列表                              [创建房间]              │ Header
├─────────────────────────────────────────────────────────────┤
│ 房间名          地图         模式      人数    状态    操作    │ Column header
├─────────────────────────────────────────────────────────────┤
│ Room 2294    GUANACO DESERT  TDM      6/16   公开   [加入]  │ Room row
│ Room 3301    URBAN WARFARE    CTF      4/8    私密   [加入]  │ Room row (even)
│ Room 4412    FOREST OUTPOST  FFA      2/6    公开   [加入]  │ Room row
│ ...                                                         │ (scrollable)
├─────────────────────────────────────────────────────────────┤
│ 共 12 个房间                             [刷新]              │ Footer
└─────────────────────────────────────────────────────────────┘
```

## Build Steps

### 1. Create Files
- Copy `assets/RoomListPanel.uxml` to `Assets/UI/`
- Copy `assets/RoomListPanel.uss` to `Assets/UI/`
- Copy `assets/RoomListPanel.cs` to `Assets/Scripts/`
- Copy `assets/LobbyManager.cs` to `Assets/Scripts/`

### 2. Scene Setup
1. Create GameObject "RoomListPanel" with UIDocument (sourceAsset = RoomListPanel.uxml) + RoomListPanel script
2. Create GameObject "LobbyPanel" with UIDocument (sourceAsset = LobbyPanel.uxml) + LobbyPanel script
3. Create GameObject "LobbyManager" with LobbyManager script
4. In Inspector, assign roomListPanelGO and lobbyPanelGO to LobbyManager
5. LobbyPanel should have a "back-btn" (返回) that calls `lobby.OnBack += () => manager.ShowRoomList()`

### 3. Wire Events
```csharp
var roomList = FindObjectOfType<RoomListPanel>();
var lobby = FindObjectOfType<LobbyPanel>();
var manager = FindObjectOfType<LobbyManager>();

roomList.OnJoinRoom += (info) => { manager.ShowLobby(); };
roomList.OnCreateRoom += () => { manager.ShowLobby(); };
lobby.OnBack += () => { manager.ShowRoomList(); };
```

### 4. Test
```csharp
var rooms = new List<RoomListPanel.RoomInfo> {
    new RoomListPanel.RoomInfo { roomName = "Room 2294", map = "GUANACO DESERT",
        mode = "TEAM DEATH MATCH", currentPlayers = 6, maxPlayers = 16, isPublic = true },
    // ...
};
roomList.SetRooms(rooms);
```

## C# API

```csharp
// RoomListPanel
roomList.SetRooms(rooms);                    // bulk update
roomList.AddRoom(room);                      // add single room
roomList.RemoveRoom("roomName");              // remove room
roomList.ClearRooms();
roomList.OnJoinRoom += (info) => { ... };    // join button clicked
roomList.OnCreateRoom += () => { ... };       // create button clicked
roomList.OnRefresh += () => { ... };          // refresh button clicked

// LobbyManager
manager.ShowRoomList();   // show room list, hide lobby
manager.ShowLobby();      // show lobby, hide room list

// LobbyPanel (from unity-lobby-panel skill)
lobby.OnBack += () => { ... };   // back button event
```

## Design Notes

- **Alternating row colors**: even rows use `.room-row-even` class for subtle stripe effect
- **Status badges**: green "公开" / orange "私密" for public/private
- **ScrollView**: uses `vertical-scroller-visibility="Auto"` — scrollbar appears only when needed
- **Empty state**: shows "暂无房间，点击「创建房间」开始" when list is empty
- **RoomInfo class**: public, ready for network serialization

## Integration with unity-lobby-panel skill

This skill is designed to pair with `unity-lobby-panel`. The flow is:

```
RoomListPanel ──[加入]──→ LobbyPanel
RoomListPanel ──[创建房间]──→ LobbyPanel
LobbyPanel ──[返回]──→ RoomListPanel
```

LobbyManager handles the GameObject active/inactive switching. Both panels use the same PanelSettings.asset.

## USS Pitfalls

- **No `:last-child`** — use explicit class names instead
- **`overflow: hidden`** on cells — prevents long room names/maps from breaking layout
- **Fixed column widths** — `.col-name` has `flex-grow: 1` to fill remaining space, others have fixed widths
