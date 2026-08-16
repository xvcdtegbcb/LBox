using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class RoomListPanel : MonoBehaviour
{
    public class RoomInfo
    {
        public string roomName;
        public string map;
        public string mode;
        public int currentPlayers;
        public int maxPlayers;
        public bool isPublic;
        public string hostName;
    }

    public event Action<RoomInfo> OnJoinRoom;
    public event Action OnCreateRoom;
    public event Action OnRefresh;

    private readonly List<RoomInfo> _rooms = new();
    private VisualElement _roomList;
    private Label _roomCount;
    private Button _createBtn;
    private Button _refreshBtn;

    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        _roomList = root.Q<VisualElement>("room-list");
        _roomCount = root.Q<Label>("room-count");
        _createBtn = root.Q<Button>("create-btn");
        _refreshBtn = root.Q<Button>("refresh-btn");

        _createBtn.clicked += () => OnCreateRoom?.Invoke();
        _refreshBtn.clicked += () =>
        {
            OnRefresh?.Invoke();
            Debug.Log("[房间列表] 刷新");
        };

        RefreshUI();
    }

    public void SetRooms(List<RoomInfo> rooms)
    {
        _rooms.Clear();
        _rooms.AddRange(rooms);
        RefreshUI();
    }

    public void AddRoom(RoomInfo room)
    {
        _rooms.Add(room);
        RefreshUI();
    }

    public void RemoveRoom(string roomName)
    {
        _rooms.RemoveAll(r => r.roomName == roomName);
        RefreshUI();
    }

    public void ClearRooms()
    {
        _rooms.Clear();
        RefreshUI();
    }

    private void RefreshUI()
    {
        _roomList.Clear();

        if (_rooms.Count == 0)
        {
            _roomList.Add(CreateEmptyHint("暂无房间，点击「创建房间」开始"));
        }
        else
        {
            for (int i = 0; i < _rooms.Count; i++)
                _roomList.Add(CreateRoomRow(_rooms[i], i));
        }

        _roomCount.text = $"共 {_rooms.Count} 个房间";
    }

    private VisualElement CreateRoomRow(RoomInfo room, int index)
    {
        var row = new VisualElement();
        row.AddToClassList("room-row");
        if (index % 2 == 0)
            row.AddToClassList("room-row-even");

        var name = new Label(room.roomName);
        name.AddToClassList("room-cell");
        name.AddToClassList("cell-name");

        var map = new Label(room.map);
        map.AddToClassList("room-cell");
        map.AddToClassList("cell-map");

        var mode = new Label(room.mode);
        mode.AddToClassList("room-cell");
        mode.AddToClassList("cell-mode");

        var players = new Label($"{room.currentPlayers}/{room.maxPlayers}");
        players.AddToClassList("room-cell");
        players.AddToClassList("cell-players");

        var status = new Label(room.isPublic ? "公开" : "私密");
        status.AddToClassList("room-cell");
        status.AddToClassList("cell-status");
        status.AddToClassList(room.isPublic ? "status-public" : "status-private");

        var joinBtn = new Button { text = "加入" };
        joinBtn.AddToClassList("join-btn");
        joinBtn.clicked += () => OnJoinRoom?.Invoke(room);

        var actionCell = new VisualElement();
        actionCell.AddToClassList("room-cell");
        actionCell.AddToClassList("cell-action");
        actionCell.Add(joinBtn);

        row.Add(name);
        row.Add(map);
        row.Add(mode);
        row.Add(players);
        row.Add(status);
        row.Add(actionCell);

        return row;
    }

    private static Label CreateEmptyHint(string text)
    {
        var hint = new Label(text);
        hint.AddToClassList("empty-hint");
        return hint;
    }
}
