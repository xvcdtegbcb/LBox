using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class PlayerListPanel : MonoBehaviour
{
    public enum Team { Red, Blue }

    public class PlayerInfo
    {
        public string name;
        public Team team;
        public int slot;
        public bool isReady;
    }

    [SerializeField] private int slotsPerTeam = 8;
    [SerializeField] private string localPlayerName = "LocalPlayer";

    private readonly List<PlayerInfo> _players = new();

    private VisualElement _redList;
    private VisualElement _blueList;
    private Label _redCount;
    private Label _blueCount;
    private Label _totalCount;
    private Button _readyBtn;
    private Button _cancelBtn;

    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        _redList = root.Q<VisualElement>("red-list");
        _blueList = root.Q<VisualElement>("blue-list");
        _redCount = root.Q<Label>("red-count");
        _blueCount = root.Q<Label>("blue-count");
        _totalCount = root.Q<Label>("total-count");
        _readyBtn = root.Q<Button>("ready-btn");
        _cancelBtn = root.Q<Button>("cancel-btn");

        _readyBtn.clicked += () => SetReady(localPlayerName, true);
        _cancelBtn.clicked += () => SetReady(localPlayerName, false);

        RefreshUI();
    }

    public void AddPlayer(string playerName, Team team, int slot = -1)
    {
        if (slot < 0)
            slot = FindFreeSlot(team);
        _players.Add(new PlayerInfo { name = playerName, team = team, slot = slot, isReady = false });
        RefreshUI();
    }

    public void RemovePlayer(string playerName)
    {
        _players.RemoveAll(p => p.name == playerName);
        RefreshUI();
    }

    public void SetReady(string playerName, bool ready)
    {
        var player = _players.Find(p => p.name == playerName);
        if (player == null) return;
        player.isReady = ready;
        RefreshUI();
    }

    public void SetLocalPlayer(string playerName)
    {
        localPlayerName = playerName;
        RefreshUI();
    }

    public void ClearPlayers()
    {
        _players.Clear();
        RefreshUI();
    }

    private int FindFreeSlot(Team team)
    {
        var used = new HashSet<int>();
        foreach (var p in _players)
            if (p.team == team) used.Add(p.slot);
        for (int i = 0; i < slotsPerTeam; i++)
            if (!used.Contains(i)) return i;
        return 0;
    }

    private bool IsSlotFree(Team team, int slot)
    {
        foreach (var p in _players)
            if (p.team == team && p.slot == slot) return false;
        return true;
    }

    private void MoveToSlot(Team targetTeam, int targetSlot)
    {
        var local = _players.Find(p => p.name == localPlayerName);
        if (local == null || local.isReady) return;
        if (!IsSlotFree(targetTeam, targetSlot)) return;
        local.team = targetTeam;
        local.slot = targetSlot;
        RefreshUI();
    }

    private void RefreshUI()
    {
        _redList.Clear();
        _blueList.Clear();

        int red = 0, blue = 0, readyCount = 0;

        for (int i = 0; i < slotsPerTeam; i++)
        {
            var rp = _players.Find(p => p.team == Team.Red && p.slot == i);
            _redList.Add(CreateSlot(rp, Team.Red, i));
            if (rp != null) red++;

            var bp = _players.Find(p => p.team == Team.Blue && p.slot == i);
            _blueList.Add(CreateSlot(bp, Team.Blue, i));
            if (bp != null) blue++;
        }

        foreach (var p in _players)
            if (p.isReady) readyCount++;

        _redCount.text = $"{red}/{slotsPerTeam}";
        _blueCount.text = $"{blue}/{slotsPerTeam}";
        _totalCount.text = $"{readyCount} / {_players.Count} 已准备  |  {_players.Count} / {slotsPerTeam * 2}";

        // Toggle action buttons based on local player state
        var local = _players.Find(p => p.name == localPlayerName);
        bool localExists = local != null;
        bool localReady = localExists && local.isReady;
        _readyBtn.style.display = localExists && !localReady ? DisplayStyle.Flex : DisplayStyle.None;
        _cancelBtn.style.display = localReady ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private VisualElement CreateSlot(PlayerInfo player, Team team, int slotIndex)
    {
        var slot = new VisualElement();
        slot.AddToClassList("slot");

        if (player == null)
        {
            // Empty slot — clickable
            slot.AddToClassList("slot-empty");

            var hint = new Label("空位");
            hint.AddToClassList("slot-empty-hint");

            var indexLabel = new Label($"#{slotIndex + 1}");
            indexLabel.AddToClassList("slot-index");

            slot.Add(indexLabel);
            slot.Add(hint);

            var capturedTeam = team;
            var capturedSlot = slotIndex;
            slot.RegisterCallback<ClickEvent>(_ => MoveToSlot(capturedTeam, capturedSlot));
        }
        else
        {
            slot.AddToClassList("slot-occupied");

            var indexLabel = new Label($"#{slotIndex + 1}");
            indexLabel.AddToClassList("slot-index");

            var name = new Label(player.name);
            name.AddToClassList("player-name");

            var status = new Label(player.isReady ? "已准备" : "未准备");
            status.AddToClassList("player-status");
            status.AddToClassList(player.isReady ? "player-status-ready" : "player-status-not-ready");

            slot.Add(indexLabel);
            slot.Add(name);
            slot.Add(status);

            // Highlight local player
            if (player.name == localPlayerName)
                slot.AddToClassList("slot-local");
        }

        return slot;
    }
}
