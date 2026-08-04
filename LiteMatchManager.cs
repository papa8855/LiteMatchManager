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

namespace LiteMatchManager;

#pragma warning disable CS8618

public class LiteMatchConfig : BasePluginConfig
{
    [JsonPropertyName("MinPlayersToStart")] public int MinPlayersToStart { get; set; } = 4;
    [JsonPropertyName("MaxPlayersPerTeam")] public int MaxPlayersPerTeam { get; set; } = 3; 
    [JsonPropertyName("KickUnreadyPlayerTime")] public int KickUnreadyPlayerTime { get; set; } = 360;
    
    [JsonPropertyName("UnreadyReminderInterval")] public int UnreadyReminderInterval { get; set; } = 60;
    [JsonPropertyName("PublicUnreadyReminderInterval")] public int PublicUnreadyReminderInterval { get; set; } = 15;
    
    [JsonPropertyName("WaitingForOpponentInterval")] public int WaitingForOpponentInterval { get; set; } = 30;

    [JsonPropertyName("ChatPrefix")] public string ChatPrefix { get; set; } = "[ {Green}狙 擊 模 式{White} ]";
    [JsonPropertyName("EnableChatWeaponCommands")] public bool EnableChatWeaponCommands { get; set; } = true;
    
    [JsonPropertyName("SpawnWeapons")] 
    public List<string> SpawnWeapons { get; set; } = ["weapon_knife", "item_assaultsuit", "weapon_awp"];
    
    [JsonPropertyName("WarmupConfigName")] public string WarmupConfigName { get; set; } = "warmup.cfg";
    [JsonPropertyName("LiveConfigName")] public string LiveConfigName { get; set; } = "live.cfg";
    [JsonPropertyName("Duel_MapChangeDelay")] public int MapChangeDelay { get; set; } = 5;
    
    [JsonPropertyName("MapList")] 
    public List<string> MapList { get; set; } = ["Aim_redline_vieforit:3290337428", "aimpro_vieforit:3290753343"];

    // 獨立秒數設定
    [JsonPropertyName("HudDuration_Prep")] public float HudDuration_Prep { get; set; } = 5.0f; 
    [JsonPropertyName("HudDuration_MatchAbort")] public float HudDuration_MatchAbort { get; set; } = 5.0f;
    [JsonPropertyName("HudDuration_MatchStart")] public float HudDuration_MatchStart { get; set; } = 5.0f;
    [JsonPropertyName("RoundStartHudDuration")] public float RoundStartHudDuration { get; set; } = 2.0f;
    [JsonPropertyName("Live_Execute_Delay")] public float Live_Execute_Delay { get; set; } = 4.0f;

    // HTML HUD 文本設定 (已完整解碼為中文與 HTML)
    [JsonPropertyName("HudHtml_Prep1v1_Line1")] 
    public string HudHtml_Prep1v1_Line1 { get; set; } = "<font class='fontSize-l' color='lime'><b>✦</font> <font class='fontSize-l' color='white'>人 數 觸 發 <font class='fontSize-l' color='gold'>1 v 1</font> 單 挑 </font><font class='fontSize-l' color='lime'>✦</font></b><br>";
    
    [JsonPropertyName("HudHtml_Prep1v1_Line2")] 
    public string HudHtml_Prep1v1_Line2 { get; set; } = "<font class='fontSize-l' color='white'><b>已 準 備：</font><font class='fontSize-l' color='lime'>{0} / 2</font><font class='fontSize-l' color='white'> 尚 缺 <font class='fontSize-l' color='lime'><b>{1}</b></font> 人</font></b>";
    
    [JsonPropertyName("HudHtml_Prep2v2_Line1")] 
    public string HudHtml_Prep2v2_Line1 { get; set; } = "<font class='fontSize-l' color='lime'><b>✦</font> <font class='fontSize-l' color='white'>人 數 觸 發 <font class='fontSize-l' color='gold'>2 v 2</font> 團 戰 </font><font class='fontSize-l' color='lime'>✦</font></b><br>";
    
    [JsonPropertyName("HudHtml_Prep2v2_Line2")] 
    public string HudHtml_Prep2v2_Line2 { get; set; } = "<font class='fontSize-l' color='white'><b>已 準 備：</font><font class='fontSize-l' color='lime'>{0} / {2}</font><font class='fontSize-l' color='white'> 尚 缺 <font class='fontSize-l' color='lime'><b>{1}</b></font> 人</font></b>";

    [JsonPropertyName("HudHtml_Prep3v3_Line1")] 
    public string HudHtml_Prep3v3_Line1 { get; set; } = "<font class='fontSize-l' color='lime'><b>✦</font> <font class='fontSize-l' color='white'>人 數 觸 發<font class='fontSize-l' color='gold'>3 v 3</font> 團 戰 </font><font class='fontSize-l' color='lime'>✦</font></b><br>";
    
    [JsonPropertyName("HudHtml_Prep3v3_Line2")] 
    public string HudHtml_Prep3v3_Line2 { get; set; } = "<font class='fontSize-l' color='white'><b>已 準 備：</font><font class='fontSize-l' color='lime'>{0} / {2}</font><font class='fontSize-l' color='white'> 尚 缺 <font class='fontSize-l' color='lime'><b>{1}</b></font> 人</font></b>";
    
    [JsonPropertyName("HudHtml_MatchAbort_Line1")] 
    public string HudHtml_MatchAbort_Line1 { get; set; } = "<font class='fontSize-l' color='gold'><b>有 玩 家 逃 跑 ， 戰 鬥 已 終 止</font></b><br>";

    [JsonPropertyName("HudHtml_MatchAbort_Line2")] 
    public string HudHtml_MatchAbort_Line2 { get; set; } = "<font class='fontSize-l' color='lime'><b>比 賽 已 退 回 暖 身 模 式</font></b>";

    [JsonPropertyName("HudHtml_Round1_Line1")] 
    public string HudHtml_Round1_Line1 { get; set; } = "<font class='fontSize-l' color='gold'><b>★ 狙 擊 戰 鬥 開 始 ★</font></b><br>";

    [JsonPropertyName("HudHtml_Round1_Line2")] 
    public string HudHtml_Round1_Line2 { get; set; } = "<font class='fontSize-l' color='white'><b>對 戰 採</font><font class='fontSize-l' color='lime'><b>２０</b></font><font class='fontSize-l' color='white'> 回 合 勝 利 制</font></b>";

    [JsonPropertyName("HudHtml_RoundStart_Title")]
    public string HudHtml_RoundStart_Title { get; set; } = "<font class='fontSize-l' color='lime'>{0}：</font> <font class='fontSize-l' color='gold'><font color='red'>{1}</font> 模式/搶<font class='fontSize-l' color='Green'><b>３０</b></font><font class='fontSize-l' color='gold'>勝</font><br>";

    [JsonPropertyName("HudHtml_RoundStart_TScore")]
    public string HudHtml_RoundStart_TScore { get; set; } = "<font class='fontSize-l' color='#FF4500'><b>目 前 恐 怖 份 子：{0}</b></font><br>";

    [JsonPropertyName("HudHtml_RoundStart_CTScore")]
    public string HudHtml_RoundStart_CTScore { get; set; } = "<font class='fontSize-l' color='lightblue'><b>目 前 反 恐 精 英：{0}</b></font><br><font class='fontSize-l' color='gold'>比 賽 贏</font> <font class='fontSize-l' color='Green'><b>３０</b></font> <font class='fontSize-l' color='gold'>回合 為 主</font>";
}

public class LiteMatchManager : BasePlugin, IPluginConfig<LiteMatchConfig>
{
    public override string ModuleName => "LiteMatchManager";
    public override string ModuleVersion => "8.75_JSON_Decoded";
    public override string ModuleAuthor => "Optimized";
    public override string ModuleDescription => "完美同步 JSON 設定，涵蓋單一防殘影 HUD 系統與計分板功能";

    public LiteMatchConfig Config { get; set; } = new();

    private string _cachedPrefix = "";
    
    private HashSet<ulong> _readyPlayers = new(64);
    private Dictionary<ulong, int> _playerUnreadyTime = new(64); 
    private List<string> _unreadyNamesCache = new(64); 
    private Dictionary<ulong, string> _playerPrimary = new(64);
    private Dictionary<ulong, float> _pendingInitialReminders = new(64);
    private HashSet<ulong> _hasReceivedInitialReminder = new(64);

    private bool _isMatchLive = false;
    private bool _isChangingMap = false; 
    private int _liveMatchTargetPlayers = 0; 
    private bool _isServerShuttingDown = false; 
    
    private CounterStrikeSharp.API.Modules.Timers.Timer? _privateCheckTimer;
    private CounterStrikeSharp.API.Modules.Timers.Timer? _publicBroadcastTimer;
    private CounterStrikeSharp.API.Modules.Timers.Timer? _waitingTimer;
    private CounterStrikeSharp.API.Modules.Timers.Timer? _liveTimer; 

    private CCSGameRules? _gameRules;
    private bool _gameRulesInitialized;

    private CCSTeam? _cachedTeamT = null;
    private CCSTeam? _cachedTeamCT = null;

    // HUD 專用動態變數
    private string _activeCenterMessage = "";
    private float _centerMessageExpiration = 0f;

    private void InitializeGameRules()
    {
        if (_gameRulesInitialized) return;
        
        foreach (var proxy in Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules"))
        {
            _gameRules = proxy.GameRules;
            break;
        }
        
        _gameRulesInitialized = _gameRules is not null;
    }

    // 統一由這裡控制 HUD 並發送秒數
    private void ShowHud(string html, float duration)
    {
        _activeCenterMessage = html;
        _centerMessageExpiration = Server.CurrentTime + duration;
    }

    private void OnTick()
    {
        if (!_gameRulesInitialized) InitializeGameRules();

        bool shouldFreezeUI = false;

        if (!string.IsNullOrEmpty(_activeCenterMessage))
        {
            if (Server.CurrentTime <= _centerMessageExpiration)
            {
                shouldFreezeUI = true; 
                foreach (var p in Utilities.GetPlayers())
                {
                    if (p is { IsValid: true, IsBot: false }) p.PrintToCenterHtml(_activeCenterMessage);
                }
            }
            else
            {
                _activeCenterMessage = "";
                foreach (var p in Utilities.GetPlayers())
                {
                    if (p is { IsValid: true, IsBot: false }) p.PrintToCenterHtml("");
                }
            }
        }

        // 凍結 UI 避免 HTML 5秒殘留
        if (_gameRules is not null)
        {
            if (shouldFreezeUI) _gameRules.GameRestart = _gameRules.RestartRoundTime < Server.CurrentTime;
            else _gameRules.GameRestart = false; 
        }

        if (_pendingInitialReminders.Count > 0)
        {
            float currentTime = Server.CurrentTime;
            List<ulong>? toRemove = null;

            foreach (var kvp in _pendingInitialReminders)
            {
                if (currentTime >= kvp.Value)
                {
                    ulong steamId = kvp.Key;
                    toRemove ??= []; 
                    toRemove.Add(steamId);

                    if (!_isMatchLive && !_readyPlayers.Contains(steamId))
                    {
                        foreach (var p in Utilities.GetPlayers())
                        {
                            if (p is { IsValid: true, SteamID: var id, TeamNum: 2 or 3 } && id == steamId)
                            {
                                _playerUnreadyTime.TryGetValue(steamId, out int elapsed);
                                int timeLeft = Config.KickUnreadyPlayerTime - elapsed;
                                
                                p.PrintToChat($" {_cachedPrefix} 請輸入 {ChatColors.Lime}!R{ChatColors.White} 準備 ，{ChatColors.Lime}{timeLeft}{ChatColors.White} 秒未準備將被踢出");
                                break;
                            }
                        }
                    }
                }
            }

            if (toRemove is not null)
            {
                foreach (var id in toRemove) _pendingInitialReminders.Remove(id);
            }
        }
    }

    public void OnConfigParsed(LiteMatchConfig config)
    {
        Config = config;
        _cachedPrefix = config.ChatPrefix
            .Replace("{White}", ChatColors.White.ToString())
            .Replace("{Red}", ChatColors.Red.ToString())
            .Replace("{Green}", ChatColors.Green.ToString())
            .Replace("{Lime}", ChatColors.Lime.ToString())
            .Replace("{LightBlue}", ChatColors.LightBlue.ToString())
            .Replace("{Yellow}", ChatColors.Yellow.ToString())
            .Replace("{Gold}", ChatColors.Gold.ToString())
            .Replace("{Orange}", ChatColors.Orange.ToString());
    }

    public override void Load(bool hotReload)
    {
        Console.WriteLine("=================================================");
        Console.WriteLine("  LiteMatchManager JSON 繁體中文反解碼版 啟動！");
        Console.WriteLine("=================================================");

        _isServerShuttingDown = false;

        AddCommandListener("say", OnPlayerSay);
        AddCommandListener("say_team", OnPlayerSay);
        AddCommandListener("jointeam", OnJoinTeam);
        AddCommandListener("drop", (player, info) => HookResult.Handled);
        
        RegisterListener<Listeners.OnTick>(OnTick);
        RegisterEventHandler<EventMapShutdown>((@event, info) => {
            _isServerShuttingDown = true;
            return HookResult.Continue;
        });

        RegisterEventHandler<EventPlayerDisconnect>((@event, info) =>
        {
            if (@event.Userid is { SteamID: > 0 } player)
            {
                try 
                {
                    ulong steamId = player.SteamID;
                    _readyPlayers.Remove(steamId);
                    _playerUnreadyTime.Remove(steamId);
                    _playerPrimary.Remove(steamId);
                    _pendingInitialReminders.Remove(steamId);
                    _hasReceivedInitialReminder.Remove(steamId);

                    if (_isMatchLive) CheckAndResetGameImmediate();
                    else Server.NextFrame(CheckMatchStart);
                } 
                catch (Exception) { }
            }
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

                    if (_readyPlayers.Remove(steamId))
                    {
                        if (!_isMatchLive)
                            Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Orange}{player.PlayerName}{ChatColors.White} 跳 去 觀 戰，已 取 消 準 備");
                        else
                            Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Orange}{player.PlayerName}{ChatColors.White} 退 出 了 戰 鬥，移 至 觀 戰 ");
                    }
                    _playerUnreadyTime.Remove(steamId); 
                }

                if (!_isMatchLive)
                {
                    Server.NextFrame(CheckMatchStart);
                }
                else if (newTeam is 2 or 3)
                {
                    if (!_readyPlayers.Contains(steamId))
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
                                    string modeText = _liveMatchTargetPlayers == 2 ? "單 挑" : "團 戰";
                                    player.PrintToChat($" {_cachedPrefix} {ChatColors.Orange}{modeText} 比 賽 已 開 始，無 法 中 途 加 入");
                                }
                            });
                        }
                        else
                        {
                            _readyPlayers.Add(steamId);
                        }
                    }
                    CheckAndResetGameImmediate();
                }
            }
            return HookResult.Continue;
        }, HookMode.Post);

        RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
        RegisterEventHandler<EventCsWinPanelMatch>(OnMatchEnd);
        
        // 註冊回合開始事件 (計分板)
        RegisterEventHandler<EventRoundStart>(OnEventRoundStart);

        RegisterListener<Listeners.OnMapStart>(mapName => 
        {
            _isServerShuttingDown = false;
            _gameRules = null;
            _gameRulesInitialized = false;
            
            _cachedTeamT = null;
            _cachedTeamCT = null;

            ResetMatchState();
            Console.WriteLine($"[LiteMatch] [StartWarmup] 地圖載入完成！準備執行暖身設定檔：{Config.WarmupConfigName}");
            Server.NextFrame(() => {
                Server.ExecuteCommand($"exec {Config.WarmupConfigName}");
            });
        });

        if (hotReload) InitializeGameRules();
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

    private void CheckAndResetGameImmediate()
    {
        Server.NextFrame(() => {
            if (_isServerShuttingDown || !_isMatchLive || _isChangingMap) return; 
            try 
            {
                if (_cachedTeamT is not { IsValid: true } || _cachedTeamCT is not { IsValid: true })
                {
                    foreach (var team in Utilities.FindAllEntitiesByDesignerName<CCSTeam>("cs_team_manager"))
                    {
                        if (team.TeamNum == 2) _cachedTeamT = team;
                        else if (team.TeamNum == 3) _cachedTeamCT = team;
                    }
                }

                if (_cachedTeamT is { Score: >= 30 } || _cachedTeamCT is { Score: >= 30 }) return;

                int activeT = 0, activeCT = 0;
                foreach (var p in Utilities.GetPlayers())
                {
                    if (p is { IsValid: true, Handle: not 0, IsBot: false })
                    {
                        if (p.TeamNum == 2) activeT++;
                        else if (p.TeamNum == 3) activeCT++;
                    }
                }
                if (activeT == 0 || activeCT == 0) AbortMatch();
            } 
            catch (Exception) { }
        });
    }

    private void AbortMatch()
    {
        if (!_isMatchLive) return;
        
        _liveTimer?.Kill();
        _liveTimer = null;

        ShowHud($"{Config.HudHtml_MatchAbort_Line1}<br>{Config.HudHtml_MatchAbort_Line2}<br>", Config.HudDuration_MatchAbort);

        Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Orange}玩 家 離 退 對 戰 終 止，請 重 新 輸 入 {ChatColors.Lime}!R {ChatColors.Orange}對 戰");
        Server.ExecuteCommand("mp_warmup_start");
        
        var pauseConVar = ConVar.Find("mp_warmup_pausetimer");
        if (pauseConVar is not null) pauseConVar.SetValue(1);
        else Server.ExecuteCommand("mp_warmup_pausetimer 1");
        
        ResetMatchState();
        Console.WriteLine($"[LiteMatch] [AbortMatch] 對戰已終止！正在切換回暖身設定檔：{Config.WarmupConfigName}");
        Server.NextFrame(() => { Server.ExecuteCommand($"exec {Config.WarmupConfigName}"); });
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
                    player.PrintToChat($" {_cachedPrefix} {ChatColors.Orange}對 戰 進 行 中，無 法 切 換 隊 伍！");
                    return HookResult.Handled;
                }

                if (!_readyPlayers.Contains(player.SteamID))
                {
                    int liveTeamMax = _liveMatchTargetPlayers / 2;
                    int currentTeamCount = 0;
                    foreach (var p in Utilities.GetPlayers())
                    {
                        if (p is { IsValid: true, Handle: not 0, IsBot: false, TeamNum: var team } && team == teamIndex && p.SteamID != player.SteamID)
                        {
                            currentTeamCount++;
                        }
                    }
                    
                    if (currentTeamCount >= liveTeamMax)
                    {
                        string modeText = _liveMatchTargetPlayers == 2 ? "單 挑" : "團 戰";
                        player.PrintToChat($" {_cachedPrefix} {ChatColors.Orange}{modeText} 比 賽 已 開 始，無 法 中 途 加 入");
                        return HookResult.Handled;
                    }
                }
            }
            else 
            {
                int currentTeamCount = 0;
                foreach (var p in Utilities.GetPlayers())
                {
                    if (p is { IsValid: true, Handle: not 0, IsBot: false, TeamNum: var team } && team == teamIndex && p.SteamID != player.SteamID)
                    {
                        currentTeamCount++;
                    }
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
        if (string.IsNullOrEmpty(rawArg)) return HookResult.Continue;

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
            string senderPrefix = $" {ChatColors.White}[所有人]{ChatColors.White}";
            
            string nameColor = player.TeamNum switch {
                1 => $"{ChatColors.Grey}",
                2 => "\x10",
                3 => "\x0B",
                _ => $"{ChatColors.White}"
            };

            string formattedMessage = $"{senderPrefix} {nameColor}{playerName}{ChatColors.White}：{message}";

            foreach (var p in Utilities.GetPlayers())
            {
                if (p is { IsValid: true, IsBot: false }) p.PrintToChat(formattedMessage);
            }

            return HookResult.Handled; 
        }

        string command = rawArg.Trim('"', ' ').ToLower();

        if (command == "!nextmap")
        {
            if (AdminManager.PlayerHasPermissions(player, "@css/root")) TriggerMapChange(false);
            else player.PrintToChat($" {_cachedPrefix} {ChatColors.Red}你沒有權限執行此指令。");
            return HookResult.Handled;
        }

        if (command is "!r" or "!ready")
        {
            if (!_isMatchLive) HandlePlayerReady(player);
            return HookResult.Continue; 
        }
        else if (command == "!unready")
        {
            if (!_isMatchLive) HandlePlayerUnready(player);
            return HookResult.Continue; 
        }

        if (Config.EnableChatWeaponCommands && HandleWeaponCommand(player, command)) 
        {
            return HookResult.Continue; 
        }
        
        return HookResult.Continue;
    }

    private void TriggerMapChange(bool isMatchEnd = false)
    {
        if (_isChangingMap || Config.MapList is null || Config.MapList.Count == 0) return;
        _isChangingMap = true;
        
        var random = new Random();
        string selectedMapString = Config.MapList[random.Next(Config.MapList.Count)];
        string[] parts = selectedMapString.Split(':');
        string mapName = parts[0];
        string workshopId = parts.Length > 1 ? parts[1] : "";

        if (!isMatchEnd)
        {
            Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Gold}管理員強制換圖：即將切換至 {ChatColors.Lime}{mapName}");
            Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Orange}{Config.MapChangeDelay} 秒後 {ChatColors.Gold}自動載入...");
        }
        else
        {
            Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Orange}{Config.MapChangeDelay} 秒後 {ChatColors.Gold}自動載入下一張地圖：{ChatColors.Lime}{mapName} ...");
        }

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
            player.PrintToChat($" {_cachedPrefix} {ChatColors.Orange}您 無 法 從 旁 觀 者 模 式 加 入 對 戰");
            return;
        }

        ulong steamId = player.SteamID;
        if (!_readyPlayers.Add(steamId)) 
        {
            player.PrintToChat($" {_cachedPrefix} 你已經是 {ChatColors.Green}準備完成{ChatColors.White} 的狀態了！");
            return;
        }

        _playerUnreadyTime.Remove(steamId); 
        _pendingInitialReminders.Remove(steamId); 

        int targetPlayers = GetDynamicRequiredPlayers();
        int missingPlayers = targetPlayers - _readyPlayers.Count;
        Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Orange}{player.PlayerName}{ChatColors.White} 已 準 備！準 備 進 度：{ChatColors.Green}{_readyPlayers.Count} / {targetPlayers}");
        
        string prepString = targetPlayers switch
        {
            <= 2 => $"{Config.HudHtml_Prep1v1_Line1}<br>{string.Format(Config.HudHtml_Prep1v1_Line2, _readyPlayers.Count, missingPlayers)}<br>",
            <= 4 => $"{Config.HudHtml_Prep2v2_Line1}<br>{string.Format(Config.HudHtml_Prep2v2_Line2, _readyPlayers.Count, missingPlayers, targetPlayers)}<br>",
            _ => $"{Config.HudHtml_Prep3v3_Line1}<br>{string.Format(Config.HudHtml_Prep3v3_Line2, _readyPlayers.Count, missingPlayers, targetPlayers)}<br>"
        };

        ShowHud(prepString, Config.HudDuration_Prep);
        CheckMatchStart();

        if (!_isMatchLive)
        {
            int activePlayers = 0;
            foreach (var p in Utilities.GetPlayers())
            {
                if (p is { IsValid: true, IsBot: false, IsHLTV: false, TeamNum: 2 or 3 }) activePlayers++;
            }

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
            Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Red}{player.PlayerName}{ChatColors.White} 取 消 了 準 備！準 備 進 度：{ChatColors.Green}{_readyPlayers.Count} / {targetPlayers}");
            
            string prepString = targetPlayers switch
            {
                <= 2 => $"{Config.HudHtml_Prep1v1_Line1}<br>{string.Format(Config.HudHtml_Prep1v1_Line2, _readyPlayers.Count, missingPlayers)}<br>",
                <= 4 => $"{Config.HudHtml_Prep2v2_Line1}<br>{string.Format(Config.HudHtml_Prep2v2_Line2, _readyPlayers.Count, missingPlayers, targetPlayers)}<br>",
                _ => $"{Config.HudHtml_Prep3v3_Line1}<br>{string.Format(Config.HudHtml_Prep3v3_Line2, _readyPlayers.Count, missingPlayers, targetPlayers)}<br>"
            };

            ShowHud(prepString, Config.HudDuration_Prep);
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
            _liveMatchTargetPlayers = totalPlayers; 
            
            string modeText = totalPlayers == 2 ? "1 v 1 單 挑" : $"{activeT} v {activeCT} 團 戰";
            string hudStartText = $"{Config.HudHtml_Round1_Line1}<br>{Config.HudHtml_Round1_Line2}<br>";
            
            ShowHud(hudStartText, Config.HudDuration_MatchStart);
            Server.PrintToChatAll($" {_cachedPrefix} 所 有 玩 家 已 準 備，{modeText} 比 賽 開 始");
            Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Orange}對 戰 開 始！採 贏{ChatColors.Default} {ChatColors.Green}３０{ChatColors.Default} {ChatColors.Orange}回 合 制{ChatColors.Default}。");
            
            _privateCheckTimer?.Kill(); _privateCheckTimer = null;
            _publicBroadcastTimer?.Kill(); _publicBroadcastTimer = null;
            _waitingTimer?.Kill(); _waitingTimer = null;
            
            Console.WriteLine($"[LiteMatch] [MatchLive] 雙方準備就緒 ({modeText})！將於 {Config.Live_Execute_Delay} 秒後執行開賽設定檔：{Config.LiveConfigName}");
            _liveTimer?.Kill();
            _liveTimer = AddTimer(Config.Live_Execute_Delay, () => 
            {
                Server.NextFrame(() => { Server.ExecuteCommand($"exec {Config.LiveConfigName}"); });
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
            }
        }

        if (_isMatchLive && player.TeamNum is 2 or 3)
        {
            if (!_readyPlayers.Contains(steamId))
            {
                Server.NextFrame(() => {
                    if (player.IsValid) {
                        player.ChangeTeam(CsTeam.Spectator);
                        player.PrintToChat($" {_cachedPrefix} {ChatColors.Orange}比 賽 已 開 始，非 參 賽 者 無 法 加 入");
                    }
                });
                return HookResult.Continue; 
            }
        }
        
        Server.NextFrame(() => {
            Server.NextFrame(() => {
                if (player is not { IsValid: true, PawnIsAlive: true } || player.PlayerPawn.Value is not { IsValid: true }) return;
                
                player.RemoveWeapons(); 
                
                foreach (var item in Config.SpawnWeapons) 
                { 
                    string weaponToGive = item;

                    if (item.StartsWith("weapon_") && !item.Contains("knife") && !item.Contains("bayonet"))
                    {
                        if (_playerPrimary.TryGetValue(steamId, out string? prefPri)) weaponToGive = prefPri;
                    }
                    player.GiveNamedItem(weaponToGive); 
                }
            });
        });
        
        return HookResult.Continue;
    }

    private bool HandleWeaponCommand(CCSPlayerController player, string command)
    {
        if (!player.PawnIsAlive) return false;
        ulong steamId = player.SteamID;
        
        switch (command)
        {
            case "!ssg": _playerPrimary[steamId] = "weapon_ssg08"; ReplaceWeapon(player, "weapon_ssg08"); return true;
            case "!awp": _playerPrimary[steamId] = "weapon_awp"; ReplaceWeapon(player, "weapon_awp"); return true;
            case "!gs": OnGsCommand(player, null!); return true;
        }
        return false;
    }

    private void ReplaceWeapon(CCSPlayerController player, string newWeapon)
    {
        if (player.PlayerPawn.Value?.WeaponServices?.MyWeapons is not { } weapons) return;

        foreach (var weaponHandle in weapons)
        {
            if (weaponHandle.Value is { IsValid: true } weapon)
            {
                string wName = weapon.DesignerName;
                if (string.IsNullOrEmpty(wName) || wName.Contains("knife") || wName.Contains("bayonet") || wName.Contains("c4")) continue;

                weapon.Remove();
                Server.NextFrame(() => {
                    if (player is { IsValid: true, PawnIsAlive: true }) {
                        player.GiveNamedItem(newWeapon);
                    }
                });
                return; 
            }
        }
        player.GiveNamedItem(newWeapon);
    }

    private void OnGsCommand(CCSPlayerController? player, CommandInfo info)
    {
        if (player is not { IsValid: true }) return;
        player.PrintToChat($" {ChatColors.Orange} 禁 用 所 有 小 槍 、請 在 聊 天 欄 位 輸 入 您 要 的 武 器");
        player.PrintToChat($" ---------------------------------------------------------------");
        player.PrintToChat($" [ {ChatColors.Green}狙擊{ChatColors.White} ] {ChatColors.Green}!SSG {ChatColors.White}[ SSG 08 鳥狙 ] 、{ChatColors.Green}!AWP {ChatColors.White}[ AWP狙擊步槍 ]");
    }

    private void CheckAndWarnUnreadyPlayers()
    {
        if (_isMatchLive) return; 

        try 
        {
            foreach (var p in Utilities.GetPlayers())
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
                            Server.NextFrame(() => {
                                Server.ExecuteCommand($"kickid {p.UserId} Unready_Timeout");
                            });
                            Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Lime}{kickedName} {ChatColors.White}因 未 準 備 好 而 被 踢 出");
                            _playerUnreadyTime.Remove(steamId);
                        }
                        else
                        {
                            int timeLeft = Config.KickUnreadyPlayerTime - _playerUnreadyTime[steamId];
                            p.PrintToChat($" {_cachedPrefix} 請輸入 {ChatColors.Lime}!R{ChatColors.White} 準備 ，{ChatColors.Lime}{timeLeft}{ChatColors.White} 秒未準備將被踢出");
                        }
                    }
                }
            }
        } 
        catch (Exception) { }
    }

    private void BroadcastWaitingMessage()
    {
        if (_isMatchLive) return;
        
        int totalPlayers = 0;
        foreach (var p in Utilities.GetPlayers())
        {
            if (p is { IsValid: true, Handle: not 0, IsBot: false, IsHLTV: false, TeamNum: 2 or 3 }) totalPlayers++;
        }

        if (totalPlayers > 0 && totalPlayers == _readyPlayers.Count)
        {
            string modeHint = totalPlayers == 1 
                ? $" [ {ChatColors.Green}動 態 判 斷{ChatColors.White} ] {ChatColors.White}場 上 {ChatColors.Green}1 {ChatColors.White}人，等 待 對 手 加 入..."
                : $" [ {ChatColors.Green}動 態 判 斷{ChatColors.White} ] {ChatColors.White}場 上 {ChatColors.Green}{totalPlayers} {ChatColors.White}人，等 對 手 加 入 {ChatColors.Green}{GetDynamicRequiredPlayers() / 2} v {GetDynamicRequiredPlayers() / 2} {ChatColors.White}團 戰";

            Server.PrintToChatAll(modeHint);
        }
    }

    private void BroadcastUnreadyPlayers()
    {
        if (_isMatchLive) return; 
        try 
        {
            _unreadyNamesCache.Clear();
            int totalPlayers = 0; 
            
            foreach (var p in Utilities.GetPlayers())
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
                    2 => $" [ {ChatColors.Green}動 態 判 斷{ChatColors.White} ] {ChatColors.White}目 前 場 上 {ChatColors.Green}2 {ChatColors.White}人，雙 方 輸 入 {ChatColors.Orange}!R {ChatColors.White}即 可 直 接 {ChatColors.Green}1 v 1 單 挑{ChatColors.White}",
                    > 2 => $" [ {ChatColors.Green}動 態 判 斷{ChatColors.White} ] {ChatColors.White}已觸發團戰，需滿 {ChatColors.Green}{targetPlayers} {ChatColors.White}人輸入 {ChatColors.Orange}!R {ChatColors.White}可開始 {ChatColors.Green}{teamSize} v {teamSize} 團戰{ChatColors.White}",
                    _ => ""
                };
                
                if (_unreadyNamesCache.Count > 0)
                {
                    Server.PrintToChatAll($" {_cachedPrefix} 尚未準備玩家：{ChatColors.Orange}{string.Join(", ", _unreadyNamesCache)}{ChatColors.Default} | 對戰需滿 {ChatColors.Green}{targetPlayers}{ChatColors.Default} 人");
                }
                
                if (!string.IsNullOrEmpty(modeHint)) Server.PrintToChatAll(modeHint); 
            }
        }
        catch (Exception) { }
    }

    private void ResetMatchState()
    {
        _isMatchLive = false;
        _isChangingMap = false;
        _liveMatchTargetPlayers = 0; 
        _readyPlayers.Clear();
        _playerUnreadyTime.Clear();
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
        if (!_isMatchLive) return HookResult.Continue;
        
        if (_cachedTeamT is not { IsValid: true } || _cachedTeamCT is not { IsValid: true })
        {
            foreach (var team in Utilities.FindAllEntitiesByDesignerName<CCSTeam>("cs_team_manager"))
            {
                if (team.TeamNum == 2) _cachedTeamT = team;
                else if (team.TeamNum == 3) _cachedTeamCT = team;
            }
        }

        int scoreT = _cachedTeamT?.Score ?? 0;
        int scoreCT = _cachedTeamCT?.Score ?? 0;
        string modeStr = _liveMatchTargetPlayers <= 2 ? "單 挑" : "團 戰";

        string fullHudHtml = string.Format(Config.HudHtml_RoundStart_Title, "對戰進度", modeStr) + 
                             string.Format(Config.HudHtml_RoundStart_TScore, scoreT) + 
                             string.Format(Config.HudHtml_RoundStart_CTScore, scoreCT);

        ShowHud(fullHudHtml, Config.RoundStartHudDuration);

        return HookResult.Continue;
    }

    private HookResult OnMatchEnd(EventCsWinPanelMatch @event, GameEventInfo info)
    {
        if (!_isMatchLive) return HookResult.Continue;
        
        if (_cachedTeamT is not { IsValid: true } || _cachedTeamCT is not { IsValid: true })
        {
            foreach (var team in Utilities.FindAllEntitiesByDesignerName<CCSTeam>("cs_team_manager"))
            {
                if (team.TeamNum == 2) _cachedTeamT = team;
                else if (team.TeamNum == 3) _cachedTeamCT = team;
            }
        }

        int scoreT = _cachedTeamT?.Score ?? 0;
        int scoreCT = _cachedTeamCT?.Score ?? 0;

        string winnerName = scoreT > scoreCT ? "恐怖份子 (T)" : "反恐小組 (CT)";
        string loserName = scoreT > scoreCT ? "反恐小組 (CT)" : "恐怖份子 (T)";
        
        int winnerScore = Math.Max(scoreT, scoreCT);
        int loserScore = Math.Min(scoreT, scoreCT);
        string scoreString = $"{winnerScore} : {loserScore}";

        Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Lime}{winnerName} {ChatColors.Gold}以 {ChatColors.Green}({scoreString}) {ChatColors.Gold}的分數贏過 {ChatColors.Lime}{loserName}");

        TriggerMapChange(true);
        return HookResult.Continue;
    }

    public override void Unload(bool hotReload)
    {
        _isServerShuttingDown = true;
        base.Unload(hotReload);
    }
}
