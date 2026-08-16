using UnityEngine;

public class LobbyManager : MonoBehaviour
{
    [SerializeField] private GameObject roomListPanelGO;
    [SerializeField] private GameObject lobbyPanelGO;

    public RoomListPanel.RoomInfo CurrentRoom { get; private set; }
    public bool IsHost { get; private set; }

    private void Start()
    {
        WireEvents();
        ShowRoomList();
    }

    private void WireEvents()
    {
        var roomList = roomListPanelGO.GetComponent<RoomListPanel>();
        var lobby = lobbyPanelGO.GetComponent<LobbyPanel>();

        roomList.OnJoinRoom += JoinRoom;
        roomList.OnCreateRoom += CreateRoom;
        lobby.OnBack += BackToRoomList;
    }

    public void JoinRoom(RoomListPanel.RoomInfo room)
    {
        CurrentRoom = room;
        IsHost = false;

        var lobby = lobbyPanelGO.GetComponent<LobbyPanel>();
        lobby.SetRoomName(room.roomName);
        lobby.SetMapByName(room.map);
        lobby.SetModeByName(room.mode);
        lobby.SetMaxPlayers(room.maxPlayers);
        lobby.SetHost(false);
        lobby.SetLocalPlayer("LocalPlayer");
        lobby.ClearPlayers();
        lobby.AddPlayer("LocalPlayer", LobbyPanel.Team.Red, 0);
        lobby.SetLocalPlayer("LocalPlayer");

        ShowLobby();
        Debug.Log($"[LobbyManager] 加入房间: {room.roomName} | 地图: {room.map} | 模式: {room.mode}");
    }

    public void CreateRoom()
    {
        int roomNum = Random.Range(1000, 9999);
        CurrentRoom = new RoomListPanel.RoomInfo
        {
            roomName = $"Room {roomNum}",
            map = "GUANACO DESERT",
            mode = "TEAM DEATH MATCH",
            currentPlayers = 1,
            maxPlayers = 16,
            isPublic = true,
            hostName = "LocalPlayer"
        };
        IsHost = true;

        var lobby = lobbyPanelGO.GetComponent<LobbyPanel>();
        lobby.SetRoomName(CurrentRoom.roomName);
        lobby.SetMapByName(CurrentRoom.map);
        lobby.SetModeByName(CurrentRoom.mode);
        lobby.SetMaxPlayers(CurrentRoom.maxPlayers);
        lobby.SetHost(true);
        lobby.SetLocalPlayer("LocalPlayer");
        lobby.ClearPlayers();
        lobby.AddPlayer("LocalPlayer", LobbyPanel.Team.Red, 0);
        lobby.SetLocalPlayer("LocalPlayer");

        ShowLobby();
        Debug.Log($"[LobbyManager] 创建房间: {CurrentRoom.roomName} | 房主模式");
    }

    public void BackToRoomList()
    {
        ShowRoomList();
        Debug.Log("[LobbyManager] 返回房间列表");
    }

    public void ShowRoomList()
    {
        if (roomListPanelGO != null) roomListPanelGO.SetActive(true);
        if (lobbyPanelGO != null) lobbyPanelGO.SetActive(false);
    }

    public void ShowLobby()
    {
        if (roomListPanelGO != null) roomListPanelGO.SetActive(false);
        if (lobbyPanelGO != null) lobbyPanelGO.SetActive(true);
    }
}
