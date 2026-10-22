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

namespace LiteMatchManager;

#pragma warning disable CS8618

// =========================================
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

    // 【0-GC 快取】：預先凍結各階段可用武器集合，消除每次重生與換槍時的 List 線性查找
    [JsonIgnore] public FrozenSet<string> PrimaryWeaponsSet { get; set; } = FrozenSet<string>.Empty;
    [JsonIgnore] public FrozenSet<string> SecondaryWeaponsSet { get; set; } = FrozenSet<string>.Empty;
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

    // 【新增】：遠端 iFastNet PHP 排位 API 設定 (實際邏輯拆分於 EloRankSystem.cs)
    [JsonPropertyName("StatsApiUrl")] public string StatsApiUrl { get; set; } = "";
    [JsonPropertyName("StatsApiKey")] public string StatsApiKey { get; set; } = "TW_CS_Secret_2026";
}

// 加上 partial 關鍵字，與 EloRankSystem.cs 自動合體
public partial class LiteMatchManager : BasePlugin, IPluginConfig<LiteMatchConfig>
{
    public override string ModuleName => "LiteMatchManager";
    public override string ModuleVersion => "9.39_Elo_VoiceSplit_NoRagdoll";
    public override string ModuleAuthor => "Optimized";
    public override string ModuleDescription => "原生30勝換圖 (整合段位+2v2語音分流+屍體清除)";

    public LiteMatchConfig Config { get; set; } = new();

    private string _cachedPrefix = "";
    // 【.NET 10 升級】：集合表達式
    private List<string> _cachedGunMenuMessage = []; 
    private bool[] _spawnWeaponIsReplaceable = [];
    
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
    private List<ulong> _reminderRemoveCache = new(16);
    private HashSet<ulong> _hasReceivedInitialReminder = new(64);

    private List<CCSPlayerController> _serverPlayersCache = new(64);

    private bool _isMatchLive = false;
    private bool _isCountdownActive = false;
    private bool _isChangingMap = false; 
    private int _liveMatchTargetPlayers = 0; 
    private bool _isServerShuttingDown = false; 
    
    private CounterStrikeSharp.API.Modules.Timers.Timer? _privateCheckTimer;
    private CounterStrikeSharp.API.Modules.Timers.Timer? _publicBroadcastTimer;
    private CounterStrikeSharp.API.Modules.Timers.Timer? _waitingTimer;
    private CounterStrikeSharp.API.Modules.Timers.Timer? _liveTimer; 

    private CCSTeam? _cachedTeamT = null;
    private CCSTeam? _cachedTeamCT = null;
    private ConVar? _cvWarmupPauseTimer = null;

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

        // 【.NET 10 升級】：將設定檔傳入的指令直接轉換為凍結集合並預熱 JIT
        _readyCommandsSet = config.ReadyCommands.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
        _gunMenuCommandsSet = config.GunMenuCommands.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
        _ = _readyCommandsSet.Contains("");
        _ = _gunMenuCommandsSet.Contains("");
        _ = PistolWeapons.Contains("");
        _ = config.WeaponCommands.TryGetValue("", out _);

        if (config.MatchModes != null)
        {
            foreach (var mode in config.MatchModes)
            {
                mode.PrimaryWeaponsSet = (mode.PrimaryWeapons ?? []).ToFrozenSet(StringComparer.OrdinalIgnoreCase);
                mode.SecondaryWeaponsSet = (mode.SecondaryWeapons ?? []).ToFrozenSet(StringComparer.OrdinalIgnoreCase);
                // 預熱正賽階段武器查表，防止跳轉 Live 時首次觸發 JIT 編譯
                _ = mode.PrimaryWeaponsSet.Contains("");
                _ = mode.SecondaryWeaponsSet.Contains("");
            }
        }

        if (config.SpawnWeapons != null)
        {
            _spawnWeaponIsReplaceable = new bool[config.SpawnWeapons.Count];
            for (int i = 0; i < config.SpawnWeapons.Count; i++)
            {
                string item = config.SpawnWeapons[i];
                _spawnWeaponIsReplaceable[i] = item.StartsWith("weapon_") && !item.Contains("knife") && !item.Contains("bayonet");
            }
        }
        else
        {
            _spawnWeaponIsReplaceable = [];
        }

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

    // 【核心修正】：徹底移除工作坊單挑地圖內建的 game_player_equip 發槍實體，
    // 阻斷地圖在第 0 幀偷塞 AK47 或洗掉玩家副武器導致的第 1 次錯誤拉槍動畫
    private static void PurgeMapWeaponEntities()
    {
        foreach (var equip in Utilities.FindAllEntitiesByDesignerName<CEntityInstance>("game_player_equip"))
        {
            if (equip is { IsValid: true })
            {
                equip.Remove();
            }
        }
    }

    // 【新增功能 23】：徹底清除地面屍體模型 (cs_ragdoll)，防止窄道掩體被屍體擋住視線並減輕客戶端物理運算
    private static void PurgeRagdolls()
    {
        foreach (var ragdoll in Utilities.FindAllEntitiesByDesignerName<CEntityInstance>("cs_ragdoll"))
        {
            if (ragdoll is { IsValid: true })
            {
                ragdoll.Remove();
            }
        }
    }

    // 【新增功能 18】：1v1 與 2v2 語音自動分流控制
    private static void ApplyVoiceMode(bool enableAllTalk)
    {
        if (enableAllTalk)
        {
            // 1v1 單挑、暖身或賽後 5 秒：開啟全頻語音讓雙方直接交流
            Server.ExecuteCommand("sv_alltalk 1; sv_full_alltalk 1; sv_deadtalk 1");
        }
        else
        {
            // 2v2 團戰正賽期間：關閉全頻語音，僅限同隊語音報點 (死後仍可對同隊活人報點)
            Server.ExecuteCommand("sv_alltalk 0; sv_full_alltalk 0; sv_deadtalk 1");
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

    private void ShowHud(string html, float duration)
    {
        _activeCenterMessage = html;
        _centerMessageExpiration = Server.CurrentTime + duration;
    }

    private void ClearHudImmediate()
    {
        if (!string.IsNullOrEmpty(_activeCenterMessage))
        {
            _activeCenterMessage = "";
            _centerMessageExpiration = 0f;
            foreach (var p in _serverPlayersCache)
            {
                if (p is { IsValid: true, TeamNum: 2 or 3 }) p.PrintToCenterHtml("&#8203;", 0);
            }
        }
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
        _reminderRemoveCache.Clear();

        foreach (var p in _serverPlayersCache)
        {
            if (p is { IsValid: true, SteamID: > 0, TeamNum: 2 or 3 })
            {
                ulong steamId = p.SteamID;
                if (_pendingInitialReminders.TryGetValue(steamId, out float triggerTime))
                {
                    if (currentTime >= triggerTime)
                    {
                        _reminderRemoveCache.Add(steamId);

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
                if (!_reminderRemoveCache.Contains(key)) _reminderRemoveCache.Add(key);
            }
        }

        if (_reminderRemoveCache.Count > 0) 
        {
            foreach (var id in _reminderRemoveCache) _pendingInitialReminders.Remove(id);
            _reminderRemoveCache.Clear();
        }
    }

    public override void Load(bool hotReload)
    {
        Console.WriteLine("=================================================");
        Console.WriteLine("  LiteMatchManager (純淨效能極致版) 啟動！");
        Console.WriteLine("=================================================");

        _isServerShuttingDown = false;
        _cvWarmupPauseTimer = ConVar.Find("mp_warmup_pausetimer");
        
        AddCommandListener("say", OnPlayerSay);
        AddCommandListener("say_team", OnPlayerSay);
        AddCommandListener("jointeam", OnJoinTeam);
        AddCommandListener("drop", (player, info) => HookResult.Handled);
        
        RegisterListener<Listeners.OnTick>(OnTick);
        AddTimer(1.0f, CheckPendingReminders, TimerFlags.REPEAT);

        RegisterEventHandler<EventMapShutdown>((@event, info) => { _isServerShuttingDown = true; return HookResult.Continue; });

        RegisterEventHandler<EventPlayerConnectFull>((@event, info) =>
        {
            Server.NextFrame(RefreshActivePlayers);
            return HookResult.Continue;
        });

        RegisterEventHandler<EventPlayerDisconnect>((@event, info) =>
        {
            Server.NextFrame(RefreshActivePlayers);

            if (@event.Userid is { SteamID: > 0 } player)
            {
                ulong steamId = player.SteamID;
                string pName = player.PlayerName;

                if (_isMatchLive && _readyPlayers.Contains(steamId))
                {
                    _readyPlayers.Remove(steamId);
                    _lockedTeam.Remove(steamId); // 釋放名冊鎖

                    if (IsMatchOver()) return HookResult.Continue;

                    if (_liveMatchTargetPlayers == 2 || _isCountdownActive)
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
            return HookResult.Continue;
        });

        RegisterEventHandler<EventPlayerTeam>((@event, info) =>
        {
            Server.NextFrame(RefreshActivePlayers);

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
                        
                        if (_liveMatchTargetPlayers == 2 || _isCountdownActive)
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
                            // 【核心防護】對照陣營名冊，阻斷 ChangeTeam 造成的無限迴圈
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
                                foreach (var p in _serverPlayersCache)
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
                                    Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Gold}玩 家 {player.PlayerName} {ChatColors.White}成 功 補 位 加 入 團 戰 比 賽");
                                }
                            }
                        }
                        Server.NextFrame(CheckPhaseWin);
                    }
                    else // !_isMatchLive
                    {
                        // 3. 非比賽期間：檢查是否超過滿員設定
                        int currentTeamCount = 0;
                        foreach (var p in _serverPlayersCache)
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
            return HookResult.Continue;
        }, HookMode.Post);

        // 【新增】：在每回合預備階段 (玩家重生前) 優先掃除地圖重生出來的 game_player_equip 發槍器
        RegisterEventHandler<EventRoundPrestart>((@event, info) =>
        {
            PurgeMapWeaponEntities();
            return HookResult.Continue;
        }, HookMode.Pre);

        // 【新增功能 23 + 32】：0.5 秒清除屍體 + 呼叫 EloRankSystem.cs 記錄擊殺與爆頭
        RegisterEventHandler<EventPlayerDeath>((@event, info) =>
        {
            if (!_isServerShuttingDown)
            {
                AddTimer(0.5f, PurgeRagdolls);
            }

            RecordEloKill(@event.Attacker, @event.Userid, @event.Headshot);
            return HookResult.Continue;
        });

        RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
        RegisterEventHandler<EventRoundStart>(OnEventRoundStart);
        RegisterEventHandler<EventCsWinPanelMatch>(OnMatchEnd);

        RegisterEventHandler<EventRoundEnd>((@event, info) => {
            if (_isMatchLive && !_isCountdownActive) Server.NextFrame(CheckPhaseWin);
            return HookResult.Continue;
        });

        RegisterListener<Listeners.OnMapStart>(mapName => 
        {
            _isServerShuttingDown = false;
            _cachedTeamT = null;
            _cachedTeamCT = null;
            _cvWarmupPauseTimer = ConVar.Find("mp_warmup_pausetimer");

            ResetMatchState();
            Server.NextFrame(() =>
            {
                PurgeMapWeaponEntities();
                Server.ExecuteCommand($"exec {Config.WarmupConfigName}");
            });
            
            Server.NextFrame(RefreshActivePlayers);
        });

        Server.NextFrame(() =>
        {
            PurgeMapWeaponEntities();
            RefreshActivePlayers();
        });
    }

    private int GetDynamicRequiredPlayers()
    {
        int activeT = 0, activeCT = 0;
        foreach (var p in _serverPlayersCache)
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
        if (_isServerShuttingDown || !_isMatchLive || _isCountdownActive || IsMatchOver()) return; 
        
        int activeT = 0, activeCT = 0;
        foreach (var p in _serverPlayersCache)
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

        // 【新增功能 18】：比賽結束最後 5 秒換圖期間，自動切回 sv_alltalk 1 讓全場玩家賽後交流
        Server.NextFrame(() => ApplyVoiceMode(enableAllTalk: true));

        int scoreT = 0, scoreCT = 0;

        EnsureTeamEntitiesCached();

        if (_cachedTeamT != null) scoreT = _cachedTeamT.Score;
        if (_cachedTeamCT != null) scoreCT = _cachedTeamCT.Score;

        string winnerName = scoreT > scoreCT ? "恐怖份子" : "反恐小組";
        string loserName = scoreT > scoreCT ? "反恐小組" : "恐怖份子";

        CCSPlayerController? playerT = null;
        CCSPlayerController? playerCT = null;
        
        if (_liveMatchTargetPlayers == 2)
        {
            string nameT = "恐怖份子";
            string nameCT = "反恐小組";
            foreach (var p in _serverPlayersCache)
            {
                if (p is { IsValid: true, Handle: not 0, IsBot: false, TeamNum: 2 }) { nameT = p.PlayerName; playerT = p; }
                else if (p is { IsValid: true, Handle: not 0, IsBot: false, TeamNum: 3 }) { nameCT = p.PlayerName; playerCT = p; }
            }
            winnerName = scoreT > scoreCT ? nameT : nameCT;
            loserName = scoreT > scoreCT ? nameCT : nameT;
        }

        int winnerScore = Math.Max(scoreT, scoreCT);
        int loserScore = Math.Min(scoreT, scoreCT);

        Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Lime}{winnerName} {ChatColors.Gold}以 {ChatColors.Green} {winnerScore} : {loserScore} {ChatColors.Gold}贏得了最終勝利");

        // 【呼叫 EloRankSystem.cs】：檢查並上傳 30 勝完賽 Elo 與段位
        TrySubmitEloMatchResult(scoreT, scoreCT, winnerScore, loserScore, winnerName, loserName, playerT, playerCT);

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
            
            _cvWarmupPauseTimer ??= ConVar.Find("mp_warmup_pausetimer");
            if (_cvWarmupPauseTimer is not null) _cvWarmupPauseTimer.SetValue(1);
            else Server.ExecuteCommand("mp_warmup_pausetimer 1");
            
            Server.NextFrame(() =>
            {
                Server.ExecuteCommand($"exec {Config.WarmupConfigName}");
                ApplyVoiceMode(enableAllTalk: true);
            });
        });
    }

    // 【核心防抖動】：在玩家按下選隊指令的瞬間直接攔截，防止引擎先生成角色肉體再於下一幀處死踢回觀戰
    private HookResult OnJoinTeam(CCSPlayerController? player, CommandInfo info)
    {
        if (player is not { IsValid: true, Handle: not 0 } || player.IsBot) return HookResult.Continue;

        string arg = info.GetArg(1);
        if (!int.TryParse(arg, out int targetTeam)) return HookResult.Continue;

        // 若選擇觀戰 (1)，直接放行交由 EventPlayerTeam 處理取消準備或放棄比賽
        if (targetTeam == 1) return HookResult.Continue;

        ulong steamId = player.SteamID;

        if (targetTeam is 0 or 2 or 3)
        {
            if (_isMatchLive)
            {
                // 1. 比賽中：場上已準備玩家企圖換隊，直接阻擋，完全不觸發換隊與重生
                if (_readyPlayers.Contains(steamId))
                {
                    if (_lockedTeam.TryGetValue(steamId, out int lockedTeam) && targetTeam != lockedTeam)
                    {
                        player.PrintToChat($" {_cachedPrefix} {ChatColors.Orange}對 戰 進 行 中，無 法 切 換 隊 伍");
                        return HookResult.Stop;
                    }
                    return HookResult.Continue;
                }
                // 2. 比賽中：新進玩家或觀戰者試圖加入
                else
                {
                    if (_liveMatchTargetPlayers == 2 || _isCountdownActive)
                    {
                        if (player.TeamNum == 0) player.ChangeTeam(CsTeam.Spectator);
                        player.PrintToChat($" {_cachedPrefix} {ChatColors.Orange}單 挑 比 賽 進 行 中，不 開 放 加 入");
                        return HookResult.Stop;
                    }
                    else
                    {
                        int liveTeamMax = _liveMatchTargetPlayers / 2;

                        if (targetTeam == 0)
                        {
                            int countT = 0, countCT = 0;
                            foreach (var p in _serverPlayersCache)
                            {
                                if (p is { IsValid: true, IsBot: false } && p.SteamID != steamId)
                                {
                                    if (p.TeamNum == 2) countT++;
                                    else if (p.TeamNum == 3) countCT++;
                                }
                            }
                            if (countT >= liveTeamMax && countCT >= liveTeamMax)
                            {
                                if (player.TeamNum == 0) player.ChangeTeam(CsTeam.Spectator);
                                player.PrintToChat($" {_cachedPrefix} {ChatColors.Orange}該 隊 伍 補 位 已 滿，無 法 加 入");
                                return HookResult.Stop;
                            }
                            return HookResult.Continue;
                        }

                        int currentCount = 0;
                        foreach (var p in _serverPlayersCache)
                        {
                            if (p is { IsValid: true, IsBot: false, TeamNum: var team } && team == targetTeam && p.SteamID != steamId)
                                currentCount++;
                        }

                        if (currentCount >= liveTeamMax)
                        {
                            if (player.TeamNum == 0) player.ChangeTeam(CsTeam.Spectator);
                            player.PrintToChat($" {_cachedPrefix} {ChatColors.Orange}該 隊 伍 補 位 已 滿，無 法 加 入");
                            return HookResult.Stop;
                        }
                    }
                }
            }
            else // !_isMatchLive (非比賽期間：檢查是否超過滿員設定)
            {
                if (targetTeam is 2 or 3)
                {
                    int currentTeamCount = 0;
                    foreach (var p in _serverPlayersCache)
                    {
                        if (p is { IsValid: true, Handle: not 0, IsBot: false, TeamNum: var team } && team == targetTeam && p.SteamID != steamId)
                            currentTeamCount++;
                    }

                    if (currentTeamCount >= Config.MaxPlayersPerTeam)
                    {
                        if (player.TeamNum == 0) player.ChangeTeam(CsTeam.Spectator);
                        string teamName = targetTeam == 2 ? "恐怖份子 (T)" : "反恐小組 (CT)";
                        player.PrintToChat($" {_cachedPrefix} {ChatColors.Orange}加 入 失 敗！{teamName} 已 經 滿 員 ( 最 多 {ChatColors.Green}{Config.MaxPlayersPerTeam}{ChatColors.Orange} 人 )");
                        return HookResult.Stop;
                    }
                }
            }
        }

        return HookResult.Continue;
    }

    private HookResult OnPlayerSay(CCSPlayerController? player, CommandInfo info)
    {
        if (player is not { IsValid: true }) return HookResult.Continue;
        string rawArg = info.GetArg(1);
        if (string.IsNullOrWhiteSpace(rawArg)) return HookResult.Continue;

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
            string playerName = player.PlayerName ?? "";
            string nameColor = player.TeamNum switch { 1 => $"{ChatColors.Grey}", 2 => "\x10", 3 => "\x0B", _ => $"{ChatColors.White}" };
            string formattedMessage = $" {ChatColors.White}[所有人]{ChatColors.White} {nameColor}{playerName}{ChatColors.White}：{message}";

            // 將 Utilities.GetPlayers() 替換為 0 GC 高效能快取
            foreach (var p in _serverPlayersCache) if (p is { IsValid: true, IsBot: false }) p.PrintToChat(formattedMessage);
            return HookResult.Handled; 
        }

        // 【.NET 10 升級】：改用 Span 切片並移除多餘的 .ToLower()，配合 IgnoreCase 集合達成真正 0 額外配置
        string command = rawArg.AsSpan().Trim(" \"!/").ToString();

        if (string.Equals(command, "nextmap", StringComparison.OrdinalIgnoreCase) && AdminManager.PlayerHasPermissions(player, "@css/root"))
        {
            TriggerMapChange(); return HookResult.Handled; 
        }

        // 【呼叫 EloRankSystem.cs】：玩家輸入 !rank 或 !elo 查詢個人段位與積分
        if (string.Equals(command, "rank", StringComparison.OrdinalIgnoreCase) || string.Equals(command, "elo", StringComparison.OrdinalIgnoreCase))
        {
            TriggerPlayerRankQuery(player);
            return HookResult.Continue;
        }

        if (_readyCommandsSet.Contains(command))
        {
            if (!_isMatchLive) 
            {
                // 【治本完美解法】：將 !R 指令的處理推遲一幀，完美錯開重生裝備載入的 Tick 衝突
                Server.NextFrame(() => {
                    if (player is { IsValid: true })
                    {
                        HandlePlayerReady(player);
                    }
                });
            }
            return HookResult.Continue; 
        }
        else if (string.Equals(command, "unready", StringComparison.OrdinalIgnoreCase))
        {
            if (!_isMatchLive) HandlePlayerUnready(player);
            return HookResult.Continue; 
        }

        if (Config.EnableChatWeaponCommands)
        {
            if (_gunMenuCommandsSet.Contains(command))
            {
                OnGsCommand(player); 
                return HookResult.Continue; // 保留 Continue，讓全服看到指令
            }
            if (Config.WeaponCommands.TryGetValue(command, out string? realWeaponName))
            {
                TryGiveWeaponByCommand(player, realWeaponName);
                return HookResult.Continue; // 保留 Continue，讓全服看到指令
            }
        }
        
        return HookResult.Continue;
    }

    private void TryGiveWeaponByCommand(CCSPlayerController player, string weaponName)
    {
        if (!player.PawnIsAlive) return;

        if (!_isMatchLive || _isCountdownActive || Config.MatchModes.Count == 0)
        {
            ReplaceWeapon(player, weaponName);
            return;
        }

        var phase = Config.MatchModes[_currentPhaseIndex];
        
        bool isPrimary = phase.PrimaryWeaponsSet.Contains(weaponName);
        bool isSecondary = phase.SecondaryWeaponsSet.Contains(weaponName);

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
        
        // 【.NET 10 升級】：改用 Random.Shared + Span 切片，徹底消除 Split 帶來的字串陣列分配
        ReadOnlySpan<char> selectedMapSpan = Config.MapList[Random.Shared.Next(Config.MapList.Count)].AsSpan();
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
            foreach (var p in _serverPlayersCache) if (p is { IsValid: true, IsBot: false, IsHLTV: false, TeamNum: 2 or 3 }) activePlayers++;
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
        foreach (var p in _serverPlayersCache)
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
            _isCountdownActive = true;
            _currentPhaseIndex = 0; 
            _liveMatchTargetPlayers = totalPlayers; 
            
            _phaseStartScoreT = 0;
            _phaseStartScoreCT = 0;
            
            _playerPrimary.Clear();
            _playerSecondary.Clear();
            _lockedTeam.Clear(); // 清空舊名冊
            _pendingInitialReminders.Clear();
            ResetEloMatchStats();
            
            // 【產生名冊鎖】：開賽瞬間紀錄所有存活玩家的隊伍
            foreach (var p in _serverPlayersCache)
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

            // 1. 關閉所有等待與催促的計時器
            _privateCheckTimer?.Kill(); _privateCheckTimer = null;
            _publicBroadcastTimer?.Kill(); _publicBroadcastTimer = null;
            _waitingTimer?.Kill(); _waitingTimer = null;
            
            // ==========================================
            // 【.NET 10 效能版：0.5秒緩衝 + 雙重0 GC快取 (包含聊天室321)】
            // ==========================================
            string precompiledLine1 = string.Format(Config.HudHtml_Round1_Line1, modeText);
            string precompiledLine2 = string.Format(Config.HudHtml_Round1_Line2, displayLimit);
            const string popupSoundCmd = "play sounds/ui/panorama/popup_reveal_01.vsnd";
            
            int countdown = (int)Config.HudDuration_MatchStart; 
            if (countdown <= 0) countdown = 3;

            // HUD 與「聊天室廣播」雙重快取！
            string[] precompiledHuds = new string[countdown + 1];
            string[] precompiledChats = new string[countdown + 1]; // 專門給聊天室用的陣列
            
            for (int i = 1; i <= countdown; i++)
            {
                // HUD 畫面快取
                string countdownHtml = $"<b>➡ ➡ ➡<font class='fontSize-l' color='orange'> 倒 數  </font><font class='fontSize-l' color='#DF3935'>{i}</font><font class='fontSize-l' color='orange'>  秒 </font>⬅ ⬅ ⬅</b><br>";
                precompiledHuds[i] = $"{precompiledLine1}{countdownHtml}{precompiledLine2}";
                
                // 聊天室廣播快取 (把 3、2、1 的紅字也準備好)
                precompiledChats[i] = $" {_cachedPrefix} {ChatColors.Gold}{modeText}{ChatColors.White} 戰 鬥 開 始！倒 數 {ChatColors.LightRed}{i}{ChatColors.White} 秒 ...";
            }

            // 第 0 秒瞬間：只發送準備提示，給玩家 0.5 秒心理準備
            Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Gold}{modeText}{ChatColors.White} 雙 方 皆 已 準 備，即 將 開 戰 ...");

            // 2. 將倒數邏輯包裝成 Action
            Action tickLogic = () => 
            {
                if (countdown > 0)
                {
                    // A. HUD 畫面同步（最後 1 秒使用 0.95f，確保在下一秒執行 exec live.cfg 前自然收尾，絕不重疊）
                    ShowHud(precompiledHuds[countdown], countdown == 1 ? 0.95f : 1.1f); 
                    
                    // B. 聊天室 3、2、1 同步廣播 (直接讀取快取，0效能消耗)
                    Server.PrintToChatAll(precompiledChats[countdown]);
                    
                    // C. 音效同步
                    foreach (var p in _serverPlayersCache)
                    {
                        if (p is { IsValid: true, IsBot: false }) p.ExecuteClientCommand(popupSoundCmd);
                    }
                    countdown--; 
                }
                else
                {
                    _isCountdownActive = false;

                    // 確保倒數 HUD 已完全收尾，避免與下一幀的 exec live.cfg 撞在同一 Tick
                    ClearHudImmediate();

                    // 正式開賽廣播與後台
                    Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Gold}{modeText}{ChatColors.White} 比 賽 正 式 開 始");
                    if (activeT >= 2 && activeCT >= 2) Console.WriteLine("[ 2 v 2 團 戰 ] 比 賽 開 始");

                    // 刷新比賽、掃除地圖發槍實體，並根據 1v1 / 2v2 自動分流語音設定
                    bool is1v1 = _liveMatchTargetPlayers == 2;
                    Server.NextFrame(() => { 
                        PurgeMapWeaponEntities();
                        Server.ExecuteCommand($"exec {Config.LiveConfigName}");
                        // 【新增功能 18】：在 live.cfg 載入後下一幀設定語音 (1v1 全頻聊天 / 2v2 僅同隊報點)
                        Server.NextFrame(() => ApplyVoiceMode(enableAllTalk: is1v1));
                    });
                    
                    _liveTimer?.Kill();
                    _liveTimer = null;
                }
            };

            // 啟動 0.5 秒黃金緩衝接力計時器
            _liveTimer?.Kill();
            _liveTimer = AddTimer(0.5f, () => 
            {
                tickLogic(); // 0.5秒後觸發倒數 3

                if (countdown >= 0) 
                {
                    _liveTimer?.Kill();
                    _liveTimer = AddTimer(1.0f, tickLogic, TimerFlags.REPEAT);
                }
            });
        }
    }

    // 【核心修正】：解決 CS2 引擎將 USP 與 M4A1-S 的 DesignerName 回傳為 hkp2000 / m4a1 導致誤判重發的問題
    private static string GetActualWeaponName(CBasePlayerWeapon weapon)
    {
        string dName = weapon.DesignerName;
        if (string.IsNullOrEmpty(dName)) return "";

        ushort defIndex = weapon.AttributeManager.Item.ItemDefinitionIndex;
        return defIndex switch
        {
            60 => "weapon_m4a1_silencer", // M4A1-S
            61 => "weapon_usp_silencer",  // USP-S
            63 => "weapon_cz75a",         // CZ75-Auto
            64 => "weapon_revolver",      // R8 Revolver
            _ => dName
        };
    }

    // 【零干擾差量配槍核心】：
    // 1. 若上一局存活且武器完全符合 -> 0 動作 (0 拔槍動畫)
    // 2. 若上一局陣亡重生 (身上僅有小刀，無錯誤主副武器) -> 絕不呼叫 RemoveWeapons() 洗掉小刀，直接以「先主武、後副武」順序補齊缺槍
    // 3. 若跨階段或持有錯誤武器 -> 安全呼叫 RemoveWeapons() 後，同樣以「刀 -> 先主武 -> 後副武」順序一次到位
    private static void ApplySmartLoadout(
        CCSPlayerController player,
        bool hasKnife,
        string currentPri,
        string currentSec,
        int priCount,
        int secCount,
        string targetPri,
        string targetSec)
    {
        bool priMatches = string.Equals(currentPri, targetPri, StringComparison.OrdinalIgnoreCase);
        bool secMatches = string.Equals(currentSec, targetSec, StringComparison.OrdinalIgnoreCase);

        bool hasWrongPri = (!string.IsNullOrEmpty(currentPri) && !priMatches) || priCount > 1;
        bool hasWrongSec = (!string.IsNullOrEmpty(currentSec) && !secMatches) || secCount > 1;

        if (!hasWrongPri && !hasWrongSec)
        {
            // 身上沒有任何錯誤武器（完全相符的贏家，或剛重生只有一把小刀的輸家）：直接差量補齊！
            if (!hasKnife) player.GiveNamedItem("weapon_knife");
            if (!priMatches && !string.IsNullOrEmpty(targetPri)) player.GiveNamedItem(targetPri);
            if (!secMatches && !string.IsNullOrEmpty(targetSec)) player.GiveNamedItem(targetSec);
        }
        else
        {
            // 身上帶有舊階段或錯誤的武器：統一使用底層安全的 RemoveWeapons() 卸載，並先給主武器再給副武器
            player.RemoveWeapons();
            player.GiveNamedItem("weapon_knife");
            if (!string.IsNullOrEmpty(targetPri)) player.GiveNamedItem(targetPri);
            if (!string.IsNullOrEmpty(targetSec)) player.GiveNamedItem(targetSec);
        }
    }

    // 【智慧護甲補發】：僅在護甲不滿 100 或缺少頭盔時才呼叫 GiveNamedItem，避免每回合無條件生成多餘實體
    private static void ApplySmartArmor(CCSPlayerController player, CCSPlayerPawn pawn, int armorMode)
    {
        if (armorMode <= 0)
        {
            if (pawn.ArmorValue > 0) pawn.ArmorValue = 0;
        }
        else if (armorMode == 1)
        {
            if (pawn.ArmorValue < 100) player.GiveNamedItem("item_kevlar");
        }
        else
        {
            bool hasHelmet = pawn.ItemServices?.As<CCSPlayer_ItemServices>()?.HasHelmet ?? false;
            if (pawn.ArmorValue < 100 || !hasHelmet)
            {
                player.GiveNamedItem("item_assaultsuit");
            }
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
            // 【新增功能 23】：暖身期間無限復活時，順手清除場上殘留的舊屍體
            if (!_isMatchLive) PurgeRagdolls();

            if (player is not { IsValid: true, PawnIsAlive: true } || player.PlayerPawn.Value is not { IsValid: true } pawn) return;
            
            // 【嚴格保護】防止對已經被踢去觀戰的「幽靈玩家」進行武器剝奪
            if (player.TeamNum is not 2 and not 3) return;

            // 掃描玩家身上目前持有的武器真實名稱與數量 (0-GC)
            string currentPri = "";
            string currentSec = "";
            int priCount = 0;
            int secCount = 0;
            bool hasKnife = false;

            if (pawn.WeaponServices?.MyWeapons is { } weapons)
            {
                foreach (var weaponHandle in weapons)
                {
                    if (weaponHandle.Value is { IsValid: true } w)
                    {
                        string wName = GetActualWeaponName(w);
                        if (string.IsNullOrEmpty(wName)) continue;

                        if (wName.Contains("knife") || wName.Contains("bayonet"))
                        {
                            hasKnife = true;
                        }
                        else if (PistolWeapons.Contains(wName))
                        {
                            currentSec = wName;
                            secCount++;
                        }
                        else if (wName.StartsWith("weapon_") && !wName.Contains("c4"))
                        {
                            currentPri = wName;
                            priCount++;
                        }
                    }
                }
            }
            
            if (_isMatchLive && !_isCountdownActive && Config.MatchModes.Count > _currentPhaseIndex)
            {
                var phase = Config.MatchModes[_currentPhaseIndex];
                
                pawn.Health = phase.Health;

                // 決定當前階段應該持有的副武器與主武器（優先讀取玩家指令記憶，如 !dg）
                string secToGive = phase.SecondaryWeapons.Count > 0 ? phase.SecondaryWeapons[0] : "";
                if (_playerSecondary.TryGetValue(steamId, out string? prefSec) && phase.SecondaryWeaponsSet.Contains(prefSec))
                    secToGive = prefSec;

                string priToGive = phase.PrimaryWeapons.Count > 0 ? phase.PrimaryWeapons[0] : "";
                if (_playerPrimary.TryGetValue(steamId, out string? prefPri) && phase.PrimaryWeaponsSet.Contains(prefPri))
                    priToGive = prefPri;

                ApplySmartLoadout(player, hasKnife, currentPri, currentSec, priCount, secCount, priToGive, secToGive);
                ApplySmartArmor(player, pawn, phase.Armor);

                if (phase.Grenades != null) foreach(var nade in phase.Grenades) player.GiveNamedItem(nade);
            }
            else
            {
                // 暖身模式：計算目標武器並智慧差量保留
                bool hasPrefPri = _playerPrimary.TryGetValue(steamId, out string? prefPri);
                bool hasPrefSec = _playerSecondary.TryGetValue(steamId, out string? prefSec);

                string targetWarmupPri = "";
                string targetWarmupSec = "";
                int warmupArmorMode = 0;

                for (int i = 0; i < Config.SpawnWeapons.Count; i++)
                {
                    string item = Config.SpawnWeapons[i];
                    if (item.Equals("item_assaultsuit", StringComparison.OrdinalIgnoreCase))
                    {
                        warmupArmorMode = 2;
                        continue;
                    }
                    if (item.Equals("item_kevlar", StringComparison.OrdinalIgnoreCase))
                    {
                        if (warmupArmorMode < 2) warmupArmorMode = 1;
                        continue;
                    }
                    if (item.Contains("knife") || item.Contains("bayonet")) continue;

                    if (PistolWeapons.Contains(item))
                    {
                        targetWarmupSec = hasPrefSec ? prefSec! : item;
                    }
                    else if (item.StartsWith("weapon_"))
                    {
                        targetWarmupPri = hasPrefPri ? prefPri! : item;
                    }
                }

                ApplySmartLoadout(player, hasKnife, currentPri, currentSec, priCount, secCount, targetWarmupPri, targetWarmupSec);
                if (warmupArmorMode > 0) ApplySmartArmor(player, pawn, warmupArmorMode);
            }
        });
        
        return HookResult.Continue;
    }

    private void ReplaceWeapon(CCSPlayerController player, string newWeapon)
    {
        // 【核心防崩潰】：將指令換槍的檢查、安全卸載與發放整個推遲到 NextFrame 執行，
        // 徹底消除在 OnPlayerSay 網路封包中途直接砍實體造成的 CopyExistingEntity 客戶端崩潰！
        Server.NextFrame(() => {
            if (player is not { IsValid: true, PawnIsAlive: true, TeamNum: 2 or 3 } || player.PlayerPawn.Value is not { IsValid: true } pawn) return;
            if (pawn.WeaponServices?.MyWeapons is not { } weapons) return;

            string currentPri = "";
            string currentSec = "";
            bool hasKnife = false;

            foreach (var weaponHandle in weapons)
            {
                if (weaponHandle.Value is { IsValid: true } weapon)
                {
                    string wName = GetActualWeaponName(weapon);
                    if (string.IsNullOrEmpty(wName) || wName.Contains("c4")) continue;

                    if (wName.Contains("knife") || wName.Contains("bayonet")) hasKnife = true;
                    else if (PistolWeapons.Contains(wName)) currentSec = wName;
                    else if (wName.StartsWith("weapon_")) currentPri = wName;
                }
            }

            bool isNewPistol = PistolWeapons.Contains(newWeapon);
            string targetSec = isNewPistol ? newWeapon : currentSec;
            string targetPri = isNewPistol ? currentPri : newWeapon;

            // 若手上已經是同一把槍，直接不動作
            if (isNewPistol && string.Equals(currentSec, newWeapon, StringComparison.OrdinalIgnoreCase)) return;
            if (!isNewPistol && string.Equals(currentPri, newWeapon, StringComparison.OrdinalIgnoreCase)) return;

            // 統一使用官方底層安全的 RemoveWeapons() 卸載後重給，並以「先主武、後副武」順序發放
            int currentArmor = pawn.ArmorValue;
            bool hasHelmet = pawn.ItemServices?.As<CCSPlayer_ItemServices>()?.HasHelmet ?? false;

            player.RemoveWeapons();
            if (hasKnife) player.GiveNamedItem("weapon_knife");
            if (!string.IsNullOrEmpty(targetPri)) player.GiveNamedItem(targetPri);
            if (!string.IsNullOrEmpty(targetSec)) player.GiveNamedItem(targetSec);

            if (currentArmor > 0)
            {
                player.GiveNamedItem(hasHelmet ? "item_assaultsuit" : "item_kevlar");
                pawn.ArmorValue = currentArmor;
            }
        });
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
        _isCountdownActive = false;
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
        ResetEloMatchStats();

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
        // 確保每回合開始時地圖沒有殘留的 game_player_equip 發槍器
        PurgeMapWeaponEntities();

        if (!_isMatchLive || _isCountdownActive || Config.MatchModes.Count == 0 || _currentPhaseIndex >= Config.MatchModes.Count) return HookResult.Continue;
        
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

    public override void Unload(bool hotReload)
    {
        _isServerShuttingDown = true;
        base.Unload(hotReload);
    }
}
