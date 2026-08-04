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
    
    // ✅ 這是你原本指定的 4 個黑框秒數 (專心管黑框，不干涉伺服器流程)
    [JsonPropertyName("HudDuration_Prep")] public float HudDuration_Prep { get; set; } = 5.0f;
    [JsonPropertyName("HudDuration_Start")] public float HudDuration_Start { get; set; } = 4.0f;
    [JsonPropertyName("HudDuration_Abort")] public float HudDuration_Abort { get; set; } = 5.0f;
    [JsonPropertyName("HudDuration_Round1")] public float HudDuration_Round1 { get; set; } = 3.0f;

    // ✅ 【全新新增】：專門獨立控制「跳轉到 Live.cfg」的等待秒數！
    [JsonPropertyName("Live_Execute_Delay")] public float Live_Execute_Delay { get; set; } = 4.0f;

    [JsonPropertyName("MapList")] 
    public List<string> MapList { get; set; } = ["Aim_redline_vieforit:3290337428", "aimpro_vieforit:3290753343"];

    [JsonPropertyName("HudHtml_Prep1v1_Line1")] 
    public string HudHtml_Prep1v1_Line1 { get; set; } = "<font class='fontSize-l' color='white'>✦ 觸 發 1 v 1 單 挑 ✦</font>";
    
    [JsonPropertyName("HudHtml_Prep1v1_Line2")] 
    public string HudHtml_Prep1v1_Line2 { get; set; } = "<font class='fontSize-l' color='gray'>進度： </font><font class='fontSize-l' color='lime'>{0} / 2</font><font class='fontSize-l' color='gray'> ( 尚缺 {1} 人 )</font>";
    
    [JsonPropertyName("HudHtml_Prep2v2_Line1")] 
    public string HudHtml_Prep2v2_Line1 { get; set; } = "<font class='fontSize-l' color='white'>✦ 觸 發 2 v 2 團 戰 ✦</font>";
    
    [JsonPropertyName("HudHtml_Prep2v2_Line2")] 
    public string HudHtml_Prep2v2_Line2 { get; set; } = "<font class='fontSize-l' color='gray'>進度： </font><font class='fontSize-l' color='lime'>{0} / {2}</font><font class='fontSize-l' color='gray'> ( 尚缺 {1} 人 )</font>";

    [JsonPropertyName("HudHtml_Prep3v3_Line1")] 
    public string HudHtml_Prep3v3_Line1 { get; set; } = "<font class='fontSize-l' color='white'>✦ 觸 發 3 v 3 大 亂 鬥 ✦</font>";
    
    [JsonPropertyName("HudHtml_Prep3v3_Line2")] 
    public string HudHtml_Prep3v3_Line2 { get; set; } = "<font class='fontSize-l' color='gray'>進度： </font><font class='fontSize-l' color='lime'>{0} / {2}</font><font class='fontSize-l' color='gray'> ( 尚缺 {1} 人 )</font>";
    
    [JsonPropertyName("HudHtml_MatchAbort_Line1")] 
    public string HudHtml_MatchAbort_Line1 { get; set; } = "<font class='fontSize-l' color='red'>[ 警 告 ] 玩 家 逃 跑 ， 戰 鬥 終 止</font>";

    [JsonPropertyName("HudHtml_MatchAbort_Line2")] 
    public string HudHtml_MatchAbort_Line2 { get; set; } = "<font class='fontSize-l' color='white'>已 退 回 暖 身 模 式</font>";

    [JsonPropertyName("HudHtml_Round1_Line1")] 
    public string HudHtml_Round1_Line1 { get; set; } = "<font class='fontSize-l' color='gold'>✦ 戰 鬥 開 始 ✦</font>";

    [JsonPropertyName("HudHtml_Round1_Line2")] 
    public string HudHtml_Round1_Line2 { get; set; } = "<font class='fontSize-l' color='white'>率 先 取 得 </font><font class='fontSize-xxl' color='lime'><b>２０</b></font><font class='fontSize-xxl' color='white'> 勝 者 為 贏 家</font>";
}

public class LiteMatchManager : BasePlugin, IPluginConfig<LiteMatchConfig>
{
    public override string ModuleName => "LiteMatchManager";
    public override string ModuleVersion => "8.61_IndependentTimers";
    public override string ModuleAuthor => "Optimized";
    public override string ModuleDescription => "黑框秒數與開賽跳轉秒數完全獨立，智慧凍結無殘留";

    public LiteMatchConfig Config { get; set; } = new LiteMatchConfig();

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
    
    private CounterStrikeSharp.API.Modules.Timers.Timer? _privateCheckTimer;
    private CounterStrikeSharp.API.Modules.Timers.Timer? _publicBroadcastTimer;
    private CounterStrikeSharp.API.Modules.Timers.Timer? _waitingTimer;
    private CounterStrikeSharp.API.Modules.Timers.Timer? _liveTimer; 

    private CCSGameRules? _gameRules;
    private bool _gameRulesInitialized;

    // 動態 UI 管理變數
    private string _activeCenterMessage = "";
    private float _centerMessageExpiration = 0f;

    private void InitializeGameRules()
    {
        if (_gameRulesInitialized) return;
        var gameRulesProxy = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").FirstOrDefault();
        _gameRules = gameRulesProxy?.GameRules;
        _gameRulesInitialized = _gameRules != null;
    }

    private void ShowHud(string html, float duration)
    {
        _activeCenterMessage = html;
        _centerMessageExpiration = Server.CurrentTime + duration;
    }

    private void OnTick()
    {
        if (!_gameRulesInitialized) InitializeGameRules();

        bool shouldFreezeUI = false;

        // 智慧型 UI 渲染與清理機制
        if (!string.IsNullOrEmpty(_activeCenterMessage))
        {
            if (Server.CurrentTime <= _centerMessageExpiration)
            {
                shouldFreezeUI = true; 
                foreach (var p in Utilities.GetPlayers())
                {
                    if (p != null && p.IsValid && !p.IsBot) p.PrintToCenterHtml(_activeCenterMessage);
                }
            }
            else
            {
                _activeCenterMessage = "";
                foreach (var p in Utilities.GetPlayers())
                {
                    if (p != null && p.IsValid && !p.IsBot) p.PrintToCenterHtml("");
                }
            }
        }

        // 智慧控制 GameRestart 引擎凍結開關 (修復幽靈遮擋框的關鍵)
        if (_gameRules != null)
        {
            if (shouldFreezeUI)
            {
                _gameRules.GameRestart = _gameRules.RestartRoundTime < Server.CurrentTime;
            }
            else
            {
                _gameRules.GameRestart = false; 
            }
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
                    toRemove ??= new List<ulong>();
                    toRemove.Add(steamId);

                    if (!_isMatchLive && !_readyPlayers.Contains(steamId))
                    {
                        foreach (var p in Utilities.GetPlayers())
                        {
                            if (p != null && p.IsValid && p.SteamID == steamId && (p.TeamNum == 2 || p.TeamNum == 3))
                            {
                                int elapsed = 0;
                                if (_playerUnreadyTime.TryGetValue(steamId, out int val)) elapsed = val;
                                int timeLeft = Config.KickUnreadyPlayerTime - elapsed;
                                
                                p.PrintToChat($" {_cachedPrefix} 請輸入 {ChatColors.Lime}!R{ChatColors.White} 準備 ，{ChatColors.Lime}{timeLeft}{ChatColors.White} 秒未準備將被踢出");
                                break;
                            }
                        }
                    }
                }
            }

            if (toRemove != null)
            {
                foreach (var id in toRemove)
                {
                    _pendingInitialReminders.Remove(id);
                }
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
        Console.WriteLine("  LiteMatchManager v8.61 (秒數完全獨立版) 啟動！");
        Console.WriteLine("=================================================");

        AddCommandListener("say", OnPlayerSay);
        AddCommandListener("say_team", OnPlayerSay);
        AddCommandListener("jointeam", OnJoinTeam);
        
        AddCommandListener("drop", (player, info) => {
            return HookResult.Handled;
        });
        
        RegisterListener<Listeners.OnTick>(OnTick);
        
        RegisterEventHandler<EventPlayerDisconnect>((@event, info) =>
        {
            var player = @event.Userid;
            if (player != null)
            {
                try 
                {
                    ulong steamId = player.SteamID;
                    if (steamId > 0)
                    {
                        _readyPlayers.Remove(steamId);
                        _playerUnreadyTime.Remove(steamId);
                        _playerPrimary.Remove(steamId);
                        
                        _pendingInitialReminders.Remove(steamId);
                        _hasReceivedInitialReminder.Remove(steamId);

                        if (_isMatchLive) 
                        {
                            CheckAndResetGameImmediate();
                        }
                        else 
                        {
                            Server.NextFrame(() => {
                                CheckMatchStart(); 
                            });
                        }
                    }
                } 
                catch (Exception) { }
            }
            return HookResult.Continue;
        });

        RegisterEventHandler<EventPlayerTeam>((@event, info) =>
        {
            var player = @event.Userid;
            if (player != null && player.IsValid && player.Handle != IntPtr.Zero)
            {
                ulong steamId = player.SteamID;
                int newTeam = @event.Team; 
                
                if (newTeam == 0 || newTeam == 1) 
                {
                    _pendingInitialReminders.Remove(steamId);
                    _hasReceivedInitialReminder.Remove(steamId);

                    if (_readyPlayers.Contains(steamId))
                    {
                        _readyPlayers.Remove(steamId);
                        if (!_isMatchLive)
                            Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Orange}{player.PlayerName}{ChatColors.White} 跳 去 觀 戰，已 取 消 準 備");
                        else
                            Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Orange}{player.PlayerName}{ChatColors.White} 退 出 了 戰 鬥，移 至 觀 戰 ");
                    }
                    _playerUnreadyTime.Remove(steamId); 
                }

                if (!_isMatchLive)
                {
                    Server.NextFrame(() => {
                        CheckMatchStart(); 
                    });
                }
                else
                {
                    if (newTeam == 2 || newTeam == 3)
                    {
                        if (!_readyPlayers.Contains(steamId))
                        {
                            int liveTeamMax = _liveMatchTargetPlayers / 2;
                            int currentCount = 0;
                            foreach (var p in Utilities.GetPlayers())
                            {
                                if (p != null && p.IsValid && !p.IsBot && p.TeamNum == newTeam && p.SteamID != steamId)
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
                    }
                    CheckAndResetGameImmediate();
                }
            }
            return HookResult.Continue;
        }, HookMode.Post);

        RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
        RegisterEventHandler<EventCsWinPanelMatch>(OnMatchEnd);

        RegisterListener<Listeners.OnMapStart>(mapName => 
        {
            _gameRules = null;
            _gameRulesInitialized = false;

            ResetMatchState();
            Console.WriteLine($"[LiteMatch] [StartWarmup] 地圖載入完成！準備執行暖身設定檔：{Config.WarmupConfigName}");
            Server.NextFrame(() => {
                Server.ExecuteCommand($"exec {Config.WarmupConfigName}");
            });
        });

        if (hotReload)
        {
            InitializeGameRules();
        }
    }

    private int GetDynamicRequiredPlayers()
    {
        int activeT = 0;
        int activeCT = 0;
        foreach (var p in Utilities.GetPlayers())
        {
            if (p != null && p.IsValid && p.Handle != IntPtr.Zero && !p.IsBot && !p.IsHLTV)
            {
                if (p.TeamNum == 2) activeT++;
                if (p.TeamNum == 3) activeCT++;
            }
        }
        int total = activeT + activeCT;
        if (total <= 2) return 2;
        
        int target = (total % 2 == 1) ? total + 1 : total;
        
        int absoluteMax = Config.MaxPlayersPerTeam * 2;
        if (target > absoluteMax) return absoluteMax;
        
        return target;
    }

    private void CheckAndResetGameImmediate()
    {
        Server.NextFrame(() => {
            if (!_isMatchLive || _isChangingMap) return; 
            try 
            {
                int activeT = 0, activeCT = 0;
                foreach (var p in Utilities.GetPlayers())
                {
                    if (p != null && p.IsValid && p.Handle != IntPtr.Zero && !p.IsBot)
                    {
                        if (p.TeamNum == 2) activeT++;
                        if (p.TeamNum == 3) activeCT++;
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

        ShowHud($"{Config.HudHtml_MatchAbort_Line1}<br>{Config.HudHtml_MatchAbort_Line2}<br>", Config.HudDuration_Abort);

        Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Orange}玩 家 離 退 對 戰 終 止，請 重 新 輸 入 {ChatColors.Lime}!R {ChatColors.Orange}對 戰");
        Server.ExecuteCommand("mp_warmup_start");
        
        var pauseConVar = ConVar.Find("mp_warmup_pausetimer");
        if (pauseConVar != null) pauseConVar.SetValue(1);
        else Server.ExecuteCommand("mp_warmup_pausetimer 1");
        
        ResetMatchState();
        Console.WriteLine($"[LiteMatch] [AbortMatch] 對戰已終止！正在切換回暖身設定檔：{Config.WarmupConfigName}");
        Server.NextFrame(() => { Server.ExecuteCommand($"exec {Config.WarmupConfigName}"); });
    }

    private HookResult OnJoinTeam(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null || !player.IsValid) return HookResult.Continue;
        if (!int.TryParse(info.GetArg(1), out int teamIndex)) return HookResult.Continue;

        if (teamIndex == 2 || teamIndex == 3)
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
                        if (p != null && p.IsValid && p.Handle != IntPtr.Zero && !p.IsBot && p.TeamNum == teamIndex && p.SteamID != player.SteamID)
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
                    if (p != null && p.IsValid && p.Handle != IntPtr.Zero && !p.IsBot && p.TeamNum == teamIndex && p.SteamID != player.SteamID)
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
        if (player == null || !player.IsValid) return HookResult.Continue;
        string rawArg = info.GetArg(1);
        if (string.IsNullOrEmpty(rawArg)) return HookResult.Continue;

        bool isCommand = false;
        for (int i = 0; i < rawArg.Length; i++)
        {
            char c = rawArg[i];
            if (c == '"' || c == ' ') continue; 
            if (c == '!') { isCommand = true; break; }
            break; 
        }

        if (!isCommand) return HookResult.Continue; 
        string command = rawArg.Trim('"', ' ').ToLower();

        if (command == "!nextmap")
        {
            if (AdminManager.PlayerHasPermissions(player, "@css/root")) TriggerMapChange(false);
            else player.PrintToChat($" {_cachedPrefix} {ChatColors.Red}你沒有權限執行此指令。");
            return HookResult.Handled;
        }

        if (command == "!r" || command == "!ready")
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
        if (_isChangingMap || Config.MapList == null || Config.MapList.Count == 0) return;
        _isChangingMap = true;
        var random = new Random();
        string selectedMapString = Config.MapList[random.Next(Config.MapList.Count)];
        string[] parts = selectedMapString.Split(':');
        string mapName = parts[0];
        string workshopId = parts.Length > 1 ? parts[1] : "";

        if (!isMatchEnd)
        {
            Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Gold}管理員強制換圖：即極切換至 {ChatColors.Lime}{mapName}");
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
        if (player.TeamNum == 0 || player.TeamNum == 1)
        {
            player.PrintToChat($" {_cachedPrefix} {ChatColors.Orange}您 無 法 從 旁 觀 者 模 式 加 入 對 戰");
            return;
        }

        ulong steamId = player.SteamID;
        if (_readyPlayers.Contains(steamId))
        {
            player.PrintToChat($" {_cachedPrefix} 你已經是 {ChatColors.Green}準備完成{ChatColors.White} 的狀態了！");
            return;
        }

        _readyPlayers.Add(steamId);
        _playerUnreadyTime.Remove(steamId); 
        
        _pendingInitialReminders.Remove(steamId); 

        int targetPlayers = GetDynamicRequiredPlayers();
        int missingPlayers = targetPlayers - _readyPlayers.Count;
        Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Orange}{player.PlayerName}{ChatColors.White} 已 準 備！準 備 進 度：{ChatColors.Green}{_readyPlayers.Count} / {targetPlayers}");
        
        string prepString = "";
        if (targetPlayers <= 2)
        {
            prepString = $"{Config.HudHtml_Prep1v1_Line1}<br>{string.Format(Config.HudHtml_Prep1v1_Line2, _readyPlayers.Count, missingPlayers)}<br>";
        }
        else if (targetPlayers <= 4)
        {
            prepString = $"{Config.HudHtml_Prep2v2_Line1}<br>{string.Format(Config.HudHtml_Prep2v2_Line2, _readyPlayers.Count, missingPlayers, targetPlayers)}<br>";
        }
        else 
        {
            prepString = $"{Config.HudHtml_Prep3v3_Line1}<br>{string.Format(Config.HudHtml_Prep3v3_Line2, _readyPlayers.Count, missingPlayers, targetPlayers)}<br>";
        }

        ShowHud(prepString, Config.HudDuration_Prep);

        CheckMatchStart();

        if (!_isMatchLive)
        {
            int activePlayers = 0;
            foreach (var p in Utilities.GetPlayers())
            {
                if (p != null && p.IsValid && !p.IsBot && !p.IsHLTV && (p.TeamNum == 2 || p.TeamNum == 3))
                {
                    activePlayers++;
                }
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
        if (_readyPlayers.Contains(steamId))
        {
            _readyPlayers.Remove(steamId);
            _playerUnreadyTime[steamId] = 0; 
            
            int targetPlayers = GetDynamicRequiredPlayers();
            int missingPlayers = targetPlayers - _readyPlayers.Count;
            Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Red}{player.PlayerName}{ChatColors.White} 取 消 了 準 備！準 備 進 度：{ChatColors.Green}{_readyPlayers.Count} / {targetPlayers}");
            
            string prepString = "";
            if (targetPlayers <= 2)
            {
                prepString = $"{Config.HudHtml_Prep1v1_Line1}<br>{string.Format(Config.HudHtml_Prep1v1_Line2, _readyPlayers.Count, missingPlayers)}<br>";
            }
            else if (targetPlayers <= 4)
            {
                prepString = $"{Config.HudHtml_Prep2v2_Line1}<br>{string.Format(Config.HudHtml_Prep2v2_Line2, _readyPlayers.Count, missingPlayers, targetPlayers)}<br>";
            }
            else 
            {
                prepString = $"{Config.HudHtml_Prep3v3_Line1}<br>{string.Format(Config.HudHtml_Prep3v3_Line2, _readyPlayers.Count, missingPlayers, targetPlayers)}<br>";
            }

            ShowHud(prepString, Config.HudDuration_Prep);
        }
    }

    private void CheckMatchStart()
    {
        if (_isMatchLive) return;

        int activeT = 0, activeCT = 0;
        foreach (var p in Utilities.GetPlayers())
        {
            if (p != null && p.IsValid && p.Handle != IntPtr.Zero && !p.IsBot && !p.IsHLTV)
            {
                if (p.TeamNum == 2) activeT++;
                if (p.TeamNum == 3) activeCT++;
            }
        }
        int totalPlayers = activeT + activeCT;

        if (totalPlayers < 2) return; 
        if (activeT != activeCT) return; 
        if (activeT > Config.MaxPlayersPerTeam) return; 

        if (_readyPlayers.Count >= totalPlayers)
        {
            _isMatchLive = true;
            _liveMatchTargetPlayers = totalPlayers; 
            
            string modeText = totalPlayers == 2 ? "1 v 1 單 挑" : $"{activeT} v {activeCT} 團 戰";

            string hudStartText = $"{Config.HudHtml_Round1_Line1}<br>{Config.HudHtml_Round1_Line2}<br>";
            
            // ✅ 使用專屬的 HudDuration_Round1 黑框顯示秒數
            ShowHud(hudStartText, Config.HudDuration_Round1);

            Server.PrintToChatAll($" {_cachedPrefix} 所 有 玩 家 已 準 備，{modeText} 比 賽 開 始");
            Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Orange}對 戰 開 始！採 贏{ChatColors.Default} {ChatColors.Green}２０{ChatColors.Default} {ChatColors.Orange}回 合 制{ChatColors.Default}。");
            
            _privateCheckTimer?.Kill();
            _privateCheckTimer = null;
            _publicBroadcastTimer?.Kill();
            _publicBroadcastTimer = null;
            _waitingTimer?.Kill();
            _waitingTimer = null;
            
            // ✅ 【關鍵】：這裡改用全新獨立出來的 Live_Execute_Delay 秒數！
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
        var player = @event.Userid;
        if (player == null || !player.IsValid) return HookResult.Continue;
        
        ulong steamId = player.SteamID;

        if (!_isMatchLive && (player.TeamNum == 2 || player.TeamNum == 3))
        {
            if (!_readyPlayers.Contains(steamId) && !_hasReceivedInitialReminder.Contains(steamId))
            {
                _pendingInitialReminders[steamId] = Server.CurrentTime + 5.0f;
                _hasReceivedInitialReminder.Add(steamId);
            }
        }

        if (_isMatchLive && (player.TeamNum == 2 || player.TeamNum == 3))
        {
            if (!_readyPlayers.Contains(steamId))
            {
                Server.NextFrame(() => {
                    if (player.IsValid) {
                        player.ChangeTeam(CsTeam.Spectator);
                        player.PrintToChat($" {_cachedPrefix} {ChatColors.Orange}比 賽 已 開 始，非 參 賽 者 無 法 偷 渡 加 入！已 移 至 觀 戰。");
                    }
                });
                return HookResult.Continue; 
            }
        }
        
        Server.NextFrame(() => {
            Server.NextFrame(() => {
                if (player == null || !player.IsValid || player.PlayerPawn == null || !player.PlayerPawn.IsValid || !player.PawnIsAlive) return;
                
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
        var pawn = player.PlayerPawn.Value;
        if (pawn == null || pawn.WeaponServices == null || pawn.WeaponServices.MyWeapons == null) return;

        foreach (var weaponHandle in pawn.WeaponServices.MyWeapons)
        {
            var weapon = weaponHandle.Value;
            if (weapon != null && weapon.IsValid)
            {
                string wName = weapon.DesignerName;
                if (string.IsNullOrEmpty(wName)) continue;
                if (wName.Contains("knife") || wName.Contains("bayonet") || wName.Contains("c4")) continue;

                weapon.Remove();
                Server.NextFrame(() => {
                    if (player.IsValid && player.PawnIsAlive) {
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
        if (player is null || !player.IsValid) return;
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
                if (p != null && p.IsValid && p.Handle != IntPtr.Zero && !p.IsBot && !p.IsHLTV && (p.TeamNum == 2 || p.TeamNum == 3))
                {
                    ulong steamId = p.SteamID;
                    if (!_readyPlayers.Contains(steamId))
                    {
                        if (!_playerUnreadyTime.ContainsKey(steamId)) _playerUnreadyTime[steamId] = 0;
                        _playerUnreadyTime[steamId] += Config.UnreadyReminderInterval;

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
            if (p != null && p.IsValid && p.Handle != IntPtr.Zero && !p.IsBot && !p.IsHLTV && (p.TeamNum == 2 || p.TeamNum == 3))
            {
                totalPlayers++;
            }
        }

        if (totalPlayers > 0 && totalPlayers == _readyPlayers.Count)
        {
            string modeHint = "";

            if (totalPlayers == 1)
            {
                modeHint = $" [ {ChatColors.Green}動 態 判 斷{ChatColors.White} ] {ChatColors.White}場 上 {ChatColors.Green}1 {ChatColors.White}人，等 待 對 手 加 入...";
            }
            else
            {
                int targetPlayers = GetDynamicRequiredPlayers();
                int teamSize = targetPlayers / 2;
                modeHint = $" [ {ChatColors.Green}動 態 判 斷{ChatColors.White} ] {ChatColors.White}場 上 {ChatColors.Green}{totalPlayers} {ChatColors.White}人，等 對 手 加 入 {ChatColors.Green}{teamSize} v {teamSize} {ChatColors.White}團 戰";
            }

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
                if (p != null && p.IsValid && p.Handle != IntPtr.Zero && !p.IsBot && !p.IsHLTV && (p.TeamNum == 2 || p.TeamNum == 3))
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
                string modeHint = "";

                if (totalPlayers == 2)
                {
                    modeHint = $" [ {ChatColors.Green}動 態 判 斷{ChatColors.White} ] {ChatColors.White}目 前 場 上 {ChatColors.Green}2 {ChatColors.White}人，雙 方 輸 入 {ChatColors.Orange}!R {ChatColors.White}即 可 直 接 {ChatColors.Green}1 v 1 單 挑{ChatColors.White}";
                }
                else if (totalPlayers > 2)
                {
                    modeHint = $" [ {ChatColors.Green}動 態 判 斷{ChatColors.White} ] {ChatColors.White}已觸發團戰，需滿 {ChatColors.Green}{targetPlayers} {ChatColors.White}人輸入 {ChatColors.Orange}!R {ChatColors.White}可開始 {ChatColors.Green}{teamSize} v {teamSize} 團戰{ChatColors.White}";
                }
                
                if (_unreadyNamesCache.Count > 0)
                {
                    Server.PrintToChatAll($" {_cachedPrefix} 尚未準備玩家：{ChatColors.Orange}{string.Join(", ", _unreadyNamesCache)}{ChatColors.Default} | 對戰需滿 {ChatColors.Green}{targetPlayers}{ChatColors.Default} 人");
                }
                
                if (!string.IsNullOrEmpty(modeHint))
                {
                    Server.PrintToChatAll(modeHint); 
                }
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

        _liveTimer?.Kill();
        _liveTimer = null;
        
        _privateCheckTimer?.Kill();
        _privateCheckTimer = AddTimer(Config.UnreadyReminderInterval, CheckAndWarnUnreadyPlayers, TimerFlags.REPEAT);
        
        _publicBroadcastTimer?.Kill();
        _publicBroadcastTimer = AddTimer(Config.PublicUnreadyReminderInterval, BroadcastUnreadyPlayers, TimerFlags.REPEAT);

        _waitingTimer?.Kill();
        _waitingTimer = AddTimer(Config.WaitingForOpponentInterval, BroadcastWaitingMessage, TimerFlags.REPEAT);
    }

    private HookResult OnMatchEnd(EventCsWinPanelMatch @event, GameEventInfo info)
    {
        if (!_isMatchLive) return HookResult.Continue;
        int scoreT = 0, scoreCT = 0;
        var teamManagers = Utilities.FindAllEntitiesByDesignerName<CCSTeam>("cs_team_manager");
        foreach (var team in teamManagers)
        {
            if (team.TeamNum == 2) scoreT = team.Score;
            if (team.TeamNum == 3) scoreCT = team.Score;
        }

        string winnerName = scoreT > scoreCT ? "恐怖份子 (T)" : "反恐小組 (CT)";
        string loserName = scoreT > scoreCT ? "反恐小組 (CT)" : "恐怖份子 (T)";
        
        int winnerScore = Math.Max(scoreT, scoreCT);
        int loserScore = Math.Min(scoreT, scoreCT);
        string scoreString = $"{winnerScore} : {loserScore}";

        Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Lime}{winnerName} {ChatColors.Gold}以 {ChatColors.Green}({scoreString}) {ChatColors.Gold}的分數贏過 {ChatColors.Lime}{loserName}");

        TriggerMapChange(true);
        return HookResult.Continue;
    }
}
