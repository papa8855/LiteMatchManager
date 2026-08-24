using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Timers; 
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Cvars;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System;
using System.Collections.Frozen; // 【.NET 10 升級】：引入凍結集合
using System.IO; // 【新增】：用來讀取廣告黑名單 txt 檔案

namespace LiteMatchManager;

#pragma warning disable CS8618

// ==========================================
// 階段對戰 (Phase) 專用設定類別
// ==========================================
public class DuelModeConfig
{
    [JsonPropertyName("Duel_WinLimit")] public int WinLimit { get; set; } = 5;
    [JsonPropertyName("Duel_DisplayTarget")] public string DisplayTarget { get; set; } = "5"; 
    [JsonPropertyName("Duel_Name")] public string Name { get; set; } = "手槍";
    [JsonPropertyName("Duel_Health")] public int Health { get; set; } = 100;
    [JsonPropertyName("Duel_Armor")] public int Armor { get; set; } = 1; 
    [JsonPropertyName("Duel_PrimaryWeapons")] public List<string> PrimaryWeapons { get; set; } = [];
    [JsonPropertyName("Duel_SecondaryWeapons")] public List<string> SecondaryWeapons { get; set; } = [];
    [JsonPropertyName("Duel_Grenades")] public List<string> Grenades { get; set; } = [];
}

public class LiteMatchConfig : BasePluginConfig
{
    [JsonPropertyName("MaxPlayersPerTeam")] public int MaxPlayersPerTeam { get; set; } = 2; 
    
    [JsonPropertyName("FinalMatchWinScore")] public int FinalMatchWinScore { get; set; } = 30; 
    
    [JsonPropertyName("KickUnreadyPlayerTime")] public int KickUnreadyPlayerTime { get; set; } = 360;
    [JsonPropertyName("ReconnectGracePeriod")] public int ReconnectGracePeriod { get; set; } = 180;
    
    [JsonPropertyName("UnreadyReminderInterval")] public int UnreadyReminderInterval { get; set; } = 60;
    [JsonPropertyName("PublicUnreadyReminderInterval")] public int PublicUnreadyReminderInterval { get; set; } = 20;
    
    [JsonPropertyName("WaitingForOpponentInterval")] public int WaitingForOpponentInterval { get; set; } = 30;

    [JsonPropertyName("ChatPrefix")] public string ChatPrefix { get; set; } = "[ {Green}對 戰 系 統{White} ]";
    [JsonPropertyName("EnableChatWeaponCommands")] public bool EnableChatWeaponCommands { get; set; } = true;
    
    [JsonPropertyName("WarmupConfigName")] public string WarmupConfigName { get; set; } = "warmup.cfg";
    [JsonPropertyName("LiveConfigName")] public string LiveConfigName { get; set; } = "live.cfg";
    [JsonPropertyName("Duel_MapChangeDelay")] public int MapChangeDelay { get; set; } = 5;
    
    [JsonPropertyName("MapList")] 
    public List<string> MapList { get; set; } = [
        "Aim_redline_vieforit:3290337428", 
        "aimpro_vieforit:3290753343", 
        "5e_aim_map:3250592791", 
        "5e_akm4_aim_duel:3250543760"
    ];

    [JsonPropertyName("SpawnWeapons")] 
    public List<string> SpawnWeapons { get; set; } = ["weapon_knife", "weapon_deagle", "weapon_ak47", "item_assaultsuit"]; 

    [JsonPropertyName("Duel_GunMenuCommands")] 
    public List<string> GunMenuCommands { get; set; } = ["gs", "GS"];
    
    [JsonPropertyName("Duel_GunMenuMessage")] 
    public List<string> GunMenuMessage { get; set; } = [
        " {Orange}您 可 在 聊 天 欄 位 輸 入 您 要 的 武 器，以 下 是 常 用 武 器",
        " -------------------------------------------------------------------",
        " [ {LightBlue}手槍{White} ]  {LightBlue}!dg {White}[ 沙鷹 ] 、{LightBlue}!usp {White}[ USP ] 、{LightBlue}!gk {White}[ 格洛克 ] 、{LightBlue}!r8 {White}[ R8 ]",
        " [ {Orange}狙擊{White} ] {Orange}!ssg {White}[ SSG 08 鳥狙 ] 、{Orange}!awp {White}[ AWP狙擊步槍 ]",
        " [ {Green}步槍{White} ] {Green}!gr {White}[ Galil ] 、{Green}!ak {White}[ AK47 ] 、{Green}!a1 {White}[ M4A1 ] 、{Green}!a4 {White}[ M4A4 ]"
    ];
    
    [JsonPropertyName("Duel_ReadyCommands")] 
    public List<string> ReadyCommands { get; set; } = ["r", "ready", "start", "join", "duel"];

    [JsonPropertyName("Duel_WeaponCommands")] 
    public Dictionary<string, string> WeaponCommands { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        {"ak", "weapon_ak47"}, {"a4", "weapon_m4a1"}, {"gr", "weapon_galilar"},
        {"a1", "weapon_m4a1_silencer"}, {"awp", "weapon_awp"}, {"ssg", "weapon_ssg08"},
        {"dg", "weapon_deagle"}, {"usp", "weapon_usp_silencer"}, {"gk", "weapon_glock"},
        {"r8", "weapon_revolver"}
    };

    [JsonPropertyName("Duel_List")] 
    public List<DuelModeConfig> MatchModes { get; set; } = [
        new DuelModeConfig { Name = "手槍", WinLimit = 5, DisplayTarget = "５", Armor = 1, SecondaryWeapons = ["weapon_usp_silencer", "weapon_deagle", "weapon_revolver", "weapon_glock"] },
        new DuelModeConfig { Name = "狙擊", WinLimit = 7, DisplayTarget = "７", Armor = 2, PrimaryWeapons = ["weapon_awp", "weapon_ssg08"], SecondaryWeapons = ["weapon_usp_silencer", "weapon_deagle", "weapon_revolver", "weapon_glock"] },
        new DuelModeConfig { Name = "步槍", WinLimit = 99, DisplayTarget = "回合３０", Armor = 2, PrimaryWeapons = ["weapon_ak47", "weapon_m4a1", "weapon_galilar", "weapon_m4a1_silencer"], SecondaryWeapons = ["weapon_usp_silencer", "weapon_deagle", "weapon_revolver", "weapon_glock"] }
    ];

    [JsonPropertyName("HudDuration_Prep")] public float HudDuration_Prep { get; set; } = 4.0f; 
    [JsonPropertyName("HudDuration_MatchAbort")] public float HudDuration_MatchAbort { get; set; } = 3.0f;
    [JsonPropertyName("HudDuration_MatchStart")] public float HudDuration_MatchStart { get; set; } = 2.0f;
    [JsonPropertyName("RoundStartHudDuration")] public float RoundStartHudDuration { get; set; } = 2.0f;
    [JsonPropertyName("Live_Execute_Delay")] public float Live_Execute_Delay { get; set; } = 2.5f;

    [JsonPropertyName("HudHtml_Prep1v1_Line1")] public string HudHtml_Prep1v1_Line1 { get; set; } = "<font class='fontSize-l' color='lime'><b>✦</font> <font class='fontSize-l' color='white'>人 數 觸 發 <font class='fontSize-l' color='gold'>1 v 1</font> 單 挑 </font><font class='fontSize-l' color='lime'>✦</font></b><br>";
    [JsonPropertyName("HudHtml_Prep1v1_Line2")] public string HudHtml_Prep1v1_Line2 { get; set; } = "<font class='fontSize-l' color='white'><b>已 準 備：</font><font class='fontSize-l' color='lime'>{0} / 2</font><font class='fontSize-l' color='white'> 尚 缺 <font class='fontSize-l' color='lime'><b>{1}</b></font> 人</font></b>";
    [JsonPropertyName("HudHtml_Prep2v2_Line1")] public string HudHtml_Prep2v2_Line1 { get; set; } = "<font class='fontSize-l' color='lime'><b>✦</font> <font class='fontSize-l' color='white'>人 數 觸 發 <font class='fontSize-l' color='gold'>2 v 2</font> 團 戰 </font><font class='fontSize-l' color='lime'>✦</font></b><br>";
    [JsonPropertyName("HudHtml_Prep2v2_Line2")] public string HudHtml_Prep2v2_Line2 { get; set; } = "<font class='fontSize-l' color='white'><b>已 準 備：</font><font class='fontSize-l' color='lime'>{0} / {2}</font><font class='fontSize-l' color='white'> 尚 缺 <font class='fontSize-l' color='lime'><b>{1}</b></font> 人</font></b>";
    
    [JsonPropertyName("HudHtml_MatchAbort_Line1")] public string HudHtml_MatchAbort_Line1 { get; set; } = "<font class='fontSize-l' color='gold'><b>有 玩 家 逃 跑 ， 戰 鬥 已 終 止</font></b><br>";
    [JsonPropertyName("HudHtml_MatchAbort_Line2")] public string HudHtml_MatchAbort_Line2 { get; set; } = "<font class='fontSize-l' color='lime'><b>比 賽 已 退 回 暖 身 模 式</font></b>";
    
    [JsonPropertyName("HudHtml_Round1_Line1")] public string HudHtml_Round1_Line1 { get; set; } = "<font class='fontSize-l' color='gold'><b>★ {0} 戰 鬥 開 始 ★</font></b><br>";
    [JsonPropertyName("HudHtml_Round1_Line2")] public string HudHtml_Round1_Line2 { get; set; } = "<font class='fontSize-l' color='white'><b>對 戰 採</font><font class='fontSize-l' color='lime'><b>３０</b></font><font class='fontSize-l' color='white'> 回 防 勝 利 制</font></b>";
    
    [JsonPropertyName("HudHtml_RoundStart_Title")] public string HudHtml_RoundStart_Title { get; set; } = "<font class='fontSize-l' color='lime'><b>{0}回合：</b></font><font class='fontSize-l' color='gold'><b>模式 / 搶 </b></font><font class='fontSize-l' color='Green'><b>{1}</b></font><font class='fontSize-l' color='gold'><b> 勝</b></font><br>";
    [JsonPropertyName("HudHtml_RoundStart_TScore")] public string HudHtml_RoundStart_TScore { get; set; } = "<font class='fontSize-l' color='#FF4500'><b>目 前 恐 怖 份 子：{0}</b></font><br>";
    [JsonPropertyName("HudHtml_RoundStart_CTScore")] public string HudHtml_RoundStart_CTScore { get; set; } = "<font class='fontSize-l' color='lightblue'><b>目 前 反 恐 精 英：{0}</b></font><br><font class='fontSize-l' color='gold'><b>比 賽 贏 </b></font><font class='fontSize-l' color='Green'><b>３０</b></font><font class='fontSize-l' color='gold'><b> 回合 為 主</b></font>";
}

public class LiteMatchManager : BasePlugin, IPluginConfig<LiteMatchConfig>
{
    public override string ModuleName => "LiteMatchManager";
    public override string ModuleVersion => "9.30_NoLinq_AntiJitter";
    public override string ModuleAuthor => "Optimized";
    public override string ModuleDescription => "原生30勝換圖";

    public LiteMatchConfig Config { get; set; } = new();

    private string _cachedPrefix = "";
    // 【.NET 10 升級】：集合表達式
    private List<string> _cachedGunMenuMessage = []; 
    
    // 【新增】：廣告防禦黑名單 (Ad Blacklist)
    public string[] adBlacklist = [];
    
    private int _currentPhaseIndex = 0; 
    private int _phaseStartScoreT = 0;
    private int _phaseStartScoreCT = 0;
    
    // 【.NET 10 升級】：改用 FrozenSet 鎖死效能
    private FrozenSet<string> _readyCommandsSet = FrozenSet<string>.Empty;
    private FrozenSet<string> _gunMenuCommandsSet = FrozenSet<string>.Empty;
    
    // 【.NET 10 升級】：靜態常數集合採用 FrozenSet 達成極致比對速度
    private static readonly FrozenSet<string> PistolWeapons = new[]
    {
        "weapon_usp_silencer", "weapon_glock", "weapon_deagle", "weapon_revolver",
        "weapon_p250", "weapon_tec9", "weapon_fiveseven", "weapon_cz75a",
        "weapon_elite", "weapon_hkp2000"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    
    private HashSet<ulong> _readyPlayers = new(64);
    
    // 【全新防禦機制】：名冊鎖定，阻斷 ChangeTeam 造成的無限迴圈當機
    private Dictionary<ulong, int> _lockedTeam = new(64);
    
    private Dictionary<ulong, int> _playerUnreadyTime = new(64); 
    private Dictionary<ulong, float> _playerJoinTime = new(64); 
    private List<string> _unreadyNamesCache = new(64); 
    private Dictionary<ulong, string> _playerPrimary = new(64);
    private Dictionary<ulong, string> _playerSecondary = new(64);
    private Dictionary<ulong, float> _pendingInitialReminders = new(64);
    private HashSet<ulong> _hasReceivedInitialReminder = new(64);

    private List<CCSPlayerController> _serverPlayersCache = new(64);

    private bool _isMatchLive = false;
    private bool _isChangingMap = false; 
    private int _liveMatchTargetPlayers = 0; 
    private bool _isServerShuttingDown = false; 
    
    private CounterStrikeSharp.API.Modules.Timers.Timer? _privateCheckTimer;
    private CounterStrikeSharp.API.Modules.Timers.Timer? _publicBroadcastTimer;
    private CounterStrikeSharp.API.Modules.Timers.Timer? _waitingTimer;
    private CounterStrikeSharp.API.Modules.Timers.Timer? _liveTimer; 

    private CCSTeam? _cachedTeamT = null;
    private CCSTeam? _cachedTeamCT = null;

    private string _activeCenterMessage = "";
    private float _centerMessageExpiration = 0f;

    private string ReplaceColorTags(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        return input
            .Replace("{Default}", ChatColors.Default.ToString())
            .Replace("{White}", ChatColors.White.ToString())
            .Replace("{Red}", ChatColors.Red.ToString())
            .Replace("{Green}", ChatColors.Green.ToString())
            .Replace("{Lime}", ChatColors.Lime.ToString())
            .Replace("{LightBlue}", ChatColors.LightBlue.ToString())
            .Replace("{Yellow}", ChatColors.Yellow.ToString())
            .Replace("{Gold}", ChatColors.Gold.ToString())
            .Replace("{Orange}", ChatColors.Orange.ToString())
            .Replace("{Grey}", ChatColors.Grey.ToString());
    }

    public void OnConfigParsed(LiteMatchConfig config)
    {
        var caseInsensitiveDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        // 【.NET 10 升級】：字典解構
        foreach (var (key, value) in config.WeaponCommands) caseInsensitiveDict[key] = value;
        config.WeaponCommands = caseInsensitiveDict;

        // 【.NET 10 升級】：將設定檔傳入的指令直接轉換為凍結集合
        _readyCommandsSet = config.ReadyCommands.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
        _gunMenuCommandsSet = config.GunMenuCommands.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

        Config = config;
        
        _cachedPrefix = ReplaceColorTags(config.ChatPrefix);
        
        _cachedGunMenuMessage.Clear();
        if (config.GunMenuMessage != null)
        {
            foreach (var line in config.GunMenuMessage)
            {
                _cachedGunMenuMessage.Add(ReplaceColorTags(line));
            }
        }
    }

    private void EnsureTeamEntitiesCached()
    {
        if (_cachedTeamT is { IsValid: true } && _cachedTeamCT is { IsValid: true }) return;

        foreach (var team in Utilities.FindAllEntitiesByDesignerName<CCSTeam>("cs_team_manager"))
        {
            if (team.TeamNum == 2) _cachedTeamT = team;
            else if (team.TeamNum == 3) _cachedTeamCT = team;
        }
    }

    private bool IsMatchOver()
    {
        if (_isChangingMap) return true;
        
        EnsureTeamEntitiesCached();
        int scoreT = _cachedTeamT?.Score ?? 0;
        int scoreCT = _cachedTeamCT?.Score ?? 0;
        
        if (scoreT >= Config.FinalMatchWinScore || scoreCT >= Config.FinalMatchWinScore)
            return true;
            
        return false;
    }

    private void RefreshActivePlayers()
    {
        _serverPlayersCache.Clear();
        foreach (var p in Utilities.GetPlayers())
        {
            if (p is { IsValid: true, Handle: not 0 })
            {
                _serverPlayersCache.Add(p);
            }
        }
    }

    private bool IsStringInList(List<string> list, string target)
    {
        foreach (var item in list)
        {
            if (string.Equals(item, target, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private void ShowHud(string html, float duration)
    {
        _activeCenterMessage = html;
        _centerMessageExpiration = Server.CurrentTime + duration;
    }

    private void OnTick()
    {
        if (!string.IsNullOrEmpty(_activeCenterMessage))
        {
            if (Server.CurrentTime <= _centerMessageExpiration)
            {
                foreach (var p in _serverPlayersCache)
                {
                    if (p is { IsValid: true, TeamNum: 2 or 3 }) p.PrintToCenterHtml(_activeCenterMessage);
                }
            }
            else
            {
                _activeCenterMessage = "";
                foreach (var p in _serverPlayersCache)
                {
                    if (p is { IsValid: true, TeamNum: 2 or 3 }) p.PrintToCenterHtml("&#8203;", 0);
                }
            }
        }
    }

    private void CheckPendingReminders()
    {
        if (_pendingInitialReminders.Count == 0) return;

        float currentTime = Server.CurrentTime;
        List<ulong>? toRemove = null;

        foreach (var p in _serverPlayersCache)
        {
            if (p is { IsValid: true, SteamID: > 0, TeamNum: 2 or 3 })
            {
                ulong steamId = p.SteamID;
                if (_pendingInitialReminders.TryGetValue(steamId, out float triggerTime))
                {
                    if (currentTime >= triggerTime)
                    {
                        // 【.NET 10 升級】：集合表達式
                        if (toRemove == null) toRemove = [];
                        toRemove.Add(steamId);

                        if (!_isMatchLive && !_readyPlayers.Contains(steamId))
                        {
                            _playerUnreadyTime.TryGetValue(steamId, out int elapsed);
                            int timeLeft = Config.KickUnreadyPlayerTime - elapsed;
                            p.PrintToChat($" {_cachedPrefix} 請輸入 {ChatColors.Lime}!R{ChatColors.White} 準備 ，{ChatColors.Lime}{timeLeft}{ChatColors.White} 秒未準備將被踢出");
                            p.PrintToCenter($"請輸入 !R 準備，{timeLeft} 秒後將被踢出"); 
                        }
                    }
                }
            }
        }

        // 【.NET 10 升級】：字典解構
        foreach (var (key, value) in _pendingInitialReminders)
        {
            if (currentTime >= value)
            {
                if (toRemove == null) toRemove = [];
                if (!toRemove.Contains(key)) toRemove.Add(key);
            }
        }

        if (toRemove != null) 
        {
            foreach (var id in toRemove) _pendingInitialReminders.Remove(id);
        }
    }

    public override void Load(bool hotReload)
    {
        Console.WriteLine("=================================================");
        Console.WriteLine("  LiteMatchManager (究極防抖動 + 無 LINQ 版) 啟動！");
        Console.WriteLine("=================================================");

        _isServerShuttingDown = false;
        
        LoadAdBlacklist(); // 【新增】：載入廣告黑名單

        AddCommandListener("say", OnPlayerSay);
        AddCommandListener("say_team", OnPlayerSay);
        AddCommandListener("jointeam", OnJoinTeam);
        AddCommandListener("drop", (player, info) => HookResult.Handled);
        
        RegisterListener<Listeners.OnTick>(OnTick);
        AddTimer(1.0f, CheckPendingReminders, TimerFlags.REPEAT);

        RegisterEventHandler<EventMapShutdown>((@event, info) => { _isServerShuttingDown = true; return HookResult.Continue; });
        // ▼▼▼ 新增：防禦機器人進場後「偷偷改名」的第二道防線 ▼▼▼
        RegisterEventHandler<EventPlayerChangename>((@event, info) => {
            var changedPlayer = @event.Userid;
            string newName = @event.Newname;

            if (changedPlayer is { IsValid: true, IsBot: false } && !string.IsNullOrEmpty(newName) && adBlacklist.Length > 0)
            {
                bool isAdName = false;
                foreach (var ad in adBlacklist)
                {
                    if (newName.Contains(ad, StringComparison.OrdinalIgnoreCase))
                    {
                        isAdName = true;
                        break;
                    }
                }

                if (isAdName)
                {
                    Console.WriteLine($"[廣告防禦] 偵測到違規改名，瞬間 Ban 掉: {newName} (SteamID: {changedPlayer.SteamID})");
                    Server.ExecuteCommand($"css_ban #{changedPlayer.UserId} 0 \"廣告機器人(改名)\"");
                }
            }
            return HookResult.Continue;
        });
        // ▲▲▲ 第二道防線結束 ▲▲▲

        // ▼▼▼ 新增：廣告防禦門神 (進場名稱秒踢與永久封鎖) ▼▼▼
        RegisterEventHandler<EventPlayerConnectFull>((@event, info) => {
            var player = @event.Userid;

            if (player is { IsValid: true, IsBot: false } && !string.IsNullOrEmpty(player.PlayerName) && adBlacklist.Length > 0)
            {
                bool isAdName = false;
                foreach (var ad in adBlacklist)
                {
                    if (player.PlayerName.Contains(ad, StringComparison.OrdinalIgnoreCase))
                    {
                        isAdName = true;
                        break;
                    }
                }

                if (isAdName)
                {
                    Console.WriteLine($"[廣告防禦] 偵測到違規名稱，進場秒 Ban: {player.PlayerName} (SteamID: {player.SteamID})");
                    Server.ExecuteCommand($"css_ban #{player.UserId} 0 \"廣告機器人封鎖\""); 
                    Server.ExecuteCommand($"kickid {player.UserId} \"Ban_Ads\""); 
                    return HookResult.Continue;
                }
            }
            return HookResult.Continue;
        });
        // ▲▲▲ 新增結束 ▲▲▲

        RegisterEventHandler<EventPlayerDisconnect>((@event, info) =>
        {
            if (@event.Userid is { SteamID: > 0 } player)
            {
                ulong steamId = player.SteamID;
                string pName = player.PlayerName;

                if (_isMatchLive && _readyPlayers.Contains(steamId))
                {
                    _readyPlayers.Remove(steamId);
                    _lockedTeam.Remove(steamId); // 釋放名冊鎖

                    if (IsMatchOver()) return HookResult.Continue;

                    if (_liveMatchTargetPlayers == 2)
                    {
                        Server.PrintToChatAll($" {_cachedPrefix} 玩 家 {ChatColors.Gold}{pName} {ChatColors.White}斷 線，比 賽 強 制 終 止");
                        Server.NextFrame(AbortMatch); // 【防記憶體報錯】必須包裝在 NextFrame 內
                    }
                    else
                    {
                        Server.PrintToChatAll($" {_cachedPrefix} 玩 家 {ChatColors.Gold}{pName} {ChatColors.Orange}斷 線，已 釋 出 名 額，開 放 補 位");
                        Server.NextFrame(CheckPhaseWin); 
                    }
                }
                else
                {
                    _readyPlayers.Remove(steamId);
                    _lockedTeam.Remove(steamId);
                    _playerUnreadyTime.Remove(steamId);
                    _playerJoinTime.Remove(steamId); 
                    _playerPrimary.Remove(steamId);
                    _playerSecondary.Remove(steamId);
                    _pendingInitialReminders.Remove(steamId);
                    _hasReceivedInitialReminder.Remove(steamId);
                    if (!_isMatchLive) Server.NextFrame(CheckMatchStart);
                }
            }
            Server.NextFrame(RefreshActivePlayers);
            return HookResult.Continue;
        });

        RegisterEventHandler<EventPlayerTeam>((@event, info) =>
        {
            if (@event.Userid is { IsValid: true, Handle: not 0 } player)
            {
                ulong steamId = player.SteamID;
                int newTeam = @event.Team; 
                
                if (newTeam is 0 or 1) 
                {
                    // 【修正】：玩家跳去觀戰或選隊伍介面的瞬間，直接強制清除他的畫面，防止白框殘留
                    player.PrintToCenterHtml("&#8203;", 0);

                    _pendingInitialReminders.Remove(steamId);
                    _hasReceivedInitialReminder.Remove(steamId);
                    _playerJoinTime.Remove(steamId); 

                    if (_isMatchLive && _readyPlayers.Contains(steamId))
                    {
                        _readyPlayers.Remove(steamId);
                        _lockedTeam.Remove(steamId); // 釋放名冊鎖
                        
                        if (IsMatchOver()) return HookResult.Continue;
                        
                        if (_liveMatchTargetPlayers == 2)
                        {
                            Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Gold}{player.PlayerName} {ChatColors.White}放 棄 比 賽，強 制 終 止");
                            Server.NextFrame(AbortMatch); 
                        }
                        else
                        {
                            Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Gold}{player.PlayerName} {ChatColors.White}離 開 隊 伍，已 釋 出 補 位 名 額");
                            Server.NextFrame(CheckPhaseWin);
                        }
                    }
                    else if (_readyPlayers.Remove(steamId))
                    {
                        Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Gold}{player.PlayerName}{ChatColors.White} 跳 去 觀 戰，已 取 消 準 備");
                    }
                    _playerUnreadyTime.Remove(steamId); 
                    if (!_isMatchLive) Server.NextFrame(CheckMatchStart);
                }
                else if (newTeam is 2 or 3)
                {
                    if (_isMatchLive)
                    {
                        // 1. 比賽中：原本已準備的玩家，企圖換到敵方隊伍
                        if (_readyPlayers.Contains(steamId))
                        {
                            // 【核心防護】對照陣營名冊，阻断 ChangeTeam 造成的無限迴圈
                            if (_lockedTeam.TryGetValue(steamId, out int lockedTeam))
                            {
                                if (newTeam != lockedTeam)
                                {
                                    Server.NextFrame(() => {
                                        if (player.IsValid) {
                                            player.ChangeTeam((CsTeam)lockedTeam); // 踢回名冊指定隊伍
                                            player.PrintToChat($" {_cachedPrefix} {ChatColors.Orange}對 戰 進 行 中，無 法 切 換 隊 伍");
                                        }
                                    });
                                    return HookResult.Continue;
                                }
                            }
                        }
                        // 2. 比賽中：未準備的玩家試圖加入
                        else 
                        {
                            if (_liveMatchTargetPlayers == 2)
                            {
                                Server.NextFrame(() => {
                                    if (player.IsValid) {
                                        player.ChangeTeam(CsTeam.Spectator);
                                        player.PrintToChat($" {_cachedPrefix} {ChatColors.Orange}單 挑 比 賽 進 行 中，不 開 放 加 入");
                                    }
                                });
                            }
                            else 
                            {
                                int liveTeamMax = _liveMatchTargetPlayers / 2;
                                int currentCount = 0;
                                foreach (var p in Utilities.GetPlayers())
                                {
                                    if (p is { IsValid: true, IsBot: false, TeamNum: var team } && team == newTeam && p.SteamID != steamId)
                                        currentCount++;
                                }
                                
                                if (currentCount >= liveTeamMax)
                                {
                                    Server.NextFrame(() => {
                                        if (player.IsValid) {
                                            player.ChangeTeam(CsTeam.Spectator);
                                            player.PrintToChat($" {_cachedPrefix} {ChatColors.Orange}該 隊 伍 補 位 已 滿，無 法 加 入");
                                        }
                                    });
                                }
                                else 
                                {
                                    _readyPlayers.Add(steamId);
                                    _lockedTeam[steamId] = newTeam; // 補位成功，寫入名冊鎖
                                    Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Gold}玩 家 {player.PlayerName} {ChatColors.White}成 功 補 位 加 加 入 團 戰 比 賽");
                                }
                            }
                        }
                        Server.NextFrame(CheckPhaseWin);
                    }
                    else // !_isMatchLive
                    {
                        // 3. 非比賽期間：檢查是否超過滿員設定
                        int currentTeamCount = 0;
                        foreach (var p in Utilities.GetPlayers())
                        {
                            if (p is { IsValid: true, Handle: not 0, IsBot: false, TeamNum: var team } && team == newTeam && p.SteamID != steamId)
                                currentTeamCount++;
                        }
                        
                        if (currentTeamCount >= Config.MaxPlayersPerTeam)
                        {
                            Server.NextFrame(() => {
                                if (player.IsValid) {
                                    player.ChangeTeam(CsTeam.Spectator); 
                                    string teamName = newTeam == 2 ? "恐怖份子 (T)" : "反恐小組 (CT)";
                                    player.PrintToChat($" {_cachedPrefix} {ChatColors.Orange}加 入 失 敗！{teamName} 已 經 滿 員 ( 最 多 {ChatColors.Green}{Config.MaxPlayersPerTeam}{ChatColors.Orange} 人 )");
                                }
                            });
                        }
                        Server.NextFrame(CheckMatchStart);
                    }
                }
            }
            Server.NextFrame(RefreshActivePlayers);
            return HookResult.Continue;
        }, HookMode.Post);

        RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
        RegisterEventHandler<EventRoundStart>(OnEventRoundStart);
        RegisterEventHandler<EventCsWinPanelMatch>(OnMatchEnd);

        RegisterEventHandler<EventRoundEnd>((@event, info) => {
            if (_isMatchLive) Server.NextFrame(CheckPhaseWin);
            return HookResult.Continue;
        });

        RegisterListener<Listeners.OnMapStart>(mapName => 
        {
            _isServerShuttingDown = false;
            _cachedTeamT = null;
            _cachedTeamCT = null;

            ResetMatchState();
            Server.NextFrame(() => Server.ExecuteCommand($"exec {Config.WarmupConfigName}"));
            
            Server.NextFrame(RefreshActivePlayers);
        });

        Server.NextFrame(RefreshActivePlayers);
    }

    private int GetDynamicRequiredPlayers()
    {
        int activeT = 0, activeCT = 0;
        foreach (var p in Utilities.GetPlayers())
        {
            if (p is { IsValid: true, Handle: not 0, IsBot: false, IsHLTV: false })
            {
                if (p.TeamNum == 2) activeT++;
                else if (p.TeamNum == 3) activeCT++;
            }
        }
        int total = activeT + activeCT;
        if (total <= 2) return 2;
        
        int target = (total % 2 == 1) ? total + 1 : total;
        int absoluteMax = Config.MaxPlayersPerTeam * 2;
        return target > absoluteMax ? absoluteMax : target;
    }

    private void CheckPhaseWin()
    {
        if (_isServerShuttingDown || !_isMatchLive || IsMatchOver()) return; 
        
        int activeT = 0, activeCT = 0;
        foreach (var p in Utilities.GetPlayers())
        {
            if (p is { IsValid: true, Handle: not 0, IsBot: false })
            {
                if (p.TeamNum == 2) activeT++;
                else if (p.TeamNum == 3) activeCT++;
            }
        }
        if (activeT == 0 || activeCT == 0) { AbortMatch(); return; }

        if (Config.MatchModes.Count == 0 || _currentPhaseIndex >= Config.MatchModes.Count) return;

        EnsureTeamEntitiesCached();

        int scoreT = _cachedTeamT?.Score ?? 0;
        int scoreCT = _cachedTeamCT?.Score ?? 0;
        
        int currentPhaseScoreT = scoreT - _phaseStartScoreT;
        int currentPhaseScoreCT = scoreCT - _phaseStartScoreCT;
        
        var currentPhase = Config.MatchModes[_currentPhaseIndex];

        if (currentPhaseScoreT >= currentPhase.WinLimit || currentPhaseScoreCT >= currentPhase.WinLimit)
        {
            _currentPhaseIndex++; 
            
            if (_currentPhaseIndex < Config.MatchModes.Count)
            {
                _phaseStartScoreT = scoreT;
                _phaseStartScoreCT = scoreCT;
                
                var nextPhase = Config.MatchModes[_currentPhaseIndex];
                Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Lime}階 段 結 束！{ChatColors.Gold}進 入 更 換【 {ChatColors.Green}{nextPhase.Name}{ChatColors.Gold} 】模 式");
            }
        }
    }

    private HookResult OnMatchEnd(EventCsWinPanelMatch @event, GameEventInfo info)
    {
        if (!_isMatchLive || _isChangingMap) return HookResult.Continue;

        int scoreT = 0, scoreCT = 0;

        EnsureTeamEntitiesCached();

        if (_cachedTeamT != null) scoreT = _cachedTeamT.Score;
        if (_cachedTeamCT != null) scoreCT = _cachedTeamCT.Score;

        string winnerName = scoreT > scoreCT ? "恐怖份子" : "反恐小組";
        string loserName = scoreT > scoreCT ? "反恐小組" : "恐怖份子";
        
        if (_liveMatchTargetPlayers == 2)
        {
            string nameT = "恐怖份子";
            string nameCT = "反恐小組";
            foreach (var p in Utilities.GetPlayers())
            {
                if (p is { IsValid: true, Handle: not 0, IsBot: false, TeamNum: 2 }) nameT = p.PlayerName;
                else if (p is { IsValid: true, Handle: not 0, IsBot: false, TeamNum: 3 }) nameCT = p.PlayerName;
            }
            winnerName = scoreT > scoreCT ? nameT : nameCT;
            loserName = scoreT > scoreCT ? nameCT : nameT;
        }

        int winnerScore = Math.Max(scoreT, scoreCT);
        int loserScore = Math.Min(scoreT, scoreCT);

        Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Lime}{winnerName} {ChatColors.Gold}以 {ChatColors.Green} {winnerScore} : {loserScore} {ChatColors.Gold}贏得了最終勝利");

        TriggerMapChange();
        return HookResult.Continue;
    }

    private void AbortMatch()
    {
        if (!_isMatchLive || IsMatchOver()) return;
        
        _liveTimer?.Kill(); _liveTimer = null;

        ResetMatchState();
        
        ShowHud($"{Config.HudHtml_MatchAbort_Line1}<br>{Config.HudHtml_MatchAbort_Line2}<br>", Config.HudDuration_MatchAbort);
        Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Lime}玩 家 {ChatColors.Orange}離 退 對 戰 終 止，請 重 新 輸 入 {ChatColors.Lime}!R {ChatColors.Orange}對 戰");

        AddTimer(Config.HudDuration_MatchAbort, () =>
        {
            Server.ExecuteCommand("mp_warmup_start");
            
            var pauseConVar = ConVar.Find("mp_warmup_pausetimer");
            if (pauseConVar is not null) pauseConVar.SetValue(1);
            else Server.ExecuteCommand("mp_warmup_pausetimer 1");
            
            Server.NextFrame(() => Server.ExecuteCommand($"exec {Config.WarmupConfigName}"));
        });
    }

    private HookResult OnJoinTeam(CCSPlayerController? player, CommandInfo info)
    {
        return HookResult.Continue; // 完全放行，由 EventPlayerTeam 透過名冊鎖進行防禦
    }

    private HookResult OnPlayerSay(CCSPlayerController? player, CommandInfo info)
    {
        if (player is not { IsValid: true }) return HookResult.Continue;
        string rawArg = info.GetArg(1);
        if (string.IsNullOrWhiteSpace(rawArg)) return HookResult.Continue;

        // ▼▼▼ 新增：廣告防禦門神 (文字黑洞吞噬與永久封鎖) ▼▼▼
        if (adBlacklist.Length > 0)
        {
            bool isSpam = false;
            foreach (var ad in adBlacklist)
            {
                if (rawArg.Contains(ad, StringComparison.OrdinalIgnoreCase))
                {
                    isSpam = true;
                    break;
                }
            }

            if (isSpam)
            {
                Console.WriteLine($"[廣告防禦] 攔截到洗頻訊息並直接吞掉: {rawArg}");
                Server.ExecuteCommand($"css_ban #{player.UserId} 0 \"廣告洗頻\"");
                Server.ExecuteCommand($"kickid {player.UserId} \"Ban_Ads\"");
                return HookResult.Handled; 
            }
        }
        // ▲▲▲ 新增結束 ▲▲▲

        bool isCommand = false;
        for (int i = 0; i < rawArg.Length; i++)
        {
            char c = rawArg[i];
            if (c is '"' or ' ') continue; 
            if (c is '!' or '/') { isCommand = true; break; }
            break; 
        }

        if (!isCommand) 
        {
            // 【.NET 10 升級】：改用 Span 切片去除多餘符號，0 記憶體分配
            string message = rawArg.AsSpan().Trim(" \"").ToString(); 
            if (string.IsNullOrWhiteSpace(message)) return HookResult.Continue;
            string playerName = player.PlayerName;
            string nameColor = player.TeamNum switch { 1 => $"{ChatColors.Grey}", 2 => "\x10", 3 => "\x0B", _ => $"{ChatColors.White}" };
            string formattedMessage = $" {ChatColors.White}[所有人]{ChatColors.White} {nameColor}{playerName}{ChatColors.White}：{message}";

            foreach (var p in Utilities.GetPlayers()) if (p is { IsValid: true, IsBot: false }) p.PrintToChat(formattedMessage);
            return HookResult.Handled; 
        }

        // 【.NET 10 升級】：改用 Span 切片去除多餘符號，0 記憶體分配
        string command = rawArg.AsSpan().Trim(" \"!/").ToString().ToLower();

        if (command == "nextmap" && AdminManager.PlayerHasPermissions(player, "@css/root"))
        {
            TriggerMapChange(); return HookResult.Handled; 
        }

        if (_readyCommandsSet.Contains(command))
        {
            if (!_isMatchLive) HandlePlayerReady(player);
            return HookResult.Continue; 
        }
        else if (command == "unready")
        {
            if (!_isMatchLive) HandlePlayerUnready(player);
            return HookResult.Continue; 
        }

        if (Config.EnableChatWeaponCommands)
        {
            if (_gunMenuCommandsSet.Contains(command))
            {
                OnGsCommand(player); return HookResult.Continue;
            }
            if (Config.WeaponCommands.TryGetValue(command, out string? realWeaponName))
            {
                TryGiveWeaponByCommand(player, realWeaponName);
                return HookResult.Continue;
            }
        }
        
        return HookResult.Continue;
    }

    private void TryGiveWeaponByCommand(CCSPlayerController player, string weaponName)
    {
        if (!player.PawnIsAlive) return;

        if (!_isMatchLive || Config.MatchModes.Count == 0)
        {
            ReplaceWeapon(player, weaponName);
            return;
        }

        var phase = Config.MatchModes[_currentPhaseIndex];
        
        bool isPrimary = IsStringInList(phase.PrimaryWeapons, weaponName);
        bool isSecondary = IsStringInList(phase.SecondaryWeapons, weaponName);

        if (!isPrimary && !isSecondary)
        {
            player.PrintToChat($" {_cachedPrefix} 當 前 為 【 {ChatColors.Gold}{phase.Name}{ChatColors.White} 】 模 式，禁 止 使 用 該 武 器");
            player.PrintToCenter("該 模 式 不 能 使 用 這 把 武 器");
            return;
        }

        if (isPrimary) _playerPrimary[player.SteamID] = weaponName;
        if (isSecondary) _playerSecondary[player.SteamID] = weaponName;
        ReplaceWeapon(player, weaponName);
    }

    private void TriggerMapChange()
    {
        if (_isChangingMap || Config.MapList is null || Config.MapList.Count == 0) return;
        _isChangingMap = true; 
        
        var random = new Random();
        
        // 【.NET 10 升級】：改用 Span 切片，徹底消除 Split 帶來的字串陣列分配
        ReadOnlySpan<char> selectedMapSpan = Config.MapList[random.Next(Config.MapList.Count)].AsSpan();
        int colonIndex = selectedMapSpan.IndexOf(':');
        string mapName = colonIndex == -1 ? selectedMapSpan.ToString() : selectedMapSpan[..colonIndex].ToString();
        string workshopId = colonIndex == -1 ? "" : selectedMapSpan[(colonIndex + 1)..].ToString();

        Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Lime}5 秒{ChatColors.White} {ChatColors.Gold}後 自動載入下一張地圖：{ChatColors.Lime}{mapName} ...");

        AddTimer(Config.MapChangeDelay, () =>
        {
            if (!string.IsNullOrEmpty(workshopId)) Server.ExecuteCommand($"host_workshop_map {workshopId}");
            else Server.ExecuteCommand($"map {mapName}");
        });
    }

    private void HandlePlayerReady(CCSPlayerController player)
    {
        if (player.TeamNum is 0 or 1) 
        { 
            player.PrintToChat($" {_cachedPrefix} {ChatColors.Gold}您 無 法 從 旁 觀 者 模 式 加 入 對 戰"); 
            player.PrintToCenter("您 無 法 從 旁 觀 者 模 式 加 入 對 戰");
            return; 
        }
        ulong steamId = player.SteamID;
        if (!_readyPlayers.Add(steamId)) 
        { 
            player.PrintToChat($" {_cachedPrefix} 你已經是 {ChatColors.Green}準備完成{ChatColors.White} 的狀態了！"); 
            player.PrintToCenter("您已經是 準 備 完 成 的狀態"); 
            return; 
        }

        _playerUnreadyTime.Remove(steamId); 
        _pendingInitialReminders.Remove(steamId); 

        int targetPlayers = GetDynamicRequiredPlayers();
        int missingPlayers = targetPlayers - _readyPlayers.Count;
        
        Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Gold}{player.PlayerName}{ChatColors.White} 已 準 備！準 備 進 度：{ChatColors.Green}{_readyPlayers.Count} / {targetPlayers}");
        
        if (targetPlayers > 2)
        {
            string prepString = $"{Config.HudHtml_Prep2v2_Line1}<br>{string.Format(Config.HudHtml_Prep2v2_Line2, _readyPlayers.Count, missingPlayers, targetPlayers)}<br>";
            ShowHud(prepString, Config.HudDuration_Prep);
        }

        CheckMatchStart();

        if (!_isMatchLive)
        {
            int activePlayers = 0;
            foreach (var p in Utilities.GetPlayers()) if (p is { IsValid: true, IsBot: false, IsHLTV: false, TeamNum: 2 or 3 }) activePlayers++;
            if (activePlayers > 0 && activePlayers == _readyPlayers.Count)
            {
                BroadcastWaitingMessage();
                _waitingTimer?.Kill();
                _waitingTimer = AddTimer(Config.WaitingForOpponentInterval, BroadcastWaitingMessage, TimerFlags.REPEAT);
            }
        }
    }

    private void HandlePlayerUnready(CCSPlayerController player)
    {
        ulong steamId = player.SteamID;
        if (_readyPlayers.Remove(steamId)) 
        {
            _playerUnreadyTime[steamId] = 0; 
            _playerJoinTime[steamId] = Server.CurrentTime; 
            
            int targetPlayers = GetDynamicRequiredPlayers();
            int missingPlayers = targetPlayers - _readyPlayers.Count;
            
            Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Gold}{player.PlayerName}{ChatColors.White} 取 消 了 準 備！準 備 進 度：{ChatColors.Green}{_readyPlayers.Count} / {targetPlayers}");
            
            if (targetPlayers > 2)
            {
                string prepString = $"{Config.HudHtml_Prep2v2_Line1}<br>{string.Format(Config.HudHtml_Prep2v2_Line2, _readyPlayers.Count, missingPlayers, targetPlayers)}<br>";
                ShowHud(prepString, Config.HudDuration_Prep);
            }
        }
    }

    private void CheckMatchStart()
    {
        if (_isMatchLive) return;

        int activeT = 0, activeCT = 0;
        foreach (var p in Utilities.GetPlayers())
        {
            if (p is { IsValid: true, Handle: not 0, IsBot: false, IsHLTV: false })
            {
                if (p.TeamNum == 2) activeT++;
                else if (p.TeamNum == 3) activeCT++;
            }
        }
        int totalPlayers = activeT + activeCT;

        if (totalPlayers < 2 || activeT != activeCT || activeT > Config.MaxPlayersPerTeam) return; 

        if (_readyPlayers.Count >= totalPlayers)
        {
            _isMatchLive = true;
            _currentPhaseIndex = 0; 
            _liveMatchTargetPlayers = totalPlayers; 
            
            _phaseStartScoreT = 0;
            _phaseStartScoreCT = 0;
            
            _playerPrimary.Clear();
            _playerSecondary.Clear();
            _lockedTeam.Clear(); // 清空舊名冊
            
            // 【產生名冊鎖】：開賽瞬間紀錄所有存活玩家的隊伍
            foreach (var p in Utilities.GetPlayers())
            {
                if (p is { IsValid: true, Handle: not 0, IsBot: false, IsHLTV: false, TeamNum: 2 or 3 })
                {
                    if (_readyPlayers.Contains(p.SteamID))
                    {
                        _lockedTeam[p.SteamID] = p.TeamNum;
                    }
                }
            }
            
            string modeText = totalPlayers == 2 ? "1 v 1 " : $"{activeT} v {activeCT} ";
            string phaseName = Config.MatchModes.Count > 0 ? Config.MatchModes[0].Name : "預設";
            string displayLimit = Config.MatchModes.Count > 0 ? Config.MatchModes[0].DisplayTarget : "20";

            string hudStartText = $"{string.Format(Config.HudHtml_Round1_Line1, modeText)}<br>{string.Format(Config.HudHtml_Round1_Line2, displayLimit)}<br>";
            ShowHud(hudStartText, Config.HudDuration_MatchStart);
            
            Server.PrintToChatAll($" {_cachedPrefix} 所 有 玩 家 已 準 備，{ChatColors.Gold}{modeText}{ChatColors.White} 比 賽 開 始");

            if (activeT >= 2 && activeCT >= 2)
            {
                Console.WriteLine("[ 2 v 2 團 戰 ] 比 賽 開 始");
            }
            
            _privateCheckTimer?.Kill(); _privateCheckTimer = null;
            _publicBroadcastTimer?.Kill(); _publicBroadcastTimer = null;
            _waitingTimer?.Kill(); _waitingTimer = null;
            
            _liveTimer?.Kill();
            _liveTimer = AddTimer(Config.Live_Execute_Delay, () => 
            {
                Server.NextFrame(() => { 
                    Server.ExecuteCommand($"exec {Config.LiveConfigName}"); 
                });
                _liveTimer = null;
            });
        }
    }

    private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        if (@event.Userid is not { IsValid: true } player) return HookResult.Continue;
        
        ulong steamId = player.SteamID;

        if (!_isMatchLive && player.TeamNum is 2 or 3)
        {
            if (!_readyPlayers.Contains(steamId) && _hasReceivedInitialReminder.Add(steamId))
            {
                _pendingInitialReminders[steamId] = Server.CurrentTime + 5.0f;
                _playerJoinTime[steamId] = Server.CurrentTime; 
            }
        }
        
        Server.NextFrame(() => {
            Server.NextFrame(() => {
                if (player is not { IsValid: true, PawnIsAlive: true } || player.PlayerPawn.Value is not { IsValid: true } pawn) return;
                
                // 【嚴格保護】防止對已經被踢去觀戰的「幽靈玩家」進行武器剝奪
                if (player.TeamNum is not 2 and not 3) return;

                player.RemoveWeapons(); 
                
                if (_isMatchLive && Config.MatchModes.Count > _currentPhaseIndex)
                {
                    var phase = Config.MatchModes[_currentPhaseIndex];
                    
                    pawn.Health = phase.Health;
                    
                    if (phase.Armor == 0) { pawn.ArmorValue = 0; }
                    else if (phase.Armor == 1) { player.GiveNamedItem("item_kevlar"); }
                    else if (phase.Armor == 2) { player.GiveNamedItem("item_assaultsuit"); }

                    player.GiveNamedItem("weapon_knife"); 

                    string secToGive = phase.SecondaryWeapons.Count > 0 ? phase.SecondaryWeapons[0] : "";
                    if (_playerSecondary.TryGetValue(steamId, out string? prefSec) && IsStringInList(phase.SecondaryWeapons, prefSec))
                        secToGive = prefSec;
                    if (!string.IsNullOrEmpty(secToGive)) player.GiveNamedItem(secToGive);

                    string priToGive = phase.PrimaryWeapons.Count > 0 ? phase.PrimaryWeapons[0] : "";
                    if (_playerPrimary.TryGetValue(steamId, out string? prefPri) && IsStringInList(phase.PrimaryWeapons, prefPri))
                        priToGive = prefPri;
                    if (!string.IsNullOrEmpty(priToGive)) player.GiveNamedItem(priToGive);

                    if (phase.Grenades != null) foreach(var nade in phase.Grenades) player.GiveNamedItem(nade);
                }
                else
                {
                    foreach (var item in Config.SpawnWeapons) 
                    { 
                        string weaponToGive = item;
                        if (item.StartsWith("weapon_") && !item.Contains("knife") && !item.Contains("bayonet"))
                        {
                            if (_playerPrimary.TryGetValue(steamId, out string? prefPri)) weaponToGive = prefPri;
                        }
                        player.GiveNamedItem(weaponToGive); 
                    }
                }
            });
        });
        
        return HookResult.Continue;
    }

    private void ReplaceWeapon(CCSPlayerController player, string newWeapon)
    {
        if (player.PlayerPawn.Value?.WeaponServices?.MyWeapons is not { } weapons) return;

        bool isNewWeaponPistol = PistolWeapons.Contains(newWeapon);

        foreach (var weaponHandle in weapons)
        {
            if (weaponHandle.Value is { IsValid: true } weapon)
            {
                string wName = weapon.DesignerName;
                if (string.IsNullOrEmpty(wName) || wName.Contains("knife") || wName.Contains("bayonet") || wName.Contains("c4")) continue;

                bool isCurrentWeaponPistol = PistolWeapons.Contains(wName);

                if (isNewWeaponPistol == isCurrentWeaponPistol)
                {
                    weapon.Remove();
                    Server.NextFrame(() => {
                        if (player is { IsValid: true, PawnIsAlive: true }) player.GiveNamedItem(newWeapon);
                    });
                    return; 
                }
            }
        }
        player.GiveNamedItem(newWeapon);
    }

    private void OnGsCommand(CCSPlayerController player)
    {
        foreach (var line in _cachedGunMenuMessage)
        {
            player.PrintToChat(line);
        }
    }

    private void CheckAndWarnUnreadyPlayers()
    {
        if (_isMatchLive) return; 

        foreach (var p in _serverPlayersCache)
        {
            if (p is { IsValid: true, Handle: not 0, IsBot: false, IsHLTV: false, TeamNum: 2 or 3 })
            {
                ulong steamId = p.SteamID;
                if (!_readyPlayers.Contains(steamId))
                {
                    _playerJoinTime.TryGetValue(steamId, out float joinTime);
                    if (joinTime > 0 && (Server.CurrentTime - joinTime) < (Config.UnreadyReminderInterval - 2.0f))
                    {
                        continue; 
                    }

                    _playerUnreadyTime.TryGetValue(steamId, out int currentTime);
                    _playerUnreadyTime[steamId] = currentTime + Config.UnreadyReminderInterval;

                    if (_playerUnreadyTime[steamId] >= Config.KickUnreadyPlayerTime) 
                    {
                        string kickedName = p.PlayerName;
                        Server.NextFrame(() => Server.ExecuteCommand($"kickid {p.UserId} Unready_Timeout"));
                        Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Lime}{kickedName} {ChatColors.White}因 未 {ChatColors.Lime}!R{ChatColors.White}準 備 而 被 踢 出");
                        _playerUnreadyTime.Remove(steamId);
                    }
                    else
                    {
                        int timeLeft = Config.KickUnreadyPlayerTime - _playerUnreadyTime[steamId];
                        p.PrintToChat($" {_cachedPrefix} 請輸入 {ChatColors.Lime}!R{ChatColors.White} 準備 ，{ChatColors.Lime}{timeLeft}{ChatColors.White} 秒未準備將被踢出");
                        p.PrintToCenter($"請輸入 !R 準備，{timeLeft} 秒後將被踢出"); 
                    }
                }
            }
        }
    }

    private void BroadcastWaitingMessage()
    {
        if (_isMatchLive) return;
        
        int totalPlayers = 0;
        foreach (var p in _serverPlayersCache)
        {
            if (p is { IsValid: true, Handle: not 0, IsBot: false, IsHLTV: false, TeamNum: 2 or 3 }) totalPlayers++;
        }

        if (totalPlayers > 0 && totalPlayers == _readyPlayers.Count)
        {
            string modeHint = totalPlayers == 1 
                ? $" [ {ChatColors.Green}對 戰 系 統{ChatColors.White} ] {ChatColors.White}場 上 {ChatColors.Green}1 {ChatColors.White}人，等 待 對 手 加 入..."
                : $" [ {ChatColors.Green}對 戰 系 統{ChatColors.White} ] {ChatColors.White}場 上 {ChatColors.Green}{totalPlayers} {ChatColors.White}人，等 對 手 加 入 {ChatColors.Green}{GetDynamicRequiredPlayers() / 2} v {GetDynamicRequiredPlayers() / 2} {ChatColors.White}團 戰";
            Server.PrintToChatAll(modeHint);
        }
    }

    private void BroadcastUnreadyPlayers()
    {
        if (_isMatchLive) return; 
        
        _unreadyNamesCache.Clear();
        int totalPlayers = 0; 
        
        foreach (var p in _serverPlayersCache)
        {
            if (p is { IsValid: true, Handle: not 0, IsBot: false, IsHLTV: false, TeamNum: 2 or 3 })
            {
                totalPlayers++; 
                if (!_readyPlayers.Contains(p.SteamID)) _unreadyNamesCache.Add(p.PlayerName); 
            }
        }
        
        if (totalPlayers > 0 && totalPlayers == _readyPlayers.Count) return;

        if (_unreadyNamesCache.Count > 0 || totalPlayers >= 2) 
        {
            int targetPlayers = GetDynamicRequiredPlayers();
            int teamSize = targetPlayers / 2;
            
            string modeHint = totalPlayers switch {
                2 => $" [ {ChatColors.Green}對 戰 系 統{ChatColors.White} ] {ChatColors.White}目 前 場 上 {ChatColors.Green}2 {ChatColors.White}人，雙 方 輸 入 {ChatColors.Orange}!R {ChatColors.White}即 可 直 接 {ChatColors.Green}1 v 1 單 挑{ChatColors.White}",
                > 2 => $" [ {ChatColors.Green}對 戰 系 統{ChatColors.White} ] {ChatColors.White}已觸發團戰，需滿 {ChatColors.Green}{targetPlayers} {ChatColors.White}人輸入 {ChatColors.Orange}!R {ChatColors.White}可開始 {ChatColors.Green}{teamSize} v {teamSize} 團戰{ChatColors.White}",
                _ => ""
            };
            
            if (_unreadyNamesCache.Count > 0)
                Server.PrintToChatAll($" {_cachedPrefix} 尚未準備玩家：{ChatColors.Orange}{string.Join(", ", _unreadyNamesCache)}{ChatColors.Default} | 對戰需滿 {ChatColors.Green}{targetPlayers}{ChatColors.Default} 人");
            if (!string.IsNullOrEmpty(modeHint)) Server.PrintToChatAll(modeHint); 
        }
    }

    private void ResetMatchState()
    {
        _isMatchLive = false;
        _isChangingMap = false;
        _currentPhaseIndex = 0; 
        _phaseStartScoreT = 0;
        _phaseStartScoreCT = 0;
        _liveMatchTargetPlayers = 0; 
        _readyPlayers.Clear();
        _lockedTeam.Clear(); // 重置名冊
        _playerUnreadyTime.Clear();
        _playerJoinTime.Clear(); 
        
        _playerPrimary.Clear();
        _playerSecondary.Clear();

        _pendingInitialReminders.Clear();
        _hasReceivedInitialReminder.Clear();
        _activeCenterMessage = "";
        _centerMessageExpiration = 0f;

        _liveTimer?.Kill(); _liveTimer = null;
        _privateCheckTimer?.Kill();
        _privateCheckTimer = AddTimer(Config.UnreadyReminderInterval, CheckAndWarnUnreadyPlayers, TimerFlags.REPEAT);
        
        _publicBroadcastTimer?.Kill();
        _publicBroadcastTimer = AddTimer(Config.PublicUnreadyReminderInterval, BroadcastUnreadyPlayers, TimerFlags.REPEAT);

        _waitingTimer?.Kill();
        _waitingTimer = AddTimer(Config.WaitingForOpponentInterval, BroadcastWaitingMessage, TimerFlags.REPEAT);
    }

    private HookResult OnEventRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        if (!_isMatchLive || Config.MatchModes.Count == 0 || _currentPhaseIndex >= Config.MatchModes.Count) return HookResult.Continue;
        
        EnsureTeamEntitiesCached();

        int scoreT = _cachedTeamT?.Score ?? 0;
        int scoreCT = _cachedTeamCT?.Score ?? 0;
        
        int displayScoreT = scoreT - _phaseStartScoreT;
        int displayScoreCT = scoreCT - _phaseStartScoreCT;
        
        if (displayScoreT < 0) displayScoreT = 0;
        if (displayScoreCT < 0) displayScoreCT = 0;

        if (displayScoreT == 0 && displayScoreCT == 0)
        {
            return HookResult.Continue;
        }

        var currentPhase = Config.MatchModes[_currentPhaseIndex];

        string fullHudHtml = string.Format(Config.HudHtml_RoundStart_Title, currentPhase.Name, currentPhase.DisplayTarget) + 
                             string.Format(Config.HudHtml_RoundStart_TScore, displayScoreT) + 
                             string.Format(Config.HudHtml_RoundStart_CTScore, displayScoreCT);

        ShowHud(fullHudHtml, Config.RoundStartHudDuration);

        return HookResult.Continue;
    }

    // =========================================================================
    // 載入廣告黑名單 (Ad Blacklist Loader)
    // =========================================================================
    private void LoadAdBlacklist()
    {
        // 完美鎖定 CS# 設定檔資料夾 (與 LiteMatchManager.json 同位子)
        string directoryPath = Path.GetFullPath(Path.Combine(ModuleDirectory, "..", "..", "configs", "plugins", "LiteMatchManager"));
        string filePath = Path.Combine(directoryPath, "ad_blacklist.txt");

        if (File.Exists(filePath))
        {
            try
            {
                var lines = File.ReadAllLines(filePath);
                List<string> validLines = [];
                foreach (var line in lines)
                {
                    // 【.NET 10 升級】：Span 零分配切片與驗證
                    var trimmed = line.AsSpan().Trim();
                    if (!trimmed.IsEmpty && !trimmed.StartsWith("//"))
                    {
                        validLines.Add(trimmed.ToString());
                    }
                }
                // 【.NET 10 升級】：集合表達式轉換
                adBlacklist = [.. validLines];
                Console.WriteLine($"[LoadAdBlacklist] 成功載入 {adBlacklist.Length} 筆廣告黑名單。");
            }
            catch (Exception e)
            {
                Console.WriteLine($"[LoadAdBlacklist FATAL] 讀取黑名單時發生錯誤: {e.Message}");
            }
        }
        else
        {
            Console.WriteLine("[LoadAdBlacklist] 黑名單檔案不存在，建立預設檔案。");
            try
            {
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
                // 預設寫入常見廣告，加上教學註解
                File.WriteAllLines(filePath, [
                    "// 在下方加入要封鎖的廣告網址或關鍵字 (一行一個)", 
                    "// 系統會自動忽略 // 開頭的註解與空白行",
                    "cs2commends", 
                    "cs2commends.com"
                ]);
                adBlacklist = ["cs2commends", "cs2commends.com"];
            }
            catch (Exception e)
            {
                Console.WriteLine($"[LoadAdBlacklist FATAL] 建立黑名單檔案時發生錯誤: {e.Message}");
            }
        }
    }

    public override void Unload(bool hotReload)
    {
        _isServerShuttingDown = true;
        base.Unload(hotReload);
    }
}
