using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class LobbyPanel : MonoBehaviour
{
    public enum Team { Red, Blue }

    public class MapInfo
    {
        public string name;
        public string description;
        public string previewColor; // hex color for placeholder preview
    }

    public class PlayerInfo
    {
        public string name;
        public Team team;
        public int slot;
        public bool isReady;
    }

    #region Static Data

    private static readonly MapInfo[] Maps =
    {
        new MapInfo
        {
            name = "GUANACO DESERT",
            description = "广阔的沙漠地形，狙击手的天堂。沙丘提供掩护，但开阔区域危险重重。适合远距离交火。",
            previewColor = "#3D2B1F"
        },
        new MapInfo
        {
            name = "URBAN WARFARE",
            description = "密集的城市街区，巷战为主。建筑物内部和屋顶都是战术要点。适合近距离突击。",
            previewColor = "#2A2A3A"
        },
        new MapInfo
        {
            name = "FOREST OUTPOST",
            description = "茂密森林中的废弃哨所。树木和雾气提供天然隐蔽，适合伏击和游击战术。",
            previewColor = "#1A2A1A"
        },
        new MapInfo
        {
            name = "ARCTIC BASE",
            description = "极地军事基地，冰雪覆盖。低温影响视野，滑冰面需要特殊移动技巧。适合团队配合。",
            previewColor = "#2A3A4A"
        },
    };

    private static readonly (string label, string[] options)[] Settings =
    {
        ("地图", new[] { "GUANACO DESERT", "URBAN WARFARE", "FOREST OUTPOST", "ARCTIC BASE" }),
        ("游戏模式", new[] { "TEAM DEATH MATCH", "CAPTURE THE FLAG", "FREE FOR ALL", "DOMINATION" }),
        ("最大玩家", new[] { "6 PLAYERS", "8 PLAYERS", "10 PLAYERS", "12 PLAYERS", "16 PLAYERS", "20 PLAYERS", "32 PLAYERS" }),
        ("比赛时间", new[] { "15 MINUTE", "10 MINUTE", "20 MINUTE", "30 MINUTE" }),
        ("目标分数", new[] { "50 KILLS", "30 KILLS", "75 KILLS", "100 KILLS" }),
        ("最大延迟", new[] { "500 MS", "200 MS", "300 MS", "1000 MS" }),
        ("比赛结束后", new[] { "BACK TO LOBBY", "STAY IN ROOM", "NEXT MAP", "AUTO RESTART" }),
        ("队伍选择", new[] { "MANUALLY", "AUTO BALANCE", "RANDOM" }),
        ("友军伤害", new[] { "DISABLE", "ENABLE" }),
        ("机器人", new[] { "DISABLE", "ENABLE" }),
    };

    #endregion

    #region Serialized Fields

    [SerializeField] private int slotsPerTeam = 8;
    [SerializeField] private string localPlayerName = "LocalPlayer";
    [SerializeField] private bool isHost = true;

    #endregion

    #region State

    private readonly List<PlayerInfo> _players = new();
    private readonly int[] _settingIndices = new int[Settings.Length];

    #endregion

    #region UI References

    private VisualElement _redList;
    private VisualElement _blueList;
    private Label _redCount;
    private Label _blueCount;
    private Label _totalCount;
    private Button _readyBtn;
    private Button _cancelBtn;
    private Button _startBtn;
    private VisualElement _mapPreview;
    private Label _mapDesc;
    private VisualElement _settingsContainer;
    private Button _publicToggle;
    private bool _isPublic = true;
    private TextField _passwordField;
    private Label _passwordLabel;
    private Button _backBtn;
    private TextField _roomNameField;

    public event System.Action OnBack;

    #endregion

    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        // Room name
        _roomNameField = root.Q<TextField>("room-name");

        // Player list
        _redList = root.Q<VisualElement>("red-list");
        _blueList = root.Q<VisualElement>("blue-list");
        _redCount = root.Q<Label>("red-count");
        _blueCount = root.Q<Label>("blue-count");
        _totalCount = root.Q<Label>("total-count");

        // Action buttons
        _readyBtn = root.Q<Button>("ready-btn");
        _cancelBtn = root.Q<Button>("cancel-btn");
        _startBtn = root.Q<Button>("start-btn");
        _backBtn = root.Q<Button>("back-btn");
        _backBtn.clicked += () => OnBack?.Invoke();

        // Map preview + description
        _mapPreview = root.Q<VisualElement>("map-preview");
        _mapDesc = root.Q<Label>("map-desc");

        // Settings
        _settingsContainer = root.Q<VisualElement>("settings-container");

        // Room header
        _publicToggle = root.Q<Button>("public-toggle");
        _publicToggle.clicked += () =>
        {
            _isPublic = !_isPublic;
            _publicToggle.text = _isPublic ? "●" : "○";
            _publicToggle.EnableInClassList("public-toggle-on", _isPublic);
            _publicToggle.EnableInClassList("public-toggle-off", !_isPublic);
            Debug.Log($"[房间设置] 公开: {_isPublic}");
        };
        _publicToggle.EnableInClassList("public-toggle-on", true);
        _passwordField = root.Q<TextField>("password-field");
        _passwordField.maxLength = 20;
        _passwordLabel = root.Q<Label>("password-label");

        // Wire button events
        _readyBtn.clicked += () => SetReady(localPlayerName, true);
        _cancelBtn.clicked += () => SetReady(localPlayerName, false);
        _startBtn.clicked += OnStartGame;

        // Build setting rows
        BuildSettings();

        // Initial UI state
        UpdateMapPreview();
        UpdateSlotsPerTeam();
        RefreshPlayerList();
    }

    #region Player Management

    public void AddPlayer(string playerName, Team team, int slot = -1)
    {
        if (slot < 0) slot = FindFreeSlot(team);
        _players.Add(new PlayerInfo { name = playerName, team = team, slot = slot, isReady = false });
        RefreshPlayerList();
    }

    public void RemovePlayer(string playerName)
    {
        _players.RemoveAll(p => p.name == playerName);
        RefreshPlayerList();
    }

    public void SetReady(string playerName, bool ready)
    {
        var player = _players.Find(p => p.name == playerName);
        if (player == null) return;
        player.isReady = ready;
        RefreshPlayerList();
    }

    public void SetLocalPlayer(string playerName)
    {
        localPlayerName = playerName;
        RefreshPlayerList();
    }

    public void SetHost(bool host)
    {
        isHost = host;
        // Only host can edit room name
        _roomNameField.SetEnabled(isHost);
        RefreshPlayerList();
    }

    public void SetRoomName(string name)
    {
        _roomNameField.value = name;
    }

    public void SetMapByName(string mapName)
    {
        for (int i = 0; i < Maps.Length; i++)
        {
            if (Maps[i].name == mapName)
            {
                _settingIndices[0] = i;
                BuildSettings();
                UpdateMapPreview();
                return;
            }
        }
    }

    public void SetModeByName(string modeName)
    {
        var modes = Settings[1].options;
        for (int i = 0; i < modes.Length; i++)
        {
            if (modes[i] == modeName)
            {
                _settingIndices[1] = i;
                BuildSettings();
                return;
            }
        }
    }

    public void SetMaxPlayers(int maxPlayers)
    {
        var options = Settings[2].options;
        for (int i = 0; i < options.Length; i++)
        {
            if (int.TryParse(options[i].Split(' ')[0], out int n) && n == maxPlayers)
            {
                _settingIndices[2] = i;
                BuildSettings();
                UpdateSlotsPerTeam();
                return;
            }
        }
    }

    public void ClearPlayers()
    {
        _players.Clear();
        RefreshPlayerList();
    }

    #endregion

    #region Slot System

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
        RefreshPlayerList();
    }

    #endregion

    #region Settings

    private void BuildSettings()
    {
        // Clear only dynamically created rows, keep the header-group (public/password)
        var rows = _settingsContainer.Query<VisualElement>(null, "setting-row").ToList();
        foreach (var row in rows)
            row.RemoveFromHierarchy();

        for (int i = 0; i < Settings.Length; i++)
            _settingsContainer.Add(CreateSettingRow(i));
    }

    private VisualElement CreateSettingRow(int settingIndex)
    {
        var (label, options) = Settings[settingIndex];
        int currentIndex = _settingIndices[settingIndex];

        var row = new VisualElement();
        row.AddToClassList("setting-row");

        var rowLabel = new Label(label);
        rowLabel.AddToClassList("row-label");

        var controlArea = new VisualElement();
        controlArea.AddToClassList("control-area");

        var leftBtn = new Button { text = "\u25C0" };
        leftBtn.AddToClassList("arrow-btn");

        var valueLabel = new Label(options[currentIndex]);
        valueLabel.AddToClassList("row-value");

        var rightBtn = new Button { text = "\u25B6" };
        rightBtn.AddToClassList("arrow-btn");

        void Prev()
        {
            _settingIndices[settingIndex] = (currentIndex - 1 + options.Length) % options.Length;
            currentIndex = _settingIndices[settingIndex];
            valueLabel.text = options[currentIndex];
            if (settingIndex == 0) UpdateMapPreview();
            if (settingIndex == 2) UpdateSlotsPerTeam();
            Debug.Log($"[设置变更] {label}: {options[currentIndex]}");
        }

        void Next()
        {
            _settingIndices[settingIndex] = (currentIndex + 1) % options.Length;
            currentIndex = _settingIndices[settingIndex];
            valueLabel.text = options[currentIndex];
            if (settingIndex == 0) UpdateMapPreview();
            if (settingIndex == 2) UpdateSlotsPerTeam();
            Debug.Log($"[设置变更] {label}: {options[currentIndex]}");
        }

        leftBtn.clicked += Prev;
        rightBtn.clicked += Next;

        row.RegisterCallback<ClickEvent>(evt =>
        {
            if (evt.target is Button) return;
            var x = evt.position.x;
            var width = row.localBound.width;
            if (x > width * 0.55f) Next();
            else if (x < width * 0.45f) Prev();
        });

        controlArea.Add(leftBtn);
        controlArea.Add(valueLabel);
        controlArea.Add(rightBtn);

        row.Add(rowLabel);
        row.Add(controlArea);

        return row;
    }

    private void UpdateMapPreview()
    {
        int mapIdx = _settingIndices[0];
        var map = Maps[mapIdx];
        _mapDesc.text = map.description;

        // Set preview background color
        _mapPreview.style.backgroundColor = new Color(
            HexToFloat(map.previewColor, 1, 3),
            HexToFloat(map.previewColor, 3, 5),
            HexToFloat(map.previewColor, 5, 7),
            1f
        );
    }

    private void UpdateSlotsPerTeam()
    {
        // Parse "N PLAYERS" → N / 2 per team
        var option = Settings[2].options[_settingIndices[2]];
        var parts = option.Split(' ');
        if (int.TryParse(parts[0], out int maxPlayers))
        {
            slotsPerTeam = maxPlayers / 2;
            // Clamp player slots to valid range
            foreach (var p in _players)
            {
                if (p.slot >= slotsPerTeam)
                    p.slot = FindFreeSlot(p.team);
            }
            RefreshPlayerList();
        }
    }

    private static float HexToFloat(string hex, int start, int end)
    {
        string sub = hex.Substring(start, end - start);
        return System.Convert.ToInt32(sub, 16) / 255f;
    }

    #endregion

    #region Room Header

    #endregion

    #region Action Buttons

    private void OnStartGame()
    {
        int readyCount = 0;
        foreach (var p in _players)
            if (p.isReady) readyCount++;

        Debug.Log($"[开始游戏] 房主启动游戏 | 已准备: {readyCount}/{_players.Count} | 地图: {Maps[_settingIndices[0]].name}");
    }

    #endregion

    #region UI Refresh

    private void RefreshPlayerList()
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

        // Toggle ready/cancel buttons
        var local = _players.Find(p => p.name == localPlayerName);
        bool localExists = local != null;
        bool localReady = localExists && local.isReady;
        _readyBtn.style.display = localExists && !localReady ? DisplayStyle.Flex : DisplayStyle.None;
        _cancelBtn.style.display = localReady ? DisplayStyle.Flex : DisplayStyle.None;

        // Start button: only visible for host
        _startBtn.style.display = isHost ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private VisualElement CreateSlot(PlayerInfo player, Team team, int slotIndex)
    {
        var slot = new VisualElement();
        slot.AddToClassList("slot");

        if (player == null)
        {
            slot.AddToClassList("slot-empty");

            var indexLabel = new Label($"#{slotIndex + 1}");
            indexLabel.AddToClassList("slot-index");

            var hint = new Label("空位");
            hint.AddToClassList("slot-empty-hint");

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

            if (player.name == localPlayerName)
                slot.AddToClassList("slot-local");
        }

        return slot;
    }

    #endregion
}
