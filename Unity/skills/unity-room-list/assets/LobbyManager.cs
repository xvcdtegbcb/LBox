using UnityEngine;

public class LobbyManager : MonoBehaviour
{
    [SerializeField] private GameObject roomListPanelGO;
    [SerializeField] private GameObject lobbyPanelGO;

    private void Start()
    {
        ShowRoomList();
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
