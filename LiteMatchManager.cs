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
using System.Linq;

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
    
    // ★ 新增：最終勝利條件，達到此分數將無視中途離線並強制進行換圖流程
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
    public override string ModuleVersion => "9.28_FullyConfigurable_Final";
    public override string ModuleAuthor => "Optimized";
    public override string ModuleDescription => "原生30勝換圖 + 0記憶體垃圾極致版 + 訊息全設定檔化";

    public LiteMatchConfig Config { get; set; } = new();

    private string _cachedPrefix = "";
    private List<string> _cachedGunMenuMessage = new(); 
    
    private int _currentPhaseIndex = 0; 
    private int _phaseStartScoreT = 0;
    private int _phaseStartScoreCT = 0;
    
    private HashSet<string> _readyCommandsSet = new(StringComparer.OrdinalIgnoreCase);
    private HashSet<string> _gunMenuCommandsSet = new(StringComparer.OrdinalIgnoreCase);
    
    private static readonly HashSet<string> PistolWeapons = new(StringComparer.OrdinalIgnoreCase)
    {
        "weapon_usp_silencer", "weapon_glock", "weapon_deagle", "weapon_revolver",
        "weapon_p250", "weapon_tec9", "weapon_fiveseven", "weapon_cz75a",
        "weapon_elite", "weapon_hkp2000"
    };
    
    private HashSet<ulong> _readyPlayers = new(64);
    private Dictionary<ulong, int> _playerUnreadyTime = new(64); 
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
        foreach (var kvp in config.WeaponCommands) caseInsensitiveDict[kvp.Key] = kvp.Value;
        config.WeaponCommands = caseInsensitiveDict;

        _readyCommandsSet = new HashSet<string>(config.ReadyCommands, StringComparer.OrdinalIgnoreCase);
        _gunMenuCommandsSet = new HashSet<string>(config.GunMenuCommands, StringComparer.OrdinalIgnoreCase);

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

    // ★ 新增：檢查比賽是否已經達到最終勝利條件 (防止勝利面板空窗期逃跑)
    private bool IsMatchOver()
    {
        if (_isChangingMap) return true;
        
        EnsureTeamEntitiesCached();
        int scoreT = _cachedTeamT?.Score ?? 0;
        int scoreCT = _cachedTeamCT?.Score ?? 0;
        
        // 只要任一隊分數達到 30 勝 (或設定檔中的目標)，即代表比賽已結束，進入無敵狀態
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
                        toRemove ??= [];
                        toRemove.Add(steamId);

                        if (!_isMatchLive && !_readyPlayers.Contains(steamId))
                        {
                            _playerUnreadyTime.TryGetValue(steamId, out int elapsed);
                            int timeLeft = Config.KickUnreadyPlayerTime - elapsed;
                            p.PrintToChat($" {_cachedPrefix} 請輸入 {ChatColors.Lime}!R{ChatColors.White} 準備 ，{ChatColors.Lime}{timeLeft}{ChatColors.White} 秒未準備將被踢出");
                            p.PrintToCenter($"請輸入 !r 準備，{timeLeft} 秒後將被踢出"); // ★ 新增推薦 1 提示
                        }
                    }
                }
            }
        }

        foreach (var kvp in _pendingInitialReminders)
        {
            if (currentTime >= kvp.Value)
            {
                toRemove ??= [];
                if (!toRemove.Contains(kvp.Key)) toRemove.Add(kvp.Key);
            }
        }

        if (toRemove is not null) 
        {
            foreach (var id in toRemove) _pendingInitialReminders.Remove(id);
        }
    }

    public override void Load(bool hotReload)
    {
        Console.WriteLine("=================================================");
        Console.WriteLine("  LiteMatchManager (全自訂義訊息 + 極致優化版) 啟動！");
        Console.WriteLine("=================================================");

        _isServerShuttingDown = false;

        AddCommandListener("say", OnPlayerSay);
        AddCommandListener("say_team", OnPlayerSay);
        AddCommandListener("jointeam", OnJoinTeam);
        AddCommandListener("drop", (player, info) => HookResult.Handled);
        
        RegisterListener<Listeners.OnTick>(OnTick);
        AddTimer(1.0f, CheckPendingReminders, TimerFlags.REPEAT);

        RegisterEventHandler<EventMapShutdown>((@event, info) => { _isServerShuttingDown = true; return HookResult.Continue; });

        RegisterEventHandler<EventPlayerDisconnect>((@event, info) =>
        {
            if (@event.Userid is { SteamID: > 0 } player)
            {
                ulong steamId = player.SteamID;
                string pName = player.PlayerName;

                if (_isMatchLive && _readyPlayers.Contains(steamId))
                {
                    _readyPlayers.Remove(steamId);

                    // ★ 修正：若是比賽已經達到 30 勝（準備彈勝利面板或換圖中），直接無視玩家中離！
                    if (IsMatchOver()) return HookResult.Continue;

                    if (_liveMatchTargetPlayers == 2)
                    {
                        Server.PrintToChatAll($" {_cachedPrefix} 玩 家 {ChatColors.Gold}{pName} {ChatColors.White}斷 線，比 賽 強 制 終 止");
                        AbortMatch();
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
                    _playerUnreadyTime.Remove(steamId);
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
                    _pendingInitialReminders.Remove(steamId);
                    _hasReceivedInitialReminder.Remove(steamId);

                    if (_isMatchLive && _readyPlayers.Contains(steamId))
                    {
                        _readyPlayers.Remove(steamId);
                        
                        // ★ 分數已滿，無視換隊干擾
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
                        Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Gold}{player.PlayerName}{ChatColors.White} 跳 去 觀 戰，已 取 取 準 備");
                    }
                    _playerUnreadyTime.Remove(steamId); 
                }

                if (!_isMatchLive) Server.NextFrame(CheckMatchStart);
                else if (newTeam is 2 or 3)
                {
                    if (!_readyPlayers.Contains(steamId))
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
                                Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Gold}玩 家 {player.PlayerName} {ChatColors.White}成 功 補 位 加 入 團 戰 比 賽");
                            }
                        }
                    }
                    Server.NextFrame(CheckPhaseWin);
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
        // ★ 修正：檢查是否已達 30 勝，若達標則封鎖階段結算防干擾
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
        // ★ 修正：若分數已達 30，直接強制封鎖退回暖身的操作
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
        if (player is not { IsValid: true }) return HookResult.Continue;
        if (!int.TryParse(info.GetArg(1), out int teamIndex)) return HookResult.Continue;

        if (teamIndex is 2 or 3)
        {
            if (_isMatchLive)
            {
                if (_readyPlayers.Contains(player.SteamID) && player.TeamNum >= 2)
                {
                    player.PrintToChat($" {_cachedPrefix} {ChatColors.Orange}對 戰 進 行 中，無 法 切 換 隊 伍");
                    return HookResult.Handled;
                }

                if (!_readyPlayers.Contains(player.SteamID))
                {
                    if (_liveMatchTargetPlayers == 2)
                    {
                        player.PrintToChat($" {_cachedPrefix} {ChatColors.Orange}單 挑 比 賽 進 行 中，嚴 禁 中 途 加 入");
                        return HookResult.Handled;
                    }
                    else
                    {
                        int liveTeamMax = _liveMatchTargetPlayers / 2;
                        int currentTeamCount = 0;
                        foreach (var p in Utilities.GetPlayers())
                        {
                            if (p is { IsValid: true, Handle: not 0, IsBot: false, TeamNum: var team } && team == teamIndex && p.SteamID != player.SteamID)
                                currentTeamCount++;
                        }
                        
                        if (currentTeamCount >= liveTeamMax)
                        {
                            player.PrintToChat($" {_cachedPrefix} {ChatColors.Gold}加 入 失 敗！該 隊 伍 已 經 滿 員");
                            return HookResult.Handled;
                        }
                    }
                }
            }
            else 
            {
                int currentTeamCount = 0;
                foreach (var p in Utilities.GetPlayers())
                {
                    if (p is { IsValid: true, Handle: not 0, IsBot: false, TeamNum: var team } && team == teamIndex && p.SteamID != player.SteamID)
                        currentTeamCount++;
                }
                if (currentTeamCount >= Config.MaxPlayersPerTeam)
                {
                    string teamName = teamIndex == 2 ? "恐怖份子 (T)" : "反恐小組 (CT)";
                    player.PrintToChat($" {_cachedPrefix} {ChatColors.Orange}加 入 失 敗！{teamName} 已 經 滿 員 ( 最 多 {ChatColors.Green}{Config.MaxPlayersPerTeam}{ChatColors.Orange} 人 )");
                    return HookResult.Handled; 
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
            string message = rawArg.Trim('"', ' '); 
            if (string.IsNullOrWhiteSpace(message)) return HookResult.Continue;
            string playerName = player.PlayerName;
            string nameColor = player.TeamNum switch { 1 => $"{ChatColors.Grey}", 2 => "\x10", 3 => "\x0B", _ => $"{ChatColors.White}" };
            string formattedMessage = $" {ChatColors.White}[所有人]{ChatColors.White} {nameColor}{playerName}{ChatColors.White}：{message}";

            foreach (var p in Utilities.GetPlayers()) if (p is { IsValid: true, IsBot: false }) p.PrintToChat(formattedMessage);
            return HookResult.Handled; 
        }

        string command = rawArg.Trim('"', ' ', '!', '/').ToLower();

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
        string selectedMapString = Config.MapList[random.Next(Config.MapList.Count)];
        string[] parts = selectedMapString.Split(':');
        string mapName = parts[0];
        string workshopId = parts.Length > 1 ? parts[1] : "";

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
            player.PrintToCenter("您已經是 準 備 完 成 的狀態"); // ★ 新增推薦 2 提示
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
                _pendingInitialReminders[steamId] = Server.CurrentTime + 5.0f;
        }

        if (_isMatchLive && player.TeamNum is 2 or 3)
        {
            if (!_readyPlayers.Contains(steamId))
            {
                Server.NextFrame(() => {
                    if (player.IsValid) {
                        player.ChangeTeam(CsTeam.Spectator);
                        player.PrintToChat($" {_cachedPrefix} {ChatColors.Gold}比 賽 已 開 始，非 參 賽 者 無 法 加 加 入");
                    }
                });
                return HookResult.Continue; 
            }
        }
        
        Server.NextFrame(() => {
            Server.NextFrame(() => {
                if (player is not { IsValid: true, PawnIsAlive: true } || player.PlayerPawn.Value is not { IsValid: true } pawn) return;
                
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
                        p.PrintToCenter($"請輸入 !R 準備，{timeLeft} 秒後將被踢出"); // ★ 新增推薦 1 提示
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
        _playerUnreadyTime.Clear();
        
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

    public override void Unload(bool hotReload)
    {
        _isServerShuttingDown = true;
        base.Unload(hotReload);
    }
}
