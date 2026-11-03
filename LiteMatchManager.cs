using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Admin;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System;
using System.Collections.Frozen;
using System.Runtime.InteropServices;

namespace LiteMatchManager;

#pragma warning disable CS8618

// =========================================
// 【核心要求 3】：0-GC 類型安全 ConVar<T> 泛型封裝結構
// =========================================
public readonly struct ConVar<T>(CounterStrikeSharp.API.Modules.Cvars.ConVar? inner) where T : unmanaged
{
    public bool IsValid => inner is not null;

    public T Value
    {
        get => inner is not null ? inner.GetPrimitiveValue<T>() : default;
        set => inner?.SetValue(value);
    }

    public static ConVar<T> Find(string name) => new(CounterStrikeSharp.API.Modules.Cvars.ConVar.Find(name));
    public void SetValue(T value) => inner?.SetValue(value);
}

// =========================================
// 階段對戰 (Phase) 專用設定類別
// =========================================
public sealed class DuelModeConfig
{
    [JsonPropertyName("Duel_WinLimit")] public int WinLimit { get; set; } = 5;
    [JsonPropertyName("Duel_DisplayTarget")] public string DisplayTarget { get; set; } = "5";
    [JsonPropertyName("Duel_Name")] public string Name { get; set; } = "手槍";
    [JsonPropertyName("Duel_Health")] public int Health { get; set; } = 100;
    [JsonPropertyName("Duel_Armor")] public int Armor { get; set; } = 1;
    [JsonPropertyName("Duel_PrimaryWeapons")] public List<string> PrimaryWeapons { get; set; } = [];
    [JsonPropertyName("Duel_SecondaryWeapons")] public List<string> SecondaryWeapons { get; set; } = [];
    [JsonPropertyName("Duel_Grenades")] public List<string> Grenades { get; set; } = [];

    [JsonIgnore] public FrozenSet<string> PrimaryWeaponsSet { get; set; } = FrozenSet<string>.Empty;
    [JsonIgnore] public FrozenSet<string> SecondaryWeaponsSet { get; set; } = FrozenSet<string>.Empty;
}

// =========================================
// 主插件對戰設定區塊 (對應 MatchSettings)
// =========================================
public sealed class MatchSettingsConfig
{
    [JsonPropertyName("MaxPlayersPerTeam")] public int MaxPlayersPerTeam { get; set; } = 2;
    [JsonPropertyName("FinalMatchWinScore")] public int FinalMatchWinScore { get; set; } = 30;
    [JsonPropertyName("KickUnreadyPlayerTime")] public int KickUnreadyPlayerTime { get; set; } = 180;
    [JsonPropertyName("ReconnectGracePeriod")] public int ReconnectGracePeriod { get; set; } = 180;
    [JsonPropertyName("UnreadyReminderInterval")] public int UnreadyReminderInterval { get; set; } = 60;
    [JsonPropertyName("PublicUnreadyReminderInterval")] public int PublicUnreadyReminderInterval { get; set; } = 20;
    [JsonPropertyName("WaitingForOpponentInterval")] public int WaitingForOpponentInterval { get; set; } = 30;
    [JsonPropertyName("ChatPrefix")] public string ChatPrefix { get; set; } = "[ {Green}對 戰 系 統{White} ]";
    [JsonPropertyName("EnableChatWeaponCommands")] public bool EnableChatWeaponCommands { get; set; } = true;
    [JsonPropertyName("WarmupConfigName")] public string WarmupConfigName { get; set; } = "warmup.cfg";
    [JsonPropertyName("LiveConfigName")] public string LiveConfigName { get; set; } = "live.cfg";
    [JsonPropertyName("Duel_MapChangeDelay")] public int MapChangeDelay { get; set; } = 7;

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
        " {Orange}你 可 在 聊 天 欄 位 輸 入 您 要 的 武 器，以 下 是 常 用 武 器",
        " ---------------------------------------------------------------------",
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
        new DuelModeConfig { Name = "步槍", WinLimit = 47, DisplayTarget = "３０", Armor = 2, PrimaryWeapons = ["weapon_ak47", "weapon_m4a1", "weapon_galilar", "weapon_m4a1_silencer"], SecondaryWeapons = ["weapon_usp_silencer", "weapon_deagle", "weapon_revolver", "weapon_glock"] }
    ];

    [JsonPropertyName("HudDuration_Prep")] public float HudDuration_Prep { get; set; } = 5.0f;
    [JsonPropertyName("HudDuration_MatchAbort")] public float HudDuration_MatchAbort { get; set; } = 3.0f;
    [JsonPropertyName("HudDuration_MatchStart")] public float HudDuration_MatchStart { get; set; } = 3.0f;
    [JsonPropertyName("RoundStartHudDuration")] public float RoundStartHudDuration { get; set; } = 1.7f;
    [JsonPropertyName("Live_Execute_Delay")] public float Live_Execute_Delay { get; set; } = 2.0f;

    [JsonPropertyName("HudHtml_Prep1v1_Line1")] public string HudHtml_Prep1v1_Line1 { get; set; } = "<b><font class='fontSize-l' color='lime'>✦ </font><font class='fontSize-l' color='white'>人 數 觸 發 </font><font class='fontSize-l' color='gold'>1 v 1</font><font class='fontSize-l' color='white'> 單 挑 </font><font class='fontSize-l' color='lime'>✦</font></b><BR>";
    [JsonPropertyName("HudHtml_Prep1v1_Line2")] public string HudHtml_Prep1v1_Line2 { get; set; } = "<b><font class='fontSize-l' color='white'>已 準 備：</font><font class='fontSize-l' color='lime'>{0} / 2</font><font class='fontSize-l' color='white'> 尚 缺 </font><font class='fontSize-l' color='lime'>{1}</font><font class='fontSize-l' color='white'> 人</font></b>";
    [JsonPropertyName("HudHtml_Prep2v2_Line1")] public string HudHtml_Prep2v2_Line1 { get; set; } = "<b><font class='fontSize-l' color='lime'>✦ </font><font class='fontSize-l' color='white'>人 數 觸 發 </font><font class='fontSize-l' color='gold'>2 v 2</font><font class='fontSize-l' color='white'> 團 戰 </font><font class='fontSize-l' color='lime'>✦</font></b><BR>";
    [JsonPropertyName("HudHtml_Prep2v2_Line2")] public string HudHtml_Prep2v2_Line2 { get; set; } = "<b><font class='fontSize-l' color='white'>已 準 備：</font><font class='fontSize-l' color='lime'>{0} / {2}</font><font class='fontSize-l' color='white'> 尚 缺 </font><font class='fontSize-l' color='lime'>{1}</font><font class='fontSize-l' color='white'> 人</font></b>";

    [JsonPropertyName("HudHtml_MatchAbort_Line1")] public string HudHtml_MatchAbort_Line1 { get; set; } = "<b><font class='fontSize-l' color='gold'>玩 家 逃 跑 ， 戰 鬥 終 止</font></b><BR>";
    [JsonPropertyName("HudHtml_MatchAbort_Line2")] public string HudHtml_MatchAbort_Line2 { get; set; } = "<b><font class='fontSize-l' color='lime'>比 賽 已 退 回 暖 場 模 式</font></b>";

    [JsonPropertyName("HudHtml_Round1_Line1")] public string HudHtml_Round1_Line1 { get; set; } = "<b><font class='fontSize-l' color='gold'>★ {0}戰 鬥 開 始 ★</font></b><BR>";
    [JsonPropertyName("HudHtml_Round1_Line2")] public string HudHtml_Round1_Line2 { get; set; } = "<b><font class='fontSize-l' color='white'>對 戰 採 </font><font class='fontSize-l' color='lime'>３０</font><font class='fontSize-l' color='white'> 回 合 勝 利 制</font></b>";

    [JsonPropertyName("HudHtml_RoundStart_Title")] public string HudHtml_RoundStart_Title { get; set; } = "<b><font class='fontSize-l' color='lime'>{0}回合 </font><font class='fontSize-l' color='gold'>模式 / 搶 </font><font class='fontSize-l' color='Green'>{1}</font><font class='fontSize-l' color='gold'> 勝</font></b><br>";
    [JsonPropertyName("HudHtml_RoundStart_TScore")] public string HudHtml_RoundStart_TScore { get; set; } = "<b><font class='fontSize-l' color='#FF4500'>目 前 恐 怖 份 子：{0}</font></b><br>";
    [JsonPropertyName("HudHtml_RoundStart_CTScore")] public string HudHtml_RoundStart_CTScore { get; set; } = "<b><font class='fontSize-l' color='lightblue'>目 前 反 恐 精 英：{0}</font></b><br><b><font class='fontSize-l' color='gold'>比 賽 贏 </font><font class='fontSize-l' color='Green'>３０</font><font class='fontSize-l' color='gold'> 回 合 為 主</font></b>";
}

// =========================================
// 頂層結構化設定檔類別 (對應 LiteMatchManager.json)
// =========================================
public sealed class LiteMatchConfig : BasePluginConfig
{
    [JsonPropertyName("ConfigVersion")] public override int Version { get; set; } = 1;
    [JsonPropertyName("MatchSettings")] public MatchSettingsConfig MatchSettings { get; set; } = new();
    [JsonPropertyName("EloSettings")] public EloSettingsConfig EloSettings { get; set; } = new();
    [JsonPropertyName("AFKSettings")] public AFKSettingsConfig AFKSettings { get; set; } = new();
    [JsonPropertyName("Localization")] public LocalizationConfig Localization { get; set; } = new();
}

public partial class LiteMatchManager : BasePlugin, IPluginConfig<LiteMatchConfig>
{
    public override string ModuleName => "LiteMatchManager";
    public override string ModuleVersion => "10.0.0_DotNet10_ZeroGC_Merged";
    public override string ModuleAuthor => "Optimized (.NET 10 0-GC)";
    public override string ModuleDescription => "原生30勝換圖 (整合段位ClanTag + 暖身AFK + ConVar<T> + 0-GC架構)";

    public LiteMatchConfig Config { get; set; } = new();

    private string _cachedPrefix = "";
    private readonly List<string> _cachedGunMenuMessage = new(8);

    // 【預解析快取】：消除每次暖身重生時遍歷 SpawnWeapons 的多餘迴圈
    private string _defaultWarmupPri = "";
    private string _defaultWarmupSec = "";
    private int _defaultWarmupArmor = 0;

    private int _currentPhaseIndex = 0;
    private int _phaseStartScoreT = 0;
    private int _phaseStartScoreCT = 0;

    private FrozenSet<string> _readyCommandsSet = FrozenSet<string>.Empty;
    private FrozenSet<string> _gunMenuCommandsSet = FrozenSet<string>.Empty;

    private static readonly FrozenSet<string> PistolWeapons = new[]
    {
        "weapon_usp_silencer", "weapon_glock", "weapon_deagle", "weapon_revolver",
        "weapon_p250", "weapon_tec9", "weapon_fiveseven", "weapon_cz75a",
        "weapon_elite", "weapon_hkp2000"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    private readonly HashSet<ulong> _readyPlayers = new(64);
    private readonly Dictionary<ulong, int> _lockedTeam = new(64);
    private readonly Dictionary<ulong, int> _playerUnreadyTime = new(64);
    private readonly Dictionary<ulong, float> _playerJoinTime = new(64);
    private readonly List<string> _unreadyNamesCache = new(64);
    private readonly Dictionary<ulong, string> _playerPrimary = new(64);
    private readonly Dictionary<ulong, string> _playerSecondary = new(64);
    private readonly Dictionary<ulong, float> _pendingInitialReminders = new(64);
    private readonly List<ulong> _reminderRemoveCache = new(16);
    private readonly HashSet<ulong> _hasReceivedInitialReminder = new(64);

    // 防止玩家快速連按選隊或換槍指令引發瞬間抖動
    private readonly Dictionary<ulong, float> _joinTeamCooldown = new(64);
    private readonly HashSet<uint> _weaponReplacePending = new(64);

    // 【0-GC 實體句柄快取】：取代對槍期間與回合開始的 FindAllEntitiesByDesignerName 全圖掃描
    private readonly List<CHandle<CEntityInstance>> _trackedMapEquips = new(16);
    private readonly List<CHandle<CEntityInstance>> _pendingRagdolls = new(16);
    private bool _ragdollTimerScheduled = false;

    private readonly List<CCSPlayerController> _serverPlayersCache = new(64);

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

    // 【核心要求 3】：全面改用新型 ConVar<T> 進行類型安全存取
    private ConVar<int> _cvWarmupPauseTimer;
    private ConVar<bool> _cvAllTalk;
    private ConVar<bool> _cvFullAllTalk;
    private ConVar<bool> _cvDeadTalk;

    private string _activeCenterMessage = "";
    private float _centerMessageExpiration = 0f;

    // 支援不分大小寫的色彩標籤解析 (相容 zh-Hant.json 與主設定檔)
    private static string ReplaceColorTags(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        return input
            .Replace("{Default}", ChatColors.Default.ToString(), StringComparison.OrdinalIgnoreCase)
            .Replace("{White}", ChatColors.White.ToString(), StringComparison.OrdinalIgnoreCase)
            .Replace("{Red}", ChatColors.Red.ToString(), StringComparison.OrdinalIgnoreCase)
            .Replace("{DarkRed}", ChatColors.DarkRed.ToString(), StringComparison.OrdinalIgnoreCase)
            .Replace("{Green}", ChatColors.Green.ToString(), StringComparison.OrdinalIgnoreCase)
            .Replace("{Lime}", ChatColors.Lime.ToString(), StringComparison.OrdinalIgnoreCase)
            .Replace("{LightBlue}", ChatColors.LightBlue.ToString(), StringComparison.OrdinalIgnoreCase)
            .Replace("{Yellow}", ChatColors.Yellow.ToString(), StringComparison.OrdinalIgnoreCase)
            .Replace("{Gold}", ChatColors.Gold.ToString(), StringComparison.OrdinalIgnoreCase)
            .Replace("{Orange}", ChatColors.Orange.ToString(), StringComparison.OrdinalIgnoreCase)
            .Replace("{Grey}", ChatColors.Grey.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    public void OnConfigParsed(LiteMatchConfig config)
    {
        var matchCfg = config.MatchSettings;

        var caseInsensitiveDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in matchCfg.WeaponCommands) caseInsensitiveDict[key] = value;
        matchCfg.WeaponCommands = caseInsensitiveDict;

        _readyCommandsSet = matchCfg.ReadyCommands.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
        _gunMenuCommandsSet = matchCfg.GunMenuCommands.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
        _ = _readyCommandsSet.Contains("");
        _ = _gunMenuCommandsSet.Contains("");
        _ = PistolWeapons.Contains("");
        _ = matchCfg.WeaponCommands.TryGetValue("", out _);

        if (matchCfg.MatchModes != null)
        {
            for (int i = 0; i < matchCfg.MatchModes.Count; i++)
            {
                var mode = matchCfg.MatchModes[i];
                mode.PrimaryWeaponsSet = (mode.PrimaryWeapons ?? []).ToFrozenSet(StringComparer.OrdinalIgnoreCase);
                mode.SecondaryWeaponsSet = (mode.SecondaryWeapons ?? []).ToFrozenSet(StringComparer.OrdinalIgnoreCase);
                _ = mode.PrimaryWeaponsSet.Contains("");
                _ = mode.SecondaryWeaponsSet.Contains("");
            }
        }

        // 預先解析暖身預設武器與護甲，使 OnPlayerSpawn 暖身發槍達成 0 迴圈
        _defaultWarmupPri = "";
        _defaultWarmupSec = "";
        _defaultWarmupArmor = 0;

        if (matchCfg.SpawnWeapons != null)
        {
            for (int i = 0; i < matchCfg.SpawnWeapons.Count; i++)
            {
                string item = matchCfg.SpawnWeapons[i];
                if (item.Equals("item_assaultsuit", StringComparison.OrdinalIgnoreCase))
                {
                    _defaultWarmupArmor = 2;
                }
                else if (item.Equals("item_kevlar", StringComparison.OrdinalIgnoreCase))
                {
                    if (_defaultWarmupArmor < 2) _defaultWarmupArmor = 1;
                }
                else if (item.Contains("knife", StringComparison.OrdinalIgnoreCase) || item.Contains("bayonet", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                else if (PistolWeapons.Contains(item))
                {
                    _defaultWarmupSec = item;
                }
                else if (item.StartsWith("weapon_", StringComparison.OrdinalIgnoreCase))
                {
                    _defaultWarmupPri = item;
                }
            }
        }

        Config = config;
        _cachedPrefix = ReplaceColorTags(matchCfg.ChatPrefix);

        _cachedGunMenuMessage.Clear();
        if (matchCfg.GunMenuMessage != null)
        {
            for (int i = 0; i < matchCfg.GunMenuMessage.Count; i++)
            {
                _cachedGunMenuMessage.Add(ReplaceColorTags(matchCfg.GunMenuMessage[i]));
            }
        }

        // 同步初始化 AFK 模組與 Elo 模組開關
        OnAfkConfigParsed(config);
        OnEloConfigParsed(config);
    }

    private void CacheConVars()
    {
        _cvWarmupPauseTimer = ConVar<int>.Find("mp_warmup_pausetimer");
        _cvAllTalk          = ConVar<bool>.Find("sv_alltalk");
        _cvFullAllTalk      = ConVar<bool>.Find("sv_full_alltalk");
        _cvDeadTalk         = ConVar<bool>.Find("sv_deadtalk");
    }

    // 僅在換圖時掃描一次既有 game_player_equip，後續皆由 OnEntitySpawned 即時攔截，回合開始 0 迴圈
    private void PurgeMapWeaponEntities()
    {
        for (int i = _trackedMapEquips.Count - 1; i >= 0; i--)
        {
            var handle = _trackedMapEquips[i];
            if (handle.IsValid && handle.Value is { IsValid: true } ent)
            {
                ent.Remove();
            }
        }
        _trackedMapEquips.Clear();
    }

    private void ScanAndPurgeMapEquipsOnce()
    {
        _trackedMapEquips.Clear();
        foreach (var equip in Utilities.FindAllEntitiesByDesignerName<CEntityInstance>("game_player_equip"))
        {
            if (equip is { IsValid: true })
            {
                equip.Remove();
            }
        }
    }

    private void FlushPendingRagdolls()
    {
        _ragdollTimerScheduled = false;
        if (_isServerShuttingDown)
        {
            _pendingRagdolls.Clear();
            return;
        }

        for (int i = 0; i < _pendingRagdolls.Count; i++)
        {
            var handle = _pendingRagdolls[i];
            if (handle.IsValid && handle.Value is { IsValid: true } ragdoll)
            {
                ragdoll.Remove();
            }
        }
        _pendingRagdolls.Clear();
    }

    // 【核心要求 3】：改用 ConVar<bool> 直接設定記憶體值，不產生字串命令解析開銷
    private void ApplyVoiceMode(bool enableAllTalk)
    {
        if (!_cvAllTalk.IsValid) CacheConVars();

        if (_cvAllTalk.IsValid && _cvFullAllTalk.IsValid && _cvDeadTalk.IsValid)
        {
            _cvAllTalk.SetValue(enableAllTalk);
            _cvFullAllTalk.SetValue(enableAllTalk);
            _cvDeadTalk.SetValue(true);
        }
        else
        {
            Server.ExecuteCommand(enableAllTalk
                ? "sv_alltalk 1; sv_full_alltalk 1; sv_deadtalk 1"
                : "sv_alltalk 0; sv_full_alltalk 0; sv_deadtalk 1");
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
        int targetWinScore = Config.MatchSettings.FinalMatchWinScore;

        return scoreT >= targetWinScore || scoreCT >= targetWinScore;
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
            ReadOnlySpan<CCSPlayerController> players = CollectionsMarshal.AsSpan(_serverPlayersCache);
            for (int i = 0; i < players.Length; i++)
            {
                var p = players[i];
                if (p is { IsValid: true, TeamNum: 2 or 3 }) p.PrintToCenterHtml("&#8203;", 0);
            }
        }
    }

    private void OnTick()
    {
        if (!string.IsNullOrEmpty(_activeCenterMessage))
        {
            ReadOnlySpan<CCSPlayerController> players = CollectionsMarshal.AsSpan(_serverPlayersCache);
            if (Server.CurrentTime <= _centerMessageExpiration)
            {
                for (int i = 0; i < players.Length; i++)
                {
                    var p = players[i];
                    if (p is { IsValid: true, TeamNum: 2 or 3 }) p.PrintToCenterHtml(_activeCenterMessage);
                }
            }
            else
            {
                _activeCenterMessage = "";
                for (int i = 0; i < players.Length; i++)
                {
                    var p = players[i];
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

        ReadOnlySpan<CCSPlayerController> players = CollectionsMarshal.AsSpan(_serverPlayersCache);
        for (int i = 0; i < players.Length; i++)
        {
            var p = players[i];
            if (p is { IsValid: true, SteamID: > 0, TeamNum: 2 or 3 })
            {
                ulong steamId = p.SteamID;
                if (_pendingInitialReminders.TryGetValue(steamId, out float triggerTime) && currentTime >= triggerTime)
                {
                    _reminderRemoveCache.Add(steamId);

                    if (!_isMatchLive && !_readyPlayers.Contains(steamId))
                    {
                        _playerUnreadyTime.TryGetValue(steamId, out int elapsed);
                        int timeLeft = Config.MatchSettings.KickUnreadyPlayerTime - elapsed;
                        p.PrintToChat($" {_cachedPrefix} 請輸入 {ChatColors.Lime}!R{ChatColors.White} 準備 ，{ChatColors.Lime}{timeLeft}{ChatColors.White} 秒未準備將被踢出");
                        p.PrintToCenter($"請輸入 !R 準備，{timeLeft} 秒後將被踢出");
                    }
                }
            }
        }

        foreach (var (key, value) in _pendingInitialReminders)
        {
            if (currentTime >= value && !_reminderRemoveCache.Contains(key))
            {
                _reminderRemoveCache.Add(key);
            }
        }

        for (int i = 0; i < _reminderRemoveCache.Count; i++)
        {
            _pendingInitialReminders.Remove(_reminderRemoveCache[i]);
        }
        _reminderRemoveCache.Clear();
    }

    public override void Load(bool hotReload)
    {
        Console.WriteLine("=================================================");
        Console.WriteLine("  LiteMatchManager (.NET 10 0-GC 整合版) 啟動！");
        Console.WriteLine("=================================================");

        _isServerShuttingDown = false;
        CacheConVars();

        InitEloClanTagSystem();
        InitAfkManager(hotReload);

        AddCommandListener("say", OnPlayerSay);
        AddCommandListener("say_team", OnPlayerSay);
        AddCommandListener("jointeam", OnJoinTeam);
        AddCommandListener("drop", (player, info) => HookResult.Handled);

        RegisterListener<Listeners.OnTick>(OnTick);
        AddTimer(1.0f, CheckPendingReminders, TimerFlags.REPEAT);

        // 【核心優化】：以 OnEntitySpawned 取代回合中與死亡後的 FindAllEntitiesByDesignerName 全圖迴圈掃描
        RegisterListener<Listeners.OnEntitySpawned>(entity =>
        {
            if (_isServerShuttingDown || entity is not { IsValid: true }) return;

            string dName = entity.DesignerName;
            if (string.Equals(dName, "game_player_equip", StringComparison.Ordinal))
            {
                var handle = new CHandle<CEntityInstance>(entity.Handle);
                _trackedMapEquips.Add(handle);
                Server.NextFrame(PurgeMapWeaponEntities);
            }
            else if (string.Equals(dName, "cs_ragdoll", StringComparison.Ordinal))
            {
                _pendingRagdolls.Add(new CHandle<CEntityInstance>(entity.Handle));
                if (!_ragdollTimerScheduled)
                {
                    _ragdollTimerScheduled = true;
                    AddTimer(0.5f, FlushPendingRagdolls);
                }
            }
        });

        RegisterEventHandler<EventMapShutdown>((@event, info) =>
        {
            _isServerShuttingDown = true;
            _trackedMapEquips.Clear();
            _pendingRagdolls.Clear();
            _ragdollTimerScheduled = false;
            return HookResult.Continue;
        });

        RegisterEventHandler<EventPlayerConnectFull>((@event, info) =>
        {
            var player = @event.Userid;
            Server.NextFrame(() =>
            {
                RefreshActivePlayers();
                FetchPlayerClanTagOnConnect(player);
            });
            return HookResult.Continue;
        });

        RegisterEventHandler<EventPlayerDisconnect>((@event, info) =>
        {
            Server.NextFrame(RefreshActivePlayers);

            if (@event.Userid is { SteamID: > 0 } player)
            {
                ulong steamId = player.SteamID;
                string pName = player.PlayerName;

                RemovePlayerClanTagCache(steamId);
                _joinTeamCooldown.Remove(steamId);

                if (_isMatchLive && _readyPlayers.Contains(steamId))
                {
                    _readyPlayers.Remove(steamId);
                    _lockedTeam.Remove(steamId);

                    if (IsMatchOver()) return HookResult.Continue;

                    if (_liveMatchTargetPlayers == 2 || _isCountdownActive)
                    {
                        Server.PrintToChatAll($" {_cachedPrefix} 玩 家 {ChatColors.Gold}{pName} {ChatColors.White}斷 線，比 賽 強 制 終 止");
                        Server.NextFrame(AbortMatch);
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

                Server.NextFrame(() => ApplyPlayerRankClanTag(player));

                if (newTeam is 0 or 1)
                {
                    player.PrintToCenterHtml("&#8203;", 0);

                    _pendingInitialReminders.Remove(steamId);
                    _hasReceivedInitialReminder.Remove(steamId);
                    _playerJoinTime.Remove(steamId);

                    if (_isMatchLive && _readyPlayers.Contains(steamId))
                    {
                        _readyPlayers.Remove(steamId);
                        _lockedTeam.Remove(steamId);

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
                        if (_readyPlayers.Contains(steamId))
                        {
                            if (_lockedTeam.TryGetValue(steamId, out int lockedTeam) && newTeam != lockedTeam)
                            {
                                Server.NextFrame(() =>
                                {
                                    if (player is { IsValid: true } && player.TeamNum != lockedTeam)
                                    {
                                        player.ChangeTeam((CsTeam)lockedTeam);
                                        player.PrintToChat($" {_cachedPrefix} {ChatColors.Orange}對 戰 進 行 中，無 法 切 換 隊 伍");
                                    }
                                });
                                return HookResult.Continue;
                            }
                        }
                        else
                        {
                            if (_liveMatchTargetPlayers == 2)
                            {
                                Server.NextFrame(() =>
                                {
                                    if (player is { IsValid: true } && player.TeamNum != (byte)CsTeam.Spectator)
                                    {
                                        player.ChangeTeam(CsTeam.Spectator);
                                        player.PrintToChat($" {_cachedPrefix} {ChatColors.Orange}單 挑 比 賽 進 行 中，不 開 放 加 入");
                                    }
                                });
                            }
                            else
                            {
                                int liveTeamMax = _liveMatchTargetPlayers / 2;
                                int currentCount = CountPlayersInTeam(newTeam, steamId);

                                if (currentCount >= liveTeamMax)
                                {
                                    Server.NextFrame(() =>
                                    {
                                        if (player is { IsValid: true } && player.TeamNum != (byte)CsTeam.Spectator)
                                        {
                                            player.ChangeTeam(CsTeam.Spectator);
                                            player.PrintToChat($" {_cachedPrefix} {ChatColors.Orange}該 隊 伍 補 位 已 滿，無 法 加 入");
                                        }
                                    });
                                }
                                else
                                {
                                    _readyPlayers.Add(steamId);
                                    _lockedTeam[steamId] = newTeam;
                                    Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Gold}玩 家 {player.PlayerName} {ChatColors.White}成 功 補 位 加 入 團 戰 比 賽");
                                }
                            }
                        }
                        Server.NextFrame(CheckPhaseWin);
                    }
                    else
                    {
                        int maxPerTeam = Config.MatchSettings.MaxPlayersPerTeam;
                        int currentTeamCount = CountPlayersInTeam(newTeam, steamId);

                        if (currentTeamCount >= maxPerTeam)
                        {
                            Server.NextFrame(() =>
                            {
                                if (player is { IsValid: true } && player.TeamNum != (byte)CsTeam.Spectator)
                                {
                                    player.ChangeTeam(CsTeam.Spectator);
                                    string teamName = newTeam == 2 ? "恐怖份子 (T)" : "反恐小組 (CT)";
                                    player.PrintToChat($" {_cachedPrefix} {ChatColors.Orange}加 入 失 敗！{teamName} 已 經 滿 員 ( 最 多 {ChatColors.Green}{maxPerTeam}{ChatColors.Orange} 人 )");
                                }
                            });
                        }
                        Server.NextFrame(CheckMatchStart);
                    }
                }
            }
            return HookResult.Continue;
        }, HookMode.Post);

        RegisterEventHandler<EventRoundPrestart>((@event, info) =>
        {
            if (_trackedMapEquips.Count > 0) PurgeMapWeaponEntities();
            return HookResult.Continue;
        }, HookMode.Pre);

        RegisterEventHandler<EventPlayerDeath>((@event, info) =>
        {
            if (Config.EloSettings.EloSystemEnabled)
            {
                RecordEloKill(@event.Attacker, @event.Userid, @event.Headshot);
            }
            return HookResult.Continue;
        });

        RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
        RegisterEventHandler<EventRoundStart>(OnEventRoundStart);
        RegisterEventHandler<EventCsWinPanelMatch>(OnMatchEnd);

        RegisterEventHandler<EventRoundEnd>((@event, info) =>
        {
            if (_isMatchLive && !_isCountdownActive) Server.NextFrame(CheckPhaseWin);
            return HookResult.Continue;
        });

        RegisterListener<Listeners.OnMapStart>(mapName =>
        {
            _isServerShuttingDown = false;
            _cachedTeamT = null;
            _cachedTeamCT = null;
            _trackedMapEquips.Clear();
            _pendingRagdolls.Clear();
            _ragdollTimerScheduled = false;
            CacheConVars();

            ResetMatchState();
            OnAfkMapStart();

            Server.NextFrame(() =>
            {
                ScanAndPurgeMapEquipsOnce();
                Server.ExecuteCommand($"exec {Config.MatchSettings.WarmupConfigName}");
            });

            Server.NextFrame(() =>
            {
                RefreshActivePlayers();
                if (Config.EloSettings.EloSystemEnabled)
                {
                    ReadOnlySpan<CCSPlayerController> players = CollectionsMarshal.AsSpan(_serverPlayersCache);
                    for (int i = 0; i < players.Length; i++) FetchPlayerClanTagOnConnect(players[i]);
                }
            });
        });

        Server.NextFrame(() =>
        {
            ScanAndPurgeMapEquipsOnce();
            RefreshActivePlayers();
            if (Config.EloSettings.EloSystemEnabled)
            {
                ReadOnlySpan<CCSPlayerController> players = CollectionsMarshal.AsSpan(_serverPlayersCache);
                for (int i = 0; i < players.Length; i++) FetchPlayerClanTagOnConnect(players[i]);
            }
        });
    }

    private int CountPlayersInTeam(int targetTeam, ulong excludeSteamId = 0)
    {
        int count = 0;
        ReadOnlySpan<CCSPlayerController> players = CollectionsMarshal.AsSpan(_serverPlayersCache);
        for (int i = 0; i < players.Length; i++)
        {
            var p = players[i];
            if (p is { IsValid: true, Handle: not 0, IsBot: false, TeamNum: var team } && team == targetTeam && p.SteamID != excludeSteamId)
            {
                count++;
            }
        }
        return count;
    }

    private (int CountT, int CountCT) CountActiveCombatPlayers(ulong excludeSteamId = 0)
    {
        int activeT = 0, activeCT = 0;
        ReadOnlySpan<CCSPlayerController> players = CollectionsMarshal.AsSpan(_serverPlayersCache);
        for (int i = 0; i < players.Length; i++)
        {
            var p = players[i];
            if (p is { IsValid: true, Handle: not 0, IsBot: false, IsHLTV: false } && p.SteamID != excludeSteamId)
            {
                if (p.TeamNum == 2) activeT++;
                else if (p.TeamNum == 3) activeCT++;
            }
        }
        return (activeT, activeCT);
    }

    private int GetDynamicRequiredPlayers()
    {
        var (activeT, activeCT) = CountActiveCombatPlayers();
        int total = activeT + activeCT;
        if (total <= 2) return 2;

        int target = (total % 2 == 1) ? total + 1 : total;
        int absoluteMax = Config.MatchSettings.MaxPlayersPerTeam * 2;
        return target > absoluteMax ? absoluteMax : target;
    }

    private void CheckPhaseWin()
    {
        if (_isServerShuttingDown || !_isMatchLive || _isCountdownActive || IsMatchOver()) return;

        var (activeT, activeCT) = CountActiveCombatPlayers();
        if (activeT == 0 || activeCT == 0) { AbortMatch(); return; }

        var modes = Config.MatchSettings.MatchModes;
        if (modes.Count == 0 || _currentPhaseIndex >= modes.Count) return;

        EnsureTeamEntitiesCached();

        int scoreT = _cachedTeamT?.Score ?? 0;
        int scoreCT = _cachedTeamCT?.Score ?? 0;

        int currentPhaseScoreT = scoreT - _phaseStartScoreT;
        int currentPhaseScoreCT = scoreCT - _phaseStartScoreCT;

        var currentPhase = modes[_currentPhaseIndex];

        if (currentPhaseScoreT >= currentPhase.WinLimit || currentPhaseScoreCT >= currentPhase.WinLimit)
        {
            _currentPhaseIndex++;

            if (_currentPhaseIndex < modes.Count)
            {
                _phaseStartScoreT = scoreT;
                _phaseStartScoreCT = scoreCT;

                var nextPhase = modes[_currentPhaseIndex];
                Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Lime}階 段 結 束！{ChatColors.Gold}進 入 更 換【 {ChatColors.Green}{nextPhase.Name}{ChatColors.Gold} 】模 式");
            }
        }
    }

    private HookResult OnMatchEnd(EventCsWinPanelMatch @event, GameEventInfo info)
    {
        if (!_isMatchLive || _isChangingMap) return HookResult.Continue;

        Server.NextFrame(() => ApplyVoiceMode(enableAllTalk: true));

        EnsureTeamEntitiesCached();
        int scoreT = _cachedTeamT?.Score ?? 0;
        int scoreCT = _cachedTeamCT?.Score ?? 0;

        string winnerName = scoreT > scoreCT ? "恐怖份子" : "反恐小組";
        string loserName = scoreT > scoreCT ? "反恐小組" : "恐怖份子";

        CCSPlayerController? playerT = null;
        CCSPlayerController? playerCT = null;

        if (_liveMatchTargetPlayers == 2)
        {
            string nameT = "恐怖份子";
            string nameCT = "反恐小組";
            ReadOnlySpan<CCSPlayerController> players = CollectionsMarshal.AsSpan(_serverPlayersCache);
            for (int i = 0; i < players.Length; i++)
            {
                var p = players[i];
                if (p is { IsValid: true, Handle: not 0, IsBot: false, TeamNum: 2 }) { nameT = p.PlayerName; playerT = p; }
                else if (p is { IsValid: true, Handle: not 0, IsBot: false, TeamNum: 3 }) { nameCT = p.PlayerName; playerCT = p; }
            }
            winnerName = scoreT > scoreCT ? nameT : nameCT;
            loserName = scoreT > scoreCT ? nameCT : nameT;
        }

        int winnerScore = Math.Max(scoreT, scoreCT);
        int loserScore = Math.Min(scoreT, scoreCT);

        Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Lime}{winnerName} {ChatColors.Gold}以 {ChatColors.Green} {winnerScore} : {loserScore} {ChatColors.Gold}贏得了最終勝利");

        if (Config.EloSettings.EloSystemEnabled)
        {
            TrySubmitEloMatchResult(scoreT, scoreCT, winnerScore, loserScore, winnerName, loserName, playerT, playerCT);
        }

        TriggerMapChange();
        return HookResult.Continue;
    }

    private void AbortMatch()
    {
        if (!_isMatchLive || IsMatchOver()) return;

        _liveTimer?.Kill(); _liveTimer = null;

        ResetMatchState();
        StartAfkManagerWarmup();

        var matchCfg = Config.MatchSettings;
        ShowHud($"{matchCfg.HudHtml_MatchAbort_Line1}<br>{matchCfg.HudHtml_MatchAbort_Line2}<br>", matchCfg.HudDuration_MatchAbort);
        Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Lime}玩 家 {ChatColors.Orange}離 退 對 戰 終 止，請 重 新 輸 入 {ChatColors.Lime}!R {ChatColors.Orange}對 戰");

        AddTimer(matchCfg.HudDuration_MatchAbort, () =>
        {
            Server.ExecuteCommand("mp_warmup_start");

            if (!_cvWarmupPauseTimer.IsValid) _cvWarmupPauseTimer = ConVar<int>.Find("mp_warmup_pausetimer");
            if (_cvWarmupPauseTimer.IsValid) _cvWarmupPauseTimer.SetValue(1);
            else Server.ExecuteCommand("mp_warmup_pausetimer 1");

            Server.NextFrame(() =>
            {
                Server.ExecuteCommand($"exec {Config.MatchSettings.WarmupConfigName}");
                ApplyVoiceMode(enableAllTalk: true);
            });
        });
    }

    // 【核心防抖動重構】：
    // 1. 補齊熱身賽與正賽的 targetTeam == 0 (自動選隊) 滿員攔截，防止先生成角色再處死踢回觀戰
    // 2. 僅在玩家不在觀戰區 (TeamNum != 1) 時才於 NextFrame 移至觀戰區，避免在指令 Hook 中直接同步呼叫 ChangeTeam 引發連環狀態刷新抖動
    private HookResult OnJoinTeam(CCSPlayerController? player, CommandInfo info)
    {
        if (player is not { IsValid: true, Handle: not 0 } || player.IsBot) return HookResult.Continue;

        ReadOnlySpan<char> argSpan = info.GetArg(1).AsSpan().Trim();
        if (!int.TryParse(argSpan, out int targetTeam)) return HookResult.Continue;

        if (targetTeam == 1) return HookResult.Continue;

        ulong steamId = player.SteamID;
        float now = Server.CurrentTime;

        if (_joinTeamCooldown.TryGetValue(steamId, out float nextAllowed) && now < nextAllowed)
        {
            return HookResult.Stop;
        }

        if (targetTeam is 0 or 2 or 3)
        {
            if (_isMatchLive)
            {
                if (_readyPlayers.Contains(steamId))
                {
                    if (_lockedTeam.TryGetValue(steamId, out int lockedTeam) && targetTeam != lockedTeam)
                    {
                        _joinTeamCooldown[steamId] = now + 1.0f;
                        player.PrintToChat($" {_cachedPrefix} {ChatColors.Orange}對 戰 進 行 中，無 法 切 換 隊 伍");
                        return HookResult.Stop;
                    }
                    return HookResult.Continue;
                }
                else
                {
                    if (_liveMatchTargetPlayers == 2 || _isCountdownActive)
                    {
                        _joinTeamCooldown[steamId] = now + 1.0f;
                        MoveToSpectatorIfNeeded(player);
                        player.PrintToChat($" {_cachedPrefix} {ChatColors.Orange}單 挑 比 賽 進 行 中，不 開 放 加 入");
                        return HookResult.Stop;
                    }
                    else
                    {
                        int liveTeamMax = _liveMatchTargetPlayers / 2;
                        var (countT, countCT) = CountActiveCombatPlayers(steamId);

                        if (targetTeam == 0)
                        {
                            if (countT >= liveTeamMax && countCT >= liveTeamMax)
                            {
                                _joinTeamCooldown[steamId] = now + 1.0f;
                                MoveToSpectatorIfNeeded(player);
                                player.PrintToChat($" {_cachedPrefix} {ChatColors.Orange}該 隊 伍 補 位 已 滿，無 法 加 入");
                                return HookResult.Stop;
                            }
                            return HookResult.Continue;
                        }

                        int currentCount = targetTeam == 2 ? countT : countCT;
                        if (currentCount >= liveTeamMax)
                        {
                            _joinTeamCooldown[steamId] = now + 1.0f;
                            MoveToSpectatorIfNeeded(player);
                            player.PrintToChat($" {_cachedPrefix} {ChatColors.Orange}該 隊 伍 補 位 已 滿，無 法 加 入");
                            return HookResult.Stop;
                        }
                    }
                }
            }
            else
            {
                int maxPerTeam = Config.MatchSettings.MaxPlayersPerTeam;
                var (countT, countCT) = CountActiveCombatPlayers(steamId);

                if (targetTeam == 0)
                {
                    if (countT >= maxPerTeam && countCT >= maxPerTeam)
                    {
                        _joinTeamCooldown[steamId] = now + 1.0f;
                        MoveToSpectatorIfNeeded(player);
                        player.PrintToChat($" {_cachedPrefix} {ChatColors.Orange}加 入 失 敗！雙 方 隊 伍 皆 已 滿 員 ( 最 多 {ChatColors.Green}{maxPerTeam}{ChatColors.Orange} 人 )");
                        return HookResult.Stop;
                    }
                    return HookResult.Continue;
                }

                int currentTeamCount = targetTeam == 2 ? countT : countCT;
                if (currentTeamCount >= maxPerTeam)
                {
                    _joinTeamCooldown[steamId] = now + 1.0f;
                    MoveToSpectatorIfNeeded(player);
                    string teamName = targetTeam == 2 ? "恐怖份子 (T)" : "反恐小組 (CT)";
                    player.PrintToChat($" {_cachedPrefix} {ChatColors.Orange}加 入 失 敗！{teamName} 已 經 滿 員 ( 最 多 {ChatColors.Green}{maxPerTeam}{ChatColors.Orange} 人 )");
                    return HookResult.Stop;
                }
            }
        }

        return HookResult.Continue;
    }

    private static void MoveToSpectatorIfNeeded(CCSPlayerController player)
    {
        if (player.TeamNum != (byte)CsTeam.Spectator)
        {
            Server.NextFrame(() =>
            {
                if (player is { IsValid: true } && player.TeamNum != (byte)CsTeam.Spectator)
                {
                    player.ChangeTeam(CsTeam.Spectator);
                }
            });
        }
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
            string message = rawArg.AsSpan().Trim(" \"").ToString();
            if (string.IsNullOrWhiteSpace(message)) return HookResult.Continue;
            string playerName = player.PlayerName ?? "";
            string nameColor = player.TeamNum switch { 1 => $"{ChatColors.Grey}", 2 => "\x10", 3 => "\x0B", _ => $"{ChatColors.White}" };
            string formattedMessage = $" {ChatColors.White}[所有人]{ChatColors.White} {nameColor}{playerName}{ChatColors.White}：{message}";

            ReadOnlySpan<CCSPlayerController> players = CollectionsMarshal.AsSpan(_serverPlayersCache);
            for (int i = 0; i < players.Length; i++)
            {
                var p = players[i];
                if (p is { IsValid: true, IsBot: false }) p.PrintToChat(formattedMessage);
            }
            return HookResult.Handled;
        }

        string command = rawArg.AsSpan().Trim(" \"!/").ToString();

        if (string.Equals(command, "nextmap", StringComparison.OrdinalIgnoreCase) && AdminManager.PlayerHasPermissions(player, "@css/root"))
        {
            TriggerMapChange();
            return HookResult.Handled;
        }

        if (Config.EloSettings.EloSystemEnabled)
        {
            if (string.Equals(command, "rank", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(command, "elo", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(command, "等級", StringComparison.OrdinalIgnoreCase))
            {
                TriggerPlayerRankQuery(player);
                return HookResult.Continue;
            }

            if (string.Equals(command, "top", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(command, "排行", StringComparison.OrdinalIgnoreCase))
            {
                TriggerTopRankQuery(player);
                return HookResult.Continue;
            }
        }

        if (_readyCommandsSet.Contains(command))
        {
            if (!_isMatchLive)
            {
                Server.NextFrame(() =>
                {
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

        if (Config.MatchSettings.EnableChatWeaponCommands)
        {
            if (_gunMenuCommandsSet.Contains(command))
            {
                OnGsCommand(player);
                return HookResult.Continue;
            }
            if (Config.MatchSettings.WeaponCommands.TryGetValue(command, out string? realWeaponName))
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

        var modes = Config.MatchSettings.MatchModes;
        if (!_isMatchLive || _isCountdownActive || modes.Count == 0)
        {
            if (PistolWeapons.Contains(weaponName)) _playerSecondary[player.SteamID] = weaponName;
            else _playerPrimary[player.SteamID] = weaponName;

            ReplaceWeapon(player, weaponName);
            return;
        }

        var phase = modes[_currentPhaseIndex];

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
        var mapList = Config.MatchSettings.MapList;
        if (_isChangingMap || mapList is null || mapList.Count == 0) return;
        _isChangingMap = true;

        ReadOnlySpan<char> selectedMapSpan = mapList[Random.Shared.Next(mapList.Count)].AsSpan();
        int colonIndex = selectedMapSpan.IndexOf(':');
        string mapName = colonIndex == -1 ? selectedMapSpan.ToString() : selectedMapSpan[..colonIndex].ToString();
        string workshopId = colonIndex == -1 ? "" : selectedMapSpan[(colonIndex + 1)..].ToString();

        Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Lime}5 秒{ChatColors.White} {ChatColors.Gold}後 自動載入下一張地圖：{ChatColors.Lime}{mapName} ...");

        AddTimer(Config.MatchSettings.MapChangeDelay, () =>
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

        var matchCfg = Config.MatchSettings;
        int targetPlayers = GetDynamicRequiredPlayers();
        int missingPlayers = targetPlayers - _readyPlayers.Count;

        Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Gold}{player.PlayerName}{ChatColors.White} 已 準 備！準 備 進 度：{ChatColors.Green}{_readyPlayers.Count} / {targetPlayers}");

        if (targetPlayers > 2)
        {
            string prepString = $"{matchCfg.HudHtml_Prep2v2_Line1}<br>{string.Format(matchCfg.HudHtml_Prep2v2_Line2, _readyPlayers.Count, missingPlayers, targetPlayers)}<br>";
            ShowHud(prepString, matchCfg.HudDuration_Prep);
        }

        CheckMatchStart();

        if (!_isMatchLive)
        {
            var (activeT, activeCT) = CountActiveCombatPlayers();
            int activePlayers = activeT + activeCT;
            if (activePlayers > 0 && activePlayers == _readyPlayers.Count)
            {
                BroadcastWaitingMessage();
                _waitingTimer?.Kill();
                _waitingTimer = AddTimer(matchCfg.WaitingForOpponentInterval, BroadcastWaitingMessage, TimerFlags.REPEAT);
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

            var matchCfg = Config.MatchSettings;
            int targetPlayers = GetDynamicRequiredPlayers();
            int missingPlayers = targetPlayers - _readyPlayers.Count;

            Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Gold}{player.PlayerName}{ChatColors.White} 取 消 了 準 備！準 備 進 度：{ChatColors.Green}{_readyPlayers.Count} / {targetPlayers}");

            if (targetPlayers > 2)
            {
                string prepString = $"{matchCfg.HudHtml_Prep2v2_Line1}<br>{string.Format(matchCfg.HudHtml_Prep2v2_Line2, _readyPlayers.Count, missingPlayers, targetPlayers)}<br>";
                ShowHud(prepString, matchCfg.HudDuration_Prep);
            }
        }
    }

    private void CheckMatchStart()
    {
        if (_isMatchLive) return;

        var matchCfg = Config.MatchSettings;
        var (activeT, activeCT) = CountActiveCombatPlayers();
        int totalPlayers = activeT + activeCT;

        if (totalPlayers < 2 || activeT != activeCT || activeT > matchCfg.MaxPlayersPerTeam) return;

        if (_readyPlayers.Count >= totalPlayers)
        {
            _isMatchLive = true;
            _isCountdownActive = true;
            _currentPhaseIndex = 0;
            _liveMatchTargetPlayers = totalPlayers;

            // 【核心要求】：一旦進入正賽，立刻停止 AFK 掛機檢測計時器
            StopAfkManagerForLiveMatch();

            _phaseStartScoreT = 0;
            _phaseStartScoreCT = 0;

            _playerPrimary.Clear();
            _playerSecondary.Clear();
            _lockedTeam.Clear();
            _pendingInitialReminders.Clear();
            ResetEloMatchStats();

            ReadOnlySpan<CCSPlayerController> players = CollectionsMarshal.AsSpan(_serverPlayersCache);
            for (int i = 0; i < players.Length; i++)
            {
                var p = players[i];
                if (p is { IsValid: true, Handle: not 0, IsBot: false, IsHLTV: false, TeamNum: 2 or 3 })
                {
                    if (_readyPlayers.Contains(p.SteamID))
                    {
                        _lockedTeam[p.SteamID] = p.TeamNum;
                    }
                }
            }

            string modeText = totalPlayers == 2 ? "1 v 1 " : $"{activeT} v {activeCT} ";
            string displayLimit = matchCfg.MatchModes.Count > 0 ? matchCfg.MatchModes[0].DisplayTarget : "20";

            _privateCheckTimer?.Kill(); _privateCheckTimer = null;
            _publicBroadcastTimer?.Kill(); _publicBroadcastTimer = null;
            _waitingTimer?.Kill(); _waitingTimer = null;

            string precompiledLine1 = string.Format(matchCfg.HudHtml_Round1_Line1, modeText);
            string precompiledLine2 = string.Format(matchCfg.HudHtml_Round1_Line2, displayLimit);
            const string popupSoundCmd = "play sounds/ui/panorama/popup_reveal_01.vsnd";

            int countdown = (int)matchCfg.HudDuration_MatchStart;
            if (countdown <= 0) countdown = 3;

            string[] precompiledHuds = new string[countdown + 1];
            string[] precompiledChats = new string[countdown + 1];

            for (int i = 1; i <= countdown; i++)
            {
                string countdownHtml = $"<b>➡ ➡ ➡<font class='fontSize-l' color='orange'> 倒 數  </font><font class='fontSize-l' color='#DF3935'>{i}</font><font class='fontSize-l' color='orange'>  秒 </font>⬅ ⬅ ⬅</b><br>";
                precompiledHuds[i] = $"{precompiledLine1}{countdownHtml}{precompiledLine2}";
                precompiledChats[i] = $" {_cachedPrefix} {ChatColors.Gold}{modeText}{ChatColors.White} 戰 鬥 開 始！倒 數 {ChatColors.LightRed}{i}{ChatColors.White} 秒 ...";
            }

            Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Gold}{modeText}{ChatColors.White} 雙 方 皆 已 準 備，即 將 開 戰 ...");

            Action tickLogic = () =>
            {
                if (countdown > 0)
                {
                    ShowHud(precompiledHuds[countdown], countdown == 1 ? 0.95f : 1.1f);
                    Server.PrintToChatAll(precompiledChats[countdown]);

                    ReadOnlySpan<CCSPlayerController> curPlayers = CollectionsMarshal.AsSpan(_serverPlayersCache);
                    for (int i = 0; i < curPlayers.Length; i++)
                    {
                        var p = curPlayers[i];
                        if (p is { IsValid: true, IsBot: false }) p.ExecuteClientCommand(popupSoundCmd);
                    }
                    countdown--;
                }
                else
                {
                    _isCountdownActive = false;
                    ClearHudImmediate();

                    // 再次確保載入 LIVE.CFG 時 AFK 完全停止運作
                    StopAfkManagerForLiveMatch();

                    Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Gold}{modeText}{ChatColors.White} 比 賽 正 式 開 始");
                    if (activeT >= 2 && activeCT >= 2) Console.WriteLine("[ 2 v 2 團 戰 ] 比 賽 開 始");

                    bool is1v1 = _liveMatchTargetPlayers == 2;
                    Server.NextFrame(() =>
                    {
                        PurgeMapWeaponEntities();
                        Server.ExecuteCommand($"exec {Config.MatchSettings.LiveConfigName}");
                        Server.NextFrame(() => ApplyVoiceMode(enableAllTalk: is1v1));
                    });

                    _liveTimer?.Kill();
                    _liveTimer = null;
                }
            };

            _liveTimer?.Kill();
            _liveTimer = AddTimer(0.5f, () =>
            {
                tickLogic();

                if (countdown >= 0)
                {
                    _liveTimer?.Kill();
                    _liveTimer = AddTimer(1.0f, tickLogic, TimerFlags.REPEAT);
                }
            });
        }
    }

    // 【0-GC 常數級武器分類與實名辨識】：利用 ItemDefinitionIndex 避免多餘字串搜尋
    private static (string Name, int SlotCategory) ClassifyWeaponFast(CBasePlayerWeapon weapon)
    {
        ushort defIndex = weapon.AttributeManager.Item.ItemDefinitionIndex;
        switch (defIndex)
        {
            case 41 or 42 or 59 or >= 500:
                return ("weapon_knife", 0); // 0 = 刀
            case 1:  return ("weapon_deagle", 2);
            case 2:  return ("weapon_elite", 2);
            case 3:  return ("weapon_fiveseven", 2);
            case 4:  return ("weapon_glock", 2);
            case 30: return ("weapon_tec9", 2);
            case 32: return ("weapon_hkp2000", 2);
            case 36: return ("weapon_p250", 2);
            case 61: return ("weapon_usp_silencer", 2);
            case 63: return ("weapon_cz75a", 2);
            case 64: return ("weapon_revolver", 2); // 2 = 副武
            case 7:  return ("weapon_ak47", 1);
            case 9:  return ("weapon_awp", 1);
            case 13: return ("weapon_galilar", 1);
            case 16: return ("weapon_m4a1", 1);
            case 40: return ("weapon_ssg08", 1);
            case 60: return ("weapon_m4a1_silencer", 1); // 1 = 主武
            case 49: return ("weapon_c4", -1);
        }

        string dName = weapon.DesignerName;
        if (string.IsNullOrEmpty(dName)) return ("", -1);
        if (dName.Contains("knife", StringComparison.OrdinalIgnoreCase) || dName.Contains("bayonet", StringComparison.OrdinalIgnoreCase))
            return (dName, 0);
        if (PistolWeapons.Contains(dName))
            return (dName, 2);
        if (dName.StartsWith("weapon_", StringComparison.OrdinalIgnoreCase) && !dName.Contains("c4", StringComparison.OrdinalIgnoreCase))
            return (dName, 1);

        return (dName, -1);
    }

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
            if (!hasKnife) player.GiveNamedItem("weapon_knife");
            if (!priMatches && !string.IsNullOrEmpty(targetPri)) player.GiveNamedItem(targetPri);
            if (!secMatches && !string.IsNullOrEmpty(targetSec)) player.GiveNamedItem(targetSec);
        }
        else
        {
            player.RemoveWeapons();
            player.GiveNamedItem("weapon_knife");
            if (!string.IsNullOrEmpty(targetPri)) player.GiveNamedItem(targetPri);
            if (!string.IsNullOrEmpty(targetSec)) player.GiveNamedItem(targetSec);
        }
    }

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

            // 僅在暖身期間同步 AFK 基準座標
            OnAfkPlayerSpawn(player);
        }

        Server.NextFrame(() =>
        {
            if (player is not { IsValid: true, PawnIsAlive: true } || player.PlayerPawn.Value is not { IsValid: true } pawn) return;
            if (player.TeamNum is not 2 and not 3) return;

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
                        var (wName, slotCat) = ClassifyWeaponFast(w);
                        if (slotCat == 0) hasKnife = true;
                        else if (slotCat == 1) { currentPri = wName; priCount++; }
                        else if (slotCat == 2) { currentSec = wName; secCount++; }
                    }
                }
            }

            var modes = Config.MatchSettings.MatchModes;
            if (_isMatchLive && !_isCountdownActive && modes.Count > _currentPhaseIndex)
            {
                var phase = modes[_currentPhaseIndex];
                pawn.Health = phase.Health;

                string secToGive = phase.SecondaryWeapons.Count > 0 ? phase.SecondaryWeapons[0] : "";
                if (_playerSecondary.TryGetValue(steamId, out string? prefSec) && phase.SecondaryWeaponsSet.Contains(prefSec))
                    secToGive = prefSec;

                string priToGive = phase.PrimaryWeapons.Count > 0 ? phase.PrimaryWeapons[0] : "";
                if (_playerPrimary.TryGetValue(steamId, out string? prefPri) && phase.PrimaryWeaponsSet.Contains(prefPri))
                    priToGive = prefPri;

                ApplySmartLoadout(player, hasKnife, currentPri, currentSec, priCount, secCount, priToGive, secToGive);
                ApplySmartArmor(player, pawn, phase.Armor);

                if (phase.Grenades != null)
                {
                    for (int i = 0; i < phase.Grenades.Count; i++) player.GiveNamedItem(phase.Grenades[i]);
                }
            }
            else
            {
                // 暖身模式：直接讀取預編譯快取，0 迴圈
                string targetWarmupPri = _playerPrimary.TryGetValue(steamId, out string? prefPri) ? prefPri : _defaultWarmupPri;
                string targetWarmupSec = _playerSecondary.TryGetValue(steamId, out string? prefSec) ? prefSec : _defaultWarmupSec;

                ApplySmartLoadout(player, hasKnife, currentPri, currentSec, priCount, secCount, targetWarmupPri, targetWarmupSec);
                if (_defaultWarmupArmor > 0) ApplySmartArmor(player, pawn, _defaultWarmupArmor);
            }
        });

        return HookResult.Continue;
    }

    private void ReplaceWeapon(CCSPlayerController player, string newWeapon)
    {
        uint idx = player.Index;
        // 單幀防連點鎖：防止玩家在同一個 Tick 內透過滾輪或巨集連發換槍指令造成實體重複卸載抖動
        if (!_weaponReplacePending.Add(idx)) return;

        Server.NextFrame(() =>
        {
            _weaponReplacePending.Remove(idx);

            if (player is not { IsValid: true, PawnIsAlive: true, TeamNum: 2 or 3 } || player.PlayerPawn.Value is not { IsValid: true } pawn) return;
            if (pawn.WeaponServices?.MyWeapons is not { } weapons) return;

            string currentPri = "";
            string currentSec = "";
            bool hasKnife = false;

            foreach (var weaponHandle in weapons)
            {
                if (weaponHandle.Value is { IsValid: true } weapon)
                {
                    var (wName, slotCat) = ClassifyWeaponFast(weapon);
                    if (slotCat == 0) hasKnife = true;
                    else if (slotCat == 1) currentPri = wName;
                    else if (slotCat == 2) currentSec = wName;
                }
            }

            bool isNewPistol = PistolWeapons.Contains(newWeapon);
            string targetSec = isNewPistol ? newWeapon : currentSec;
            string targetPri = isNewPistol ? currentPri : newWeapon;

            if (isNewPistol && string.Equals(currentSec, newWeapon, StringComparison.OrdinalIgnoreCase)) return;
            if (!isNewPistol && string.Equals(currentPri, newWeapon, StringComparison.OrdinalIgnoreCase)) return;

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
        for (int i = 0; i < _cachedGunMenuMessage.Count; i++)
        {
            player.PrintToChat(_cachedGunMenuMessage[i]);
        }
    }

    private void CheckAndWarnUnreadyPlayers()
    {
        if (_isMatchLive) return;

        var matchCfg = Config.MatchSettings;
        ReadOnlySpan<CCSPlayerController> players = CollectionsMarshal.AsSpan(_serverPlayersCache);
        for (int i = 0; i < players.Length; i++)
        {
            var p = players[i];
            if (p is { IsValid: true, Handle: not 0, IsBot: false, IsHLTV: false, TeamNum: 2 or 3 })
            {
                ulong steamId = p.SteamID;
                if (!_readyPlayers.Contains(steamId))
                {
                    _playerJoinTime.TryGetValue(steamId, out float joinTime);
                    if (joinTime > 0 && (Server.CurrentTime - joinTime) < (matchCfg.UnreadyReminderInterval - 2.0f))
                    {
                        continue;
                    }

                    _playerUnreadyTime.TryGetValue(steamId, out int currentTime);
                    int updatedTime = currentTime + matchCfg.UnreadyReminderInterval;
                    _playerUnreadyTime[steamId] = updatedTime;

                    if (updatedTime >= matchCfg.KickUnreadyPlayerTime)
                    {
                        string kickedName = p.PlayerName;
                        int userId = p.UserId ?? -1;
                        Server.NextFrame(() => Server.ExecuteCommand($"kickid {userId} Unready_Timeout"));
                        Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Lime}{kickedName} {ChatColors.White}因 未 {ChatColors.Lime}!R{ChatColors.White}準 備 而 被 踢 出");
                        _playerUnreadyTime.Remove(steamId);
                    }
                    else
                    {
                        int timeLeft = matchCfg.KickUnreadyPlayerTime - updatedTime;
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

        var (activeT, activeCT) = CountActiveCombatPlayers();
        int totalPlayers = activeT + activeCT;

        if (totalPlayers > 0 && totalPlayers == _readyPlayers.Count)
        {
            int halfReq = GetDynamicRequiredPlayers() / 2;
            string modeHint = totalPlayers == 1
                ? $" [ {ChatColors.Green}對 戰 系 統{ChatColors.White} ] {ChatColors.White}場 上 {ChatColors.Green}1 {ChatColors.White}人，等 待 對 手 加 入..."
                : $" [ {ChatColors.Green}對 戰 系 統{ChatColors.White} ] {ChatColors.White}場 上 {ChatColors.Green}{totalPlayers} {ChatColors.White}人，等 對 手 加 入 {ChatColors.Green}{halfReq} v {halfReq} {ChatColors.White}團 戰";
            Server.PrintToChatAll(modeHint);
        }
    }

    private void BroadcastUnreadyPlayers()
    {
        if (_isMatchLive) return;

        _unreadyNamesCache.Clear();
        int totalPlayers = 0;

        ReadOnlySpan<CCSPlayerController> players = CollectionsMarshal.AsSpan(_serverPlayersCache);
        for (int i = 0; i < players.Length; i++)
        {
            var p = players[i];
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

            string modeHint = totalPlayers switch
            {
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
        _lockedTeam.Clear();
        _playerUnreadyTime.Clear();
        _playerJoinTime.Clear();
        _joinTeamCooldown.Clear();
        _weaponReplacePending.Clear();

        _playerPrimary.Clear();
        _playerSecondary.Clear();
        ResetEloMatchStats();

        _pendingInitialReminders.Clear();
        _hasReceivedInitialReminder.Clear();
        _activeCenterMessage = "";
        _centerMessageExpiration = 0f;

        var matchCfg = Config.MatchSettings;
        _liveTimer?.Kill(); _liveTimer = null;
        _privateCheckTimer?.Kill();
        _privateCheckTimer = AddTimer(matchCfg.UnreadyReminderInterval, CheckAndWarnUnreadyPlayers, TimerFlags.REPEAT);

        _publicBroadcastTimer?.Kill();
        _publicBroadcastTimer = AddTimer(matchCfg.PublicUnreadyReminderInterval, BroadcastUnreadyPlayers, TimerFlags.REPEAT);

        _waitingTimer?.Kill();
        _waitingTimer = AddTimer(matchCfg.WaitingForOpponentInterval, BroadcastWaitingMessage, TimerFlags.REPEAT);
    }

    private HookResult OnEventRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        if (_trackedMapEquips.Count > 0) PurgeMapWeaponEntities();

        var matchCfg = Config.MatchSettings;
        if (!_isMatchLive || _isCountdownActive || matchCfg.MatchModes.Count == 0 || _currentPhaseIndex >= matchCfg.MatchModes.Count)
            return HookResult.Continue;

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

        var currentPhase = matchCfg.MatchModes[_currentPhaseIndex];

        string fullHudHtml = string.Format(matchCfg.HudHtml_RoundStart_Title, currentPhase.Name, currentPhase.DisplayTarget) +
                             string.Format(matchCfg.HudHtml_RoundStart_TScore, displayScoreT) +
                             string.Format(matchCfg.HudHtml_RoundStart_CTScore, displayScoreCT);

        ShowHud(fullHudHtml, matchCfg.RoundStartHudDuration);

        return HookResult.Continue;
    }

    public override void Unload(bool hotReload)
    {
        _isServerShuttingDown = true;
        UnloadEloClanTagSystem();
        UnloadAfkManager();
        base.Unload(hotReload);
    }
}
