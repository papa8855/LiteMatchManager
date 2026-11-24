using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace LiteMatchManager;

// =========================================
// 指定槍模式 PK 設定結構 (對應 LiteMatchManager.json 的 CustomModeSettings)
// =========================================
public sealed class CustomModeSettingsConfig
{
    [JsonPropertyName("Enabled")] public bool Enabled { get; set; } = true;
    [JsonPropertyName("AllowIn2v2")] public bool AllowIn2v2 { get; set; } = true;
    [JsonPropertyName("EnableEloInCustomMode")] public bool EnableEloInCustomMode { get; set; } = true;
    [JsonPropertyName("DisablePistolInSniperAndRifle")] public bool DisablePistolInSniperAndRifle { get; set; } = true;
    [JsonPropertyName("AlreadyReadyConfirmSeconds")] public int AlreadyReadyConfirmSeconds { get; set; } = 15;
    [JsonPropertyName("ProposeCooldownSeconds")] public int ProposeCooldownSeconds { get; set; } = 60;
    [JsonPropertyName("MaxProposalsPerWarmup")] public int MaxProposalsPerWarmup { get; set; } = 2;

    [JsonPropertyName("PistolModeCommands")] public List<string> PistolModeCommands { get; set; } = ["pk1", "pistol", "手槍"];
    [JsonPropertyName("SniperModeCommands")] public List<string> SniperModeCommands { get; set; } = ["pk2", "sniper", "狙擊"];
    [JsonPropertyName("RifleModeCommands")] public List<string> RifleModeCommands { get; set; } = ["pk3", "rifle", "步槍"];
    [JsonPropertyName("RejectCommands")] public List<string> RejectCommands { get; set; } = ["no", "拒絕"];

    // 獨立指定模式 HTML HUD 顯示時間與排版設定 (完全複製主程式結構，互不衝突)
    [JsonPropertyName("HudDuration_CustomMatchAbort")] public float HudDuration_CustomMatchAbort { get; set; } = 3.0f;
    [JsonPropertyName("HudDuration_CustomMatchStart")] public float HudDuration_CustomMatchStart { get; set; } = 3.0f;
    [JsonPropertyName("CustomRoundStartHudDuration")] public float CustomRoundStartHudDuration { get; set; } = 1.7f;

    [JsonPropertyName("HudHtml_CustomMatchAbort_Line1")] public string HudHtml_CustomMatchAbort_Line1 { get; set; } = "<b><font class='fontSize-l' color='gold'>玩 家 逃 跑 ， {0} 戰 鬥 終 止</font></b><BR>";
    [JsonPropertyName("HudHtml_CustomMatchAbort_Line2")] public string HudHtml_CustomMatchAbort_Line2 { get; set; } = "<b><font class='fontSize-l' color='lime'>比 賽 已 退 回 暖 場 模 式</font></b>";

    // {0} = 對戰規模 (如 1 v 1 )，{1} = 指定模式名稱 (如 步槍)
    [JsonPropertyName("HudHtml_CustomRound1_Line1")] public string HudHtml_CustomRound1_Line1 { get; set; } = "<b><font class='fontSize-l' color='gold'>★ {0}{1} 戰 鬥 開 始 ★</font></b><BR>";
    // {0} = 勝利回合數 (對應 FinalMatchWinScore，如 30)
    [JsonPropertyName("HudHtml_CustomRound1_Line2")] public string HudHtml_CustomRound1_Line2 { get; set; } = "<b><font class='fontSize-l' color='white'>對 戰 採 </font><font class='fontSize-l' color='lime'>{0}</font><font class='fontSize-l' color='white'> 回 合 勝 利 制</font></b>";

    // {0} = 指定模式名稱 (如 步槍)，{1} = 勝利回合數 (如 30)
    [JsonPropertyName("HudHtml_CustomRoundStart_Title")] public string HudHtml_CustomRoundStart_Title { get; set; } = "<b><font class='fontSize-l' color='lime'>{0} </font><font class='fontSize-l' color='gold'>模式 / 搶 </font><font class='fontSize-l' color='Green'>{1}</font><font class='fontSize-l' color='gold'> 勝</font></b><br>";
    // {0} = T 目前比分
    [JsonPropertyName("HudHtml_CustomRoundStart_TScore")] public string HudHtml_CustomRoundStart_TScore { get; set; } = "<b><font class='fontSize-l' color='#FF4500'>目 前 恐 怖 份 子：{0}</font></b><br>";
    // {0} = CT 目前比分，{1} = 勝利回合數 (如 30)
    [JsonPropertyName("HudHtml_CustomRoundStart_CTScore")] public string HudHtml_CustomRoundStart_CTScore { get; set; } = "<b><font class='fontSize-l' color='lightblue'>目 前 反 恐 精 英：{0}</font></b><br><b><font class='fontSize-l' color='gold'>比 賽 贏 </font><font class='fontSize-l' color='Green'>{1}</font><font class='fontSize-l' color='gold'> 回 合 為 主</font></b>";
}

public partial class LiteMatchManager
{
    private const float JoinNotifyDelaySeconds = 1.5f;
    private const float SoloSwitchCooldownSeconds = 3.0f;

    private FrozenSet<string> _pistolModeCommandsSet = FrozenSet<string>.Empty;
    private FrozenSet<string> _sniperModeCommandsSet = FrozenSet<string>.Empty;
    private FrozenSet<string> _rifleModeCommandsSet = FrozenSet<string>.Empty;
    private FrozenSet<string> _rejectModeCommandsSet = FrozenSet<string>.Empty;

    // 正式開戰鎖定狀態
    private bool _isCustomModeLocked = false;
    private int _lockedCustomPhaseIndex = 0;

    // 預約制核心狀態 (!pkX 本身即視同 !R 已準備 + 預約該模式)
    private bool _hasCustomReservation = false;
    private int _reservedCustomPhaseIndex = -1;
    private ulong _customProposerId = 0;
    private string _customProposerName = "";
    private bool _wasBalancedInviteActive = false;

    // 僅於「對方已先 !R、我方才打 !pkX」時啟動的短倒數確認狀態
    private bool _isAlreadyReadyCountdown = false;
    private int _confirmRemainingSeconds = 0;

    private readonly HashSet<ulong> _customAgreedPlayers = new(8);
    private readonly HashSet<ulong> _notifiedJoinPlayers = new(8);
    private readonly List<ulong> _tempRemoveIds = new(8);
    private readonly List<string> _customUnrespondedNamesCache = new(8);

    private CounterStrikeSharp.API.Modules.Timers.Timer? _customHudTimer;
    private CounterStrikeSharp.API.Modules.Timers.Timer? _joinNotifyDelayTimer;

    // 防騷擾冷卻與次數上限 (僅於對手拒絕或確認逾時時計入；換圖重置)
    private readonly Dictionary<ulong, float> _customProposeCooldown = new(64);
    private readonly Dictionary<ulong, int> _customProposalCount = new(64);
    private readonly Dictionary<ulong, float> _customSwitchCooldown = new(64);

    // 僅在「已 !R 對手確認倒數中」阻擋自動開標準局，絕不凍結未準備玩家的掛機踢除計時
    private bool IsCustomModeVoteActive => _isAlreadyReadyCountdown;

    // 供主程式在 2v2 避免暖場 Prep HTML HUD 與指定模式邀請重疊
    private bool IsCustomModeSuppressingPrepHud =>
        _hasCustomReservation && (_isAlreadyReadyCountdown || IsBalancedCombatRoster());

    // 供主程式 CheckPendingReminders 判斷：該玩家當下已收到 PK 邀請提示，略過 5 秒初始 !R 彈窗防洗畫面
    private bool IsCustomModePromptActiveFor(ulong steamId)
    {
        if (_isAlreadyReadyCountdown) return true;
        return _hasCustomReservation && IsBalancedCombatRoster() && !_customAgreedPlayers.Contains(steamId);
    }

    // 僅在「指定模式 PK 的狙擊(1)／步槍(2)」禁小槍；標準 !R 三階段與手槍模式(0)完全不受影響
    private bool IsPistolDisabledInCurrentMatch()
    {
        return _isCustomModeLocked &&
               Config.CustomModeSettings.DisablePistolInSniperAndRifle &&
               _currentPhaseIndex is 1 or 2;
    }

    private static string SafeFormat1<T0>(string template, T0 arg0)
    {
        if (string.IsNullOrEmpty(template)) return "";
        try { return string.Format(template, arg0); }
        catch (FormatException) { return template; }
    }

    private static string SafeFormat2<T0, T1>(string template, T0 arg0, T1 arg1)
    {
        if (string.IsNullOrEmpty(template)) return "";
        try { return string.Format(template, arg0, arg1); }
        catch (FormatException) { return template; }
    }

    private void OnCustomModeConfigParsed(LiteMatchConfig config)
    {
        var customCfg = config.CustomModeSettings;

        _pistolModeCommandsSet = (customCfg.PistolModeCommands ?? ["pk1"]).ToFrozenSet(StringComparer.OrdinalIgnoreCase);
        _sniperModeCommandsSet = (customCfg.SniperModeCommands ?? ["pk2"]).ToFrozenSet(StringComparer.OrdinalIgnoreCase);
        _rifleModeCommandsSet  = (customCfg.RifleModeCommands ?? ["pk3"]).ToFrozenSet(StringComparer.OrdinalIgnoreCase);
        _rejectModeCommandsSet = (customCfg.RejectCommands ?? ["no"]).ToFrozenSet(StringComparer.OrdinalIgnoreCase);

        _ = _pistolModeCommandsSet.Contains("");
        _ = _sniperModeCommandsSet.Contains("");
        _ = _rifleModeCommandsSet.Contains("");
        _ = _rejectModeCommandsSet.Contains("");

        if (!customCfg.Enabled)
        {
            CancelCustomModeVote(silent: true, resumeUnreadyTimer: false);
            _isCustomModeLocked = false;
            _lockedCustomPhaseIndex = 0;
        }
    }

    private void UnloadCustomModeManager()
    {
        KillCustomModeTimers();
        _hasCustomReservation = false;
        _isAlreadyReadyCountdown = false;
        _wasBalancedInviteActive = false;
        _isCustomModeLocked = false;
        _customAgreedPlayers.Clear();
        _notifiedJoinPlayers.Clear();
        _customProposeCooldown.Clear();
        _customProposalCount.Clear();
        _customSwitchCooldown.Clear();
    }

    // 供主程式 ResetMatchState 呼叫；換圖時額外清空個人冷卻與暖場次數上限
    private void ResetCustomModeState(bool clearWarmupQuota = false)
    {
        CancelCustomModeVote(silent: true, resumeUnreadyTimer: false);
        _isCustomModeLocked = false;
        _lockedCustomPhaseIndex = 0;

        if (clearWarmupQuota)
        {
            _customProposeCooldown.Clear();
            _customProposalCount.Clear();
            _customSwitchCooldown.Clear();
        }
    }

    private void KillCustomModeTimers()
    {
        _customHudTimer?.Kill();
        _customHudTimer = null;
        _joinNotifyDelayTimer?.Kill();
        _joinNotifyDelayTimer = null;
    }

    // 【聊天室與中央純文字專用：與 LiteMatchManager.json 分開，獨立控制空格排版
    private string GetCustomModeDisplayName(int phaseIndex = -1)
    {
        int idx = phaseIndex >= 0 ? phaseIndex : _lockedCustomPhaseIndex;
        return idx switch
        {
            0 => "手 槍",
            1 => "狙 擊",
            2 => "步 槍",
            _ => "指 定 武 器"
        };
    }

    // 【HTML HUD 看板專用：維持讀取 LiteMatchManager.json 的 Duel_Name，與經典模式一致
    private string GetCustomModeHudName(int phaseIndex = -1)
    {
        int idx = phaseIndex >= 0 ? phaseIndex : _lockedCustomPhaseIndex;
        var modes = Config.MatchSettings.MatchModes;
        if (modes != null && idx >= 0 && idx < modes.Count && !string.IsNullOrWhiteSpace(modes[idx].Name))
        {
            return modes[idx].Name.Trim();
        }

        return idx switch
        {
            0 => "手槍",
            1 => "狙擊",
            2 => "步槍",
            _ => "指定武器"
        };
    }

    private string GetCustomModePrimaryCommand(int phaseIndex)
    {
        var customCfg = Config.CustomModeSettings;
        List<string>? list = phaseIndex switch
        {
            0 => customCfg.PistolModeCommands,
            1 => customCfg.SniperModeCommands,
            2 => customCfg.RifleModeCommands,
            _ => null
        };

        if (list is { Count: > 0 } && !string.IsNullOrWhiteSpace(list[0]))
            return $"!{list[0]}";

        return phaseIndex switch
        {
            0 => "!pk1",
            1 => "!pk2",
            2 => "!pk3",
            _ => "!pk1"
        };
    }

    // =========================================================================
    // 獨立指定模式 HTML HUD 供應器 (直接沿用主程式 ShowHud 管線，絕不重疊打架)
    // =========================================================================
    private string GetCustomRound1Line1(string modeText)
    {
        string modeName = GetCustomModeHudName();
        return SafeFormat2(Config.CustomModeSettings.HudHtml_CustomRound1_Line1, modeText, modeName);
    }

    private string GetCustomRound1Line2()
    {
        int targetWin = Config.MatchSettings.FinalMatchWinScore;
        return SafeFormat1(Config.CustomModeSettings.HudHtml_CustomRound1_Line2, targetWin);
    }

    private (string Html, float Duration) GetCustomMatchAbortHud()
    {
        var customCfg = Config.CustomModeSettings;
        string modeName = GetCustomModeHudName();
        string line1 = SafeFormat1(customCfg.HudHtml_CustomMatchAbort_Line1, modeName);
        return ($"{line1}<br>{customCfg.HudHtml_CustomMatchAbort_Line2}<br>", customCfg.HudDuration_CustomMatchAbort);
    }

    private void ShowCustomRoundStartHud(int scoreT, int scoreCT)
    {
        var customCfg = Config.CustomModeSettings;
        string modeName = GetCustomModeHudName();
        int targetWin = Config.MatchSettings.FinalMatchWinScore;

        string fullHudHtml =
            SafeFormat2(customCfg.HudHtml_CustomRoundStart_Title, modeName, targetWin) +
            SafeFormat1(customCfg.HudHtml_CustomRoundStart_TScore, scoreT) +
            SafeFormat2(customCfg.HudHtml_CustomRoundStart_CTScore, scoreCT, targetWin);

        ShowHud(fullHudHtml, customCfg.CustomRoundStartHudDuration);
    }

    // =========================================================================
    // 0-GC 人數與狀態輔助查詢
    // =========================================================================
    private bool IsBalancedCombatRoster()
    {
        var (activeT, activeCT) = CountActiveCombatPlayers();
        int total = activeT + activeCT;
        return total >= 2 && activeT == activeCT && activeT <= Config.MatchSettings.MaxPlayersPerTeam;
    }

    private bool IsActiveCombatPlayer(ulong steamId)
    {
        ReadOnlySpan<CCSPlayerController> players = CollectionsMarshal.AsSpan(_serverPlayersCache);
        for (int i = 0; i < players.Length; i++)
        {
            var p = players[i];
            if (p is { IsValid: true, Handle: not 0, IsBot: false, IsHLTV: false, TeamNum: 2 or 3 } && p.SteamID == steamId)
                return true;
        }
        return false;
    }

    private CCSPlayerController? FindActiveCombatPlayer(ulong steamId)
    {
        ReadOnlySpan<CCSPlayerController> players = CollectionsMarshal.AsSpan(_serverPlayersCache);
        for (int i = 0; i < players.Length; i++)
        {
            var p = players[i];
            if (p is { IsValid: true, Handle: not 0, IsBot: false, IsHLTV: false, TeamNum: 2 or 3 } && p.SteamID == steamId)
                return p;
        }
        return null;
    }

    private (int TotalCombat, int ReadyCombat, int AgreedCombat) CountCustomModeCombatStats()
    {
        int total = 0, ready = 0, agreed = 0;
        ReadOnlySpan<CCSPlayerController> players = CollectionsMarshal.AsSpan(_serverPlayersCache);
        for (int i = 0; i < players.Length; i++)
        {
            var p = players[i];
            if (p is { IsValid: true, Handle: not 0, IsBot: false, IsHLTV: false, TeamNum: 2 or 3, SteamID: > 0 })
            {
                total++;
                ulong sid = p.SteamID;
                if (_readyPlayers.Contains(sid)) ready++;
                if (_customAgreedPlayers.Contains(sid)) agreed++;
            }
        }
        return (total, ready, agreed);
    }

    private void PruneDepartedCombatPlayers()
    {
        if (_customAgreedPlayers.Count == 0 && _notifiedJoinPlayers.Count == 0) return;

        _tempRemoveIds.Clear();
        foreach (ulong sid in _customAgreedPlayers)
        {
            if (!IsActiveCombatPlayer(sid)) _tempRemoveIds.Add(sid);
        }
        for (int i = 0; i < _tempRemoveIds.Count; i++) _customAgreedPlayers.Remove(_tempRemoveIds[i]);

        _tempRemoveIds.Clear();
        foreach (ulong sid in _notifiedJoinPlayers)
        {
            if (!IsActiveCombatPlayer(sid)) _tempRemoveIds.Add(sid);
        }
        for (int i = 0; i < _tempRemoveIds.Count; i++) _notifiedJoinPlayers.Remove(_tempRemoveIds[i]);
        _tempRemoveIds.Clear();
    }

    private void MarkPlayerReadySilent(ulong steamId)
    {
        _readyPlayers.Add(steamId);
        _playerUnreadyTime.Remove(steamId);
        _pendingInitialReminders.Remove(steamId);
    }

    private void RecordProposerRejectionPenalty(ulong proposerId)
    {
        if (proposerId == 0) return;
        var customCfg = Config.CustomModeSettings;
        _customProposalCount[proposerId] = _customProposalCount.GetValueOrDefault(proposerId) + 1;
        if (customCfg.ProposeCooldownSeconds > 0)
        {
            _customProposeCooldown[proposerId] = Server.CurrentTime + customCfg.ProposeCooldownSeconds;
        }
    }

    private void BroadcastCenterTextToCombatPlayers(string message)
    {
        if (_isServerShuttingDown) return;
        ReadOnlySpan<CCSPlayerController> players = CollectionsMarshal.AsSpan(_serverPlayersCache);
        for (int i = 0; i < players.Length; i++)
        {
            var p = players[i];
            if (p is { IsValid: true, Handle: not 0, IsBot: false, IsHLTV: false, TeamNum: 2 or 3 })
            {
                p.PrintToCenter(message);
            }
        }
    }

    // =========================================================================
    // 暖場定時廣播整合（供 LiteMatchManager 的 Waiting / Unready 計時器呼叫）
    // =========================================================================
    private bool TryBroadcastCustomWaitingMessage()
    {
        if (!_hasCustomReservation || _isMatchLive || _isAlreadyReadyCountdown) return false;

        var (activeT, activeCT) = CountActiveCombatPlayers();
        int totalPlayers = activeT + activeCT;
        if (totalPlayers <= 0 || totalPlayers != _readyPlayers.Count) return false;

        string modeName = GetCustomModeDisplayName(_reservedCustomPhaseIndex);
        Server.PrintToChatAll($" {_cachedPrefix} 場 上 {ChatColors.Green}{totalPlayers} {ChatColors.White}人，已 預 約{ChatColors.Gold}{modeName} 模 式{ChatColors.White} PK，等 待 對 手 加 入...（輸 入 {ChatColors.Lime}!R {ChatColors.White}改 回 標 準）");
        return true;
    }

    private bool TryBroadcastCustomUnreadyMessage()
    {
        if (!_hasCustomReservation || _isMatchLive || _isAlreadyReadyCountdown || !IsBalancedCombatRoster())
            return false;

        _customUnrespondedNamesCache.Clear();
        ReadOnlySpan<CCSPlayerController> players = CollectionsMarshal.AsSpan(_serverPlayersCache);
        for (int i = 0; i < players.Length; i++)
        {
            var p = players[i];
            if (p is { IsValid: true, Handle: not 0, IsBot: false, IsHLTV: false, TeamNum: 2 or 3 } &&
                !_customAgreedPlayers.Contains(p.SteamID))
            {
                _customUnrespondedNamesCache.Add(p.PlayerName);
            }
        }

        if (_customUnrespondedNamesCache.Count == 0) return false;

        string modeName = GetCustomModeDisplayName(_reservedCustomPhaseIndex);
        string cmdHint = GetCustomModePrimaryCommand(_reservedCustomPhaseIndex);
        Server.PrintToChatAll($" {_cachedPrefix} 尚 未 回 應：{ChatColors.Orange}{string.Join(", ", _customUnrespondedNamesCache)}{ChatColors.White} ｜ {ChatColors.Gold}{_customProposerName} {ChatColors.White}已 預 約{ChatColors.Gold}{modeName} 模 式{ChatColors.White} PK，同 意 打 {ChatColors.Lime}{cmdHint} {ChatColors.White}，標 準 打 {ChatColors.Lime}!R{ChatColors.White}（或 {ChatColors.Orange}!no{ChatColors.White}）");
        return true;
    }

    // =========================================================================
    // 聊天室指令攔截：!pk1 / !pk2 / !pk3 / !no
    // =========================================================================
    private bool TryHandleCustomModeCommand(CCSPlayerController player, string command)
    {
        if (!Config.CustomModeSettings.Enabled) return false;

        int targetPhase = -1;
        if (_pistolModeCommandsSet.Contains(command)) targetPhase = 0;
        else if (_sniperModeCommandsSet.Contains(command)) targetPhase = 1;
        else if (_rifleModeCommandsSet.Contains(command)) targetPhase = 2;

        bool isReject = targetPhase == -1 && _rejectModeCommandsSet.Contains(command);
        if (targetPhase == -1 && !isReject) return false;

        if (_isMatchLive)
        {
            player.PrintToChat($" {_cachedPrefix} {ChatColors.Orange}對 戰 進 行 中，無 法 變 更 對 戰 模 式");
            return true;
        }

        if (player.TeamNum is not (2 or 3))
        {
            player.PrintToChat($" {_cachedPrefix} {ChatColors.Gold}您 無 法 從 旁 觀 者 模 式 預 約 或 參 與 模 式 PK");
            return true;
        }

        Server.NextFrame(() =>
        {
            if (player is not { IsValid: true, TeamNum: 2 or 3 } || _isMatchLive || _isServerShuttingDown)
                return;

            if (isReject)
            {
                HandleCustomModeReject(player);
            }
            else
            {
                HandleCustomModeReserveOrAgree(player, targetPhase);
            }
        });

        return true;
    }

    private void HandleCustomModeReject(CCSPlayerController player)
    {
        if (!_hasCustomReservation)
        {
            player.PrintToChat($" {_cachedPrefix} {ChatColors.Grey}目 前 沒 有 進 行 中 的 指 定 模 式 預 約");
            return;
        }

        string modeName = GetCustomModeDisplayName(_reservedCustomPhaseIndex);
        ulong proposerId = _customProposerId;
        bool isProposer = player.SteamID == proposerId;

        if (!isProposer)
        {
            RecordProposerRejectionPenalty(proposerId);
        }

        CancelCustomModeVote(
            silent: true,
            centerNotice: isProposer ? $"已 取 消 {modeName} 模 式 預 約，改 為 標 準 對 戰" : $"婉 拒 {modeName} 模 式，恢 復 標 準 對 戰",
            resumeUnreadyTimer: false);

        if (isProposer)
        {
            Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Gold}{player.PlayerName} {ChatColors.White}取 消 了 {ChatColors.Orange}{modeName}{ChatColors.White} 模 式 預 約，維 持 標 準 準 備（{ChatColors.Lime}!R{ChatColors.White}）");
        }
        else
        {
            Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Gold}{player.PlayerName} {ChatColors.Orange}拒 絕 了 {ChatColors.White}{modeName} {ChatColors.Orange}模 式，恢 復 標 準 對 戰 等 待");
        }

        // 若場上全員原本都已準備 (包含發起人的 !pkX 視同 !R)，婉拒後立即開打標準賽
        CheckMatchStart();
    }

    private void HandleCustomModeReserveOrAgree(CCSPlayerController player, int targetPhase)
    {
        var customCfg = Config.CustomModeSettings;
        var modes = Config.MatchSettings.MatchModes;

        if (modes == null || targetPhase < 0 || targetPhase >= modes.Count)
        {
            player.PrintToChat($" {_cachedPrefix} {ChatColors.Orange}尚 未 配 置 該 武 器 階 段，無 法 啟 用");
            return;
        }

        var (activeT, activeCT) = CountActiveCombatPlayers();
        int totalPlayers = activeT + activeCT;

        if (totalPlayers > 2 && !customCfg.AllowIn2v2)
        {
            player.PrintToChat($" {_cachedPrefix} {ChatColors.Orange}目 前 指 定 模 式 PK 僅 限 1 v 1 單 挑 開 放");
            return;
        }

        ulong steamId = player.SteamID;
        string targetModeName = GetCustomModeDisplayName(targetPhase);
        string cmdHint = GetCustomModePrimaryCommand(targetPhase);
        float now = Server.CurrentTime;

        // 情況 A：場上已有進行中的預約
        if (_hasCustomReservation)
        {
            // A-1：發起人自己再次輸入 !pkX
            if (steamId == _customProposerId)
            {
                if (targetPhase == _reservedCustomPhaseIndex)
                {
                    player.PrintToChat($" {_cachedPrefix} 你 已 預 約{ChatColors.Gold}{targetModeName} 模 式{ChatColors.White} PK（已 完 成 準 備），輸 入 {ChatColors.Lime}!R {ChatColors.White}可 改 回 標 準");
                    return;
                }

                if (_isAlreadyReadyCountdown)
                {
                    player.PrintToChat($" {_cachedPrefix} {ChatColors.Orange}目 前 正 等 待 對 方 確 認 邀 請（剩 餘 {_confirmRemainingSeconds} 秒），請 稍 候 或 輸 入 {ChatColors.Lime}!R {ChatColors.Orange}取 消");
                    return;
                }

                if (_customSwitchCooldown.TryGetValue(steamId, out float nextSwitch) && now < nextSwitch)
                {
                    int waitSec = (int)Math.Ceiling(nextSwitch - now);
                    player.PrintToChat($" {_cachedPrefix} {ChatColors.Orange}切 換 預 約 模 式 過 快，請 {ChatColors.Lime}{waitSec} 秒{ChatColors.Orange} 後 再 試");
                    return;
                }

                _customSwitchCooldown[steamId] = now + SoloSwitchCooldownSeconds;
                _reservedCustomPhaseIndex = targetPhase;
                _customAgreedPlayers.Clear();
                _customAgreedPlayers.Add(steamId);
                _notifiedJoinPlayers.Clear();
                MarkPlayerReadySilent(steamId);

                if (IsBalancedCombatRoster())
                {
                    ClearHudImmediate();
                    _wasBalancedInviteActive = true;
                    BroadcastImmediateInviteToUnagreed();
                    StartPassiveInviteHudTimer();
                }
                else
                {
                    KillCustomModeTimers();
                    _wasBalancedInviteActive = false;
                    Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Gold}{player.PlayerName} {ChatColors.White}已 更 換 預 約{ChatColors.Gold}{targetModeName} 模 式{ChatColors.White} PK（已 完 成 準 備），等 待 對 手 加 入！恢 復 標 準 請 輸 入 {ChatColors.Lime}!R");
                    player.PrintToCenter($"已 更 換 預 約{targetModeName} 模 式 PK\n等 待 對 手 加 入（打 !R 改 回 標 準）");
                }
                return;
            }

            // A-2：其他玩家輸入了不同模式
            if (targetPhase != _reservedCustomPhaseIndex)
            {
                string currentModeName = GetCustomModeDisplayName(_reservedCustomPhaseIndex);
                string currentCmd = GetCustomModePrimaryCommand(_reservedCustomPhaseIndex);
                player.PrintToChat($" {_cachedPrefix} {ChatColors.Gold}{_customProposerName} {ChatColors.White}已 預 約{ChatColors.Gold}{currentModeName} 模 式{ChatColors.White} PK ｜ 同 意 打 {ChatColors.Lime}{currentCmd} {ChatColors.White}，標 準 對 戰 打 {ChatColors.Lime}!R{ChatColors.White}（或 {ChatColors.Orange}!no{ChatColors.White}）");
                return;
            }

            // A-3：其他玩家同意同一模式（同時視同已 !R 準備）
            MarkPlayerReadySilent(steamId);
            if (!_customAgreedPlayers.Add(steamId))
            {
                player.PrintToChat($" {_cachedPrefix} 你 已 同 意{ChatColors.Gold}{targetModeName} 模 式{ChatColors.White} PK，等 待 其 他 玩 家 回 應");
                return;
            }

            // 搶快防呆：若玩家在 1.5 秒延遲提示還沒跳出前就同意，立刻取消延遲計時
            _joinNotifyDelayTimer?.Kill();
            _joinNotifyDelayTimer = null;

            PruneDepartedCombatPlayers();
            var (combatTotal, readyCombat, agreedCombat) = CountCustomModeCombatStats();

            if (IsBalancedCombatRoster())
            {
                Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Gold}{player.PlayerName} {ChatColors.White}同 意 {ChatColors.Green}{targetModeName} 模 式{ChatColors.White} PK（進 度：{ChatColors.Lime}{agreedCombat} / {combatTotal}{ChatColors.White}）");

                if (agreedCombat >= combatTotal)
                {
                    FinalizeCustomModeAgreement();
                    return;
                }

                // 2v2 情境：若其餘還沒同意的人原本就已經 !R，直接轉入確認倒數
                if (readyCombat >= combatTotal && !_isAlreadyReadyCountdown)
                {
                    StartAlreadyReadyCountdown();
                }
                else if (_isAlreadyReadyCountdown)
                {
                    BroadcastAlreadyReadyCountdownHud();
                }
                else
                {
                    player.PrintToCenter($"已 同 意{targetModeName} 模 式 PK [ {agreedCombat} / {combatTotal} ]\n等 待 其 他 玩 家 回 應");
                    BroadcastPassiveInviteHudToUnagreed();
                }
            }
            else
            {
                Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Gold}{player.PlayerName} {ChatColors.White}同 意 {ChatColors.Green}{targetModeName} 模 式{ChatColors.White} PK（已 完 成 準 備），等 待 其 他 玩 家 加 入");
                player.PrintToCenter($"已 同 意{targetModeName} 模 式 PK\n等 待 對 手 加 入（打 !R 改 回 標 準）");
            }
            return;
        }

        // 情況 B：建立全新預約（先檢查被拒次數上限與冷卻）
        int maxProposals = customCfg.MaxProposalsPerWarmup;
        if (maxProposals > 0 && _customProposalCount.TryGetValue(steamId, out int usedCount) && usedCount >= maxProposals)
        {
            player.PrintToChat($" {_cachedPrefix} {ChatColors.Lime}指 定 模 式 {ChatColors.Orange}預 約 次 數 已 達 上 限（{maxProposals} 次），請 輸 入 {ChatColors.Lime}!R {ChatColors.Orange}準 備");
            return;
        }

        if (_customProposeCooldown.TryGetValue(steamId, out float nextAllowed) && now < nextAllowed)
        {
            int waitSec = (int)Math.Ceiling(nextAllowed - now);
            player.PrintToChat($" {_cachedPrefix} {ChatColors.Orange}預 約 模 式 冷 卻 中，請 於 {ChatColors.Lime}{waitSec} 秒{ChatColors.Orange} 後 再 試");
            return;
        }

        if (_customSwitchCooldown.TryGetValue(steamId, out float nextPropose) && now < nextPropose)
        {
            int waitSec = (int)Math.Ceiling(nextPropose - now);
            player.PrintToChat($" {_cachedPrefix} {ChatColors.Orange}指 令 操 作 過 快，請 {ChatColors.Lime}{waitSec} 秒{ChatColors.Orange} 後 再 試");
            return;
        }

        _customSwitchCooldown[steamId] = now + SoloSwitchCooldownSeconds;

        // !pkX 本身就代表玩家已輸入 !R 準備完成
        MarkPlayerReadySilent(steamId);

        _hasCustomReservation = true;
        _reservedCustomPhaseIndex = targetPhase;
        _customProposerId = steamId;
        _customProposerName = player.PlayerName ?? "Player";
        _isAlreadyReadyCountdown = false;
        _confirmRemainingSeconds = 0;
        _customAgreedPlayers.Clear();
        _customAgreedPlayers.Add(steamId);
        _notifiedJoinPlayers.Clear();

        if (IsBalancedCombatRoster())
        {
            var (combatTotal, readyCombat, _) = CountCustomModeCombatStats();
            ClearHudImmediate();
            _wasBalancedInviteActive = true;

            // 相反順序：對方（場上其他人）已全部先 !R，啟動設定檔秒數的確認倒數
            if (readyCombat >= combatTotal)
            {
                StartAlreadyReadyCountdown();
            }
            else
            {
                // 對方尚未 !R：立即通知對方，不開倒數，掛機踢人計時照常運作
                BroadcastImmediateInviteToUnagreed();
                player.PrintToCenter($"已 預 約{targetModeName} 模 式 PK\n等 待 對 手 回 應（打 !R 改 回 標 準）");
                StartPassiveInviteHudTimer();
            }
        }
        else
        {
            // 單人或人數未齊（1v0 / 2v0 / 2v1）：靜態預約，不洗畫面、不開每秒倒數
            _wasBalancedInviteActive = false;
            KillCustomModeTimers();
            Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Gold}{_customProposerName} {ChatColors.White}已 預 約{ChatColors.Gold}{targetModeName} 模 式{ChatColors.White} PK（已 完 成 準 備），等 待 對 手 加 入！恢 復 標 準 對 戰 請 輸 入 {ChatColors.Lime}!R");
            player.PrintToCenter($"已 預 約{targetModeName} 模 式 PK，等 待 對 手 加 入\n恢 復 標 準 對 戰 請 輸 入 !R");
        }
    }

    // =========================================================================
    // 推播與計時器管線 (全純文字 PrintToCenter + Chat，零 OnTick)
    // =========================================================================
    private void BroadcastImmediateInviteToUnagreed()
    {
        string modeName = GetCustomModeDisplayName(_reservedCustomPhaseIndex);
        string cmdHint = GetCustomModePrimaryCommand(_reservedCustomPhaseIndex);

        Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Gold}{_customProposerName} {ChatColors.White}已 預 約{ChatColors.Gold}{modeName} 模 式{ChatColors.White} PK ｜ 同 意 打 {ChatColors.Lime}{cmdHint} {ChatColors.White}，標 準 對 戰 打 {ChatColors.Lime}!R{ChatColors.White}（或 {ChatColors.Orange}!no{ChatColors.White}）");

        ReadOnlySpan<CCSPlayerController> players = CollectionsMarshal.AsSpan(_serverPlayersCache);
        for (int i = 0; i < players.Length; i++)
        {
            var p = players[i];
            if (p is { IsValid: true, Handle: not 0, IsBot: false, IsHLTV: false, TeamNum: 2 or 3, SteamID: > 0 } &&
                !_customAgreedPlayers.Contains(p.SteamID))
            {
                _notifiedJoinPlayers.Add(p.SteamID);
                p.PrintToCenter($"{_customProposerName} 已 預 約{modeName} 模 式 PK\n同 意 打 {cmdHint} ，標 準 對 戰 打 !R（或 !no）");
            }
        }
    }

    private void StartPassiveInviteHudTimer()
    {
        _customHudTimer?.Kill();
        _customHudTimer = AddTimer(1.0f, OnCustomHudTick, TimerFlags.REPEAT);
    }

    private void BroadcastPassiveInviteHudToUnagreed()
    {
        if (!_hasCustomReservation || _isAlreadyReadyCountdown) return;

        string modeName = GetCustomModeDisplayName(_reservedCustomPhaseIndex);
        string cmdHint = GetCustomModePrimaryCommand(_reservedCustomPhaseIndex);
        var (combatTotal, _, agreedCombat) = CountCustomModeCombatStats();

        string msg = combatTotal > 2
            ? $"{_customProposerName} 已 預 約{modeName} 模 式 PK [ {agreedCombat} / {combatTotal} ]\n同 意 打 {cmdHint} ，標 準 對 戰 打 !R（或 !no）"
            : $"{_customProposerName} 已 預 約{modeName} 模 式 PK\n同 意 打 {cmdHint} ，標 準 對 戰 打 !R（或 !no）";

        ReadOnlySpan<CCSPlayerController> players = CollectionsMarshal.AsSpan(_serverPlayersCache);
        for (int i = 0; i < players.Length; i++)
        {
            var p = players[i];
            if (p is { IsValid: true, Handle: not 0, IsBot: false, IsHLTV: false, TeamNum: 2 or 3, SteamID: > 0 } &&
                !_customAgreedPlayers.Contains(p.SteamID))
            {
                p.PrintToCenter(msg);
            }
        }
    }

    private void StartAlreadyReadyCountdown()
    {
        int confirmSec = Config.CustomModeSettings.AlreadyReadyConfirmSeconds;
        if (confirmSec <= 0) confirmSec = 15;

        _isAlreadyReadyCountdown = true;
        _confirmRemainingSeconds = confirmSec;
        _joinNotifyDelayTimer?.Kill();
        _joinNotifyDelayTimer = null;

        string modeName = GetCustomModeDisplayName(_reservedCustomPhaseIndex);
        string cmdHint = GetCustomModePrimaryCommand(_reservedCustomPhaseIndex);

        Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Gold}{_customProposerName} {ChatColors.White}邀 請{ChatColors.Gold}{modeName} 模 式{ChatColors.White} PK（已 完 成 準 備）");
        Server.PrintToChatAll($" {_cachedPrefix} 同 意 打 {ChatColors.Lime}{cmdHint} {ChatColors.White}，標 準 對 戰 請 再 打 一 次 {ChatColors.Lime}!R {ChatColors.White}或 {ChatColors.Orange}!no{ChatColors.White}（{ChatColors.Lime}{confirmSec} 秒{ChatColors.White} 後 自 動 進 入 標 準）");

        BroadcastAlreadyReadyCountdownHud();

        _customHudTimer?.Kill();
        _customHudTimer = AddTimer(1.0f, OnCustomHudTick, TimerFlags.REPEAT);
    }

    private void BroadcastAlreadyReadyCountdownHud()
    {
        if (!_hasCustomReservation || !_isAlreadyReadyCountdown) return;

        string modeName = GetCustomModeDisplayName(_reservedCustomPhaseIndex);
        string cmdHint = GetCustomModePrimaryCommand(_reservedCustomPhaseIndex);
        var (combatTotal, _, agreedCombat) = CountCustomModeCombatStats();

        string pendingMsg = combatTotal > 2
            ? $"{_customProposerName} 邀 請{modeName} 模 式 PK [ {agreedCombat} / {combatTotal} ]\n同 意 {cmdHint} ，標 準 打 !R 或 !no：{_confirmRemainingSeconds} 秒"
            : $"{_customProposerName} 邀 請{modeName} 模 式 PK\n同 意 {cmdHint} ，標 準 打 !R 或 !no：{_confirmRemainingSeconds} 秒";

        string waitingMsg = combatTotal > 2
            ? $"已 邀 請{modeName} 模 式 PK [ {agreedCombat} / {combatTotal} ]\n等 待 對 方 回 應：{_confirmRemainingSeconds} 秒（打 !R 改 回 標 準）"
            : $"已 邀 請{modeName} 模 式 PK\n等 待 對 方 回 應：{_confirmRemainingSeconds} 秒（打 !R 改 回 標 準）";

        ReadOnlySpan<CCSPlayerController> players = CollectionsMarshal.AsSpan(_serverPlayersCache);
        for (int i = 0; i < players.Length; i++)
        {
            var p = players[i];
            if (p is { IsValid: true, Handle: not 0, IsBot: false, IsHLTV: false, TeamNum: 2 or 3, SteamID: > 0 })
            {
                p.PrintToCenter(_customAgreedPlayers.Contains(p.SteamID) ? waitingMsg : pendingMsg);
            }
        }
    }

    private void OnCustomHudTick()
    {
        if (!_hasCustomReservation || _isMatchLive || _isServerShuttingDown)
        {
            KillCustomModeTimers();
            return;
        }

        if (!IsActiveCombatPlayer(_customProposerId))
        {
            CancelCustomModeVote(silent: false, reason: "預 約 發 起 人 已 離 場，指 定 模 式 預 約 已 取 消", centerNotice: "發 起 人 離 場，指 定 模 式 預 約 已 取 消", resumeUnreadyTimer: false);
            return;
        }

        if (!IsBalancedCombatRoster())
        {
            _customHudTimer?.Kill();
            _customHudTimer = null;
            _isAlreadyReadyCountdown = false;
            _confirmRemainingSeconds = 0;
            return;
        }

        if (_isAlreadyReadyCountdown)
        {
            _confirmRemainingSeconds--;
            if (_confirmRemainingSeconds <= 0)
            {
                string modeName = GetCustomModeDisplayName(_reservedCustomPhaseIndex);
                RecordProposerRejectionPenalty(_customProposerId);

                CancelCustomModeVote(silent: true, centerNotice: $"{modeName} 模 式 邀 請 逾 時，進 入 標 準 對 戰", resumeUnreadyTimer: false);
                Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.White}{modeName} 模 式 {ChatColors.Orange}PK 邀 請 已 逾 時，自 動 進 入 標 準 對 戰");

                CheckMatchStart();
                return;
            }

            BroadcastAlreadyReadyCountdownHud();
        }
        else
        {
            BroadcastPassiveInviteHudToUnagreed();
        }
    }

    // 對手進場選完隊伍後延遲 1.5 秒推播，避開黑屏／選隊介面防漏看
    private void ScheduleDelayedJoinNotification()
    {
        _joinNotifyDelayTimer?.Kill();
        _joinNotifyDelayTimer = AddTimer(JoinNotifyDelaySeconds, OnDelayedJoinNotifyFired);
    }

    private void OnDelayedJoinNotifyFired()
    {
        _joinNotifyDelayTimer = null;

        if (!_hasCustomReservation || _isMatchLive || _isServerShuttingDown) return;
        if (!IsActiveCombatPlayer(_customProposerId)) return;
        if (!IsBalancedCombatRoster()) return;

        PruneDepartedCombatPlayers();
        var (combatTotal, readyCombat, agreedCombat) = CountCustomModeCombatStats();

        if (agreedCombat >= combatTotal)
        {
            FinalizeCustomModeAgreement();
            return;
        }

        ClearHudImmediate();
        _wasBalancedInviteActive = true;

        if (readyCombat >= combatTotal)
        {
            if (!_isAlreadyReadyCountdown)
            {
                StartAlreadyReadyCountdown();
            }
            return;
        }

        string modeName = GetCustomModeDisplayName(_reservedCustomPhaseIndex);
        string cmdHint = GetCustomModePrimaryCommand(_reservedCustomPhaseIndex);
        bool notifiedAnyNewPlayer = false;

        ReadOnlySpan<CCSPlayerController> players = CollectionsMarshal.AsSpan(_serverPlayersCache);
        for (int i = 0; i < players.Length; i++)
        {
            var p = players[i];
            if (p is { IsValid: true, Handle: not 0, IsBot: false, IsHLTV: false, TeamNum: 2 or 3, SteamID: > 0 } &&
                !_customAgreedPlayers.Contains(p.SteamID))
            {
                if (_notifiedJoinPlayers.Add(p.SteamID))
                {
                    notifiedAnyNewPlayer = true;
                    p.PrintToChat($" {_cachedPrefix} {ChatColors.Gold}{_customProposerName} {ChatColors.White}已 預 約{ChatColors.Gold}{modeName} 模 式{ChatColors.White} PK ｜ 同 意 打 {ChatColors.Lime}{cmdHint} {ChatColors.White}，標 準 對 戰 打 {ChatColors.Lime}!R{ChatColors.White}（或 {ChatColors.Orange}!no{ChatColors.White}）");
                }
                p.PrintToCenter($"{_customProposerName} 已 預 約{modeName} 模 式 PK\n同 意 打 {cmdHint} ，標 準 對 戰 打 !R（或 !no）");
            }
        }

        if (notifiedAnyNewPlayer)
        {
            var proposer = FindActiveCombatPlayer(_customProposerId);
            if (proposer != null)
            {
                proposer.PrintToChat($" {_cachedPrefix} {ChatColors.Lime}對 手 已 進 場{ChatColors.White}，正 等 待 回 應 您 的 {ChatColors.Gold}{modeName} 模 式{ChatColors.White} 預 約（ 打 {ChatColors.Lime}!R {ChatColors.White}可 改 回 標 準 ）");
                proposer.PrintToCenter($"等 待 回 應 {modeName} 模 式 預 約\n打 !R 可 改 回 標 準 對 戰");
            }
        }

        if (_customHudTimer == null)
        {
            StartPassiveInviteHudTimer();
        }
    }

    private void FinalizeCustomModeAgreement()
    {
        int agreedPhase = _reservedCustomPhaseIndex;
        string modeName = GetCustomModeDisplayName(agreedPhase);

        KillCustomModeTimers();
        _hasCustomReservation = false;
        _reservedCustomPhaseIndex = -1;
        _customProposerId = 0;
        _customProposerName = "";
        _wasBalancedInviteActive = false;
        _isAlreadyReadyCountdown = false;
        _confirmRemainingSeconds = 0;
        _customAgreedPlayers.Clear();
        _notifiedJoinPlayers.Clear();

        // 立刻覆寫中央純文字，避免 0.5 秒開戰延遲期間殘留舊提示
        BroadcastCenterTextToCombatPlayers($"全 員 同 意 {modeName} 模 式 PK 即 將 開 戰");

        // 鎖定指定模式並確保全體參戰玩家皆為已準備
        _isCustomModeLocked = true;
        _lockedCustomPhaseIndex = agreedPhase;

        ReadOnlySpan<CCSPlayerController> players = CollectionsMarshal.AsSpan(_serverPlayersCache);
        for (int i = 0; i < players.Length; i++)
        {
            var p = players[i];
            if (p is { IsValid: true, Handle: not 0, IsBot: false, IsHLTV: false, TeamNum: 2 or 3, SteamID: > 0 })
            {
                MarkPlayerReadySilent(p.SteamID);
            }
        }

        Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Lime}全 員 同 意 本 場 鎖 定 {ChatColors.Gold}{modeName} 模 式{ChatColors.Lime} 搶 {ChatColors.Gold}{Config.MatchSettings.FinalMatchWinScore} {ChatColors.Lime}勝");
        CheckMatchStart();

        // 極端邊界防呆：若因同幀人數突變導致未成功開賽，立即解除鎖定
        if (!_isMatchLive)
        {
            _isCustomModeLocked = false;
            _lockedCustomPhaseIndex = 0;
        }
    }

    private void CancelCustomModeVote(bool silent, string? reason = null, string? centerNotice = null, bool resumeUnreadyTimer = true)
    {
        bool wasActive = _hasCustomReservation || _isAlreadyReadyCountdown;

        KillCustomModeTimers();
        _hasCustomReservation = false;
        _reservedCustomPhaseIndex = -1;
        _customProposerId = 0;
        _customProposerName = "";
        _wasBalancedInviteActive = false;
        _isAlreadyReadyCountdown = false;
        _confirmRemainingSeconds = 0;
        _customAgreedPlayers.Clear();
        _notifiedJoinPlayers.Clear();

        if (wasActive && !_isServerShuttingDown)
        {
            if (resumeUnreadyTimer)
            {
                ResumeUnreadyCheckTimer();
            }
            if (!string.IsNullOrEmpty(centerNotice))
            {
                BroadcastCenterTextToCombatPlayers(centerNotice);
            }
            if (!silent && !string.IsNullOrEmpty(reason))
            {
                Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Orange}{reason}");
            }
        }
    }

    // =========================================================================
    // 與 !R 準備 / 取消準備 / 進離場變動 的連動掛鉤
    // =========================================================================
    // 回傳 true 代表玩家原本已在 _readyPlayers（本次 !R 用於取消預約／維持標準開戰），無須重印「已準備」
    private bool OnCustomModePlayerReady(CCSPlayerController player)
    {
        if (!_hasCustomReservation) return false;

        ulong steamId = player.SteamID;
        string modeName = GetCustomModeDisplayName(_reservedCustomPhaseIndex);
        ulong proposerId = _customProposerId;
        bool isProposer = steamId == proposerId;
        bool wasAlreadyAgreed = _customAgreedPlayers.Contains(steamId);
        bool wasAlreadyReady = _readyPlayers.Contains(steamId);

        // 非發起人（且未曾同意過）選擇 !R 拒絕指定模式 -> 計入發起人冷卻與次數
        if (!isProposer && !wasAlreadyAgreed)
        {
            RecordProposerRejectionPenalty(proposerId);
        }

        CancelCustomModeVote(
            silent: true,
            centerNotice: isProposer ? $"已 取 消 {modeName} 模 式，改 為 標 準 對 戰" : $"選 擇 標 準 對 戰，已 取 消 {modeName} 模 式",
            resumeUnreadyTimer: false);

        if (isProposer)
        {
            Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Gold}{player.PlayerName} {ChatColors.White}已 改 為 標 準 準 備（{ChatColors.Lime}!R{ChatColors.White}），取 消 {ChatColors.Orange}{modeName}{ChatColors.White} 預 約");
        }
        else if (wasAlreadyReady)
        {
            Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Gold}{player.PlayerName} {ChatColors.White}維 持 標 準 對 戰，已 取 消 {ChatColors.Orange}{modeName}{ChatColors.White} 模 式 邀 請");
        }
        else
        {
            Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Gold}{player.PlayerName} {ChatColors.White}選 擇 標 準 對 戰，已 取 消 {ChatColors.Orange}{modeName}{ChatColors.White} 模 式 預 約");
        }

        if (wasAlreadyReady)
        {
            // 發起人取消預約，或原本已 !R 的對手再次輸入 !R -> 立刻檢查開打標準賽
            CheckMatchStart();
            return true;
        }

        // 對手原本尚未 !R -> 回傳 false 交回主程式 HandlePlayerReady 標記已準備並直接觸發 CheckMatchStart
        return false;
    }

    private void OnCustomModePlayerUnready(ulong steamId)
    {
        if (!_hasCustomReservation) return;

        string modeName = GetCustomModeDisplayName(_reservedCustomPhaseIndex);

        if (steamId == _customProposerId)
        {
            CancelCustomModeVote(
                silent: false,
                reason: $"預 約 發 起 人 取 消 準 備，{ChatColors.Lime}{modeName} 模 式{ChatColors.White} 預 約 已 取 消",
                centerNotice: $"發 起 人 取 消 準 備，{modeName} 模 式 已 取 消",
                resumeUnreadyTimer: false);
            return;
        }

        _customAgreedPlayers.Remove(steamId);

        // 若原本正在「全員已 !R」倒數中，有人取消準備則退回一般邀請等待
        if (_isAlreadyReadyCountdown)
        {
            _isAlreadyReadyCountdown = false;
            _confirmRemainingSeconds = 0;
            if (IsBalancedCombatRoster())
            {
                BroadcastPassiveInviteHudToUnagreed();
            }
            else
            {
                KillCustomModeTimers();
            }
        }
    }

    private void OnCustomModeWarmupRosterChanged()
    {
        if (!_hasCustomReservation || _isMatchLive || _isServerShuttingDown) return;

        PruneDepartedCombatPlayers();

        // 1. 發起人離場或進觀戰 -> 立即清除預約
        if (!IsActiveCombatPlayer(_customProposerId))
        {
            string modeName = GetCustomModeDisplayName(_reservedCustomPhaseIndex);
            CancelCustomModeVote(
                silent: false,
                reason: $"預 約 發 起 人 離 場，{ChatColors.Lime}{modeName} 模 式{ChatColors.White} 預 約 已 自 動 取 消",
                centerNotice: $"發 起 人 離 場，{modeName} 模 式 預 約 已 取 消",
                resumeUnreadyTimer: false);
            return;
        }

        var (activeT, activeCT) = CountActiveCombatPlayers();
        int totalPlayers = activeT + activeCT;

        // 2. 不允許 2v2 但人數超過 2 人 -> 取消預約並保留發起人的 !R 狀態
        if (totalPlayers > 2 && !Config.CustomModeSettings.AllowIn2v2)
        {
            CancelCustomModeVote(
                silent: false,
                reason: "場 上 人 數 超 過 1 v 1，指 定 模 式 預 約 已 取 消（保 留 標 準 準 備）",
                centerNotice: "人 數 超 過 1 v 1，改 為 標 準 對 戰",
                resumeUnreadyTimer: false);
            return;
        }

        // 3. 尚未平衡（如對手離場退回 1v0，或 2v1 等第 4 人）-> 停掉推播與倒數，保留發起人預約
        if (!IsBalancedCombatRoster())
        {
            KillCustomModeTimers();
            _isAlreadyReadyCountdown = false;
            _confirmRemainingSeconds = 0;

            if (_wasBalancedInviteActive)
            {
                _wasBalancedInviteActive = false;
                string modeName = GetCustomModeDisplayName(_reservedCustomPhaseIndex);
                var proposer = FindActiveCombatPlayer(_customProposerId);
                if (proposer != null)
                {
                    proposer.PrintToChat($" {_cachedPrefix} {ChatColors.Orange}對 手 離 場 或 人 數 變 動，已 保 留 您 的{ChatColors.Gold}{modeName} 模 式{ChatColors.Orange} 預 約，等 待 玩 家 加 入");
                    proposer.PrintToCenter($"對 手 離 場，已 保 留{modeName} 模 式 預 約");
                }
            }
            return;
        }

        // 4. 達成 1v1 / 2v2 平衡：檢查是否已有尚未通知的新對手，延遲 1.5 秒推播防漏看
        bool hasUnnotifiedPlayer = false;
        ReadOnlySpan<CCSPlayerController> players = CollectionsMarshal.AsSpan(_serverPlayersCache);
        for (int i = 0; i < players.Length; i++)
        {
            var p = players[i];
            if (p is { IsValid: true, Handle: not 0, IsBot: false, IsHLTV: false, TeamNum: 2 or 3, SteamID: > 0 } &&
                !_customAgreedPlayers.Contains(p.SteamID) &&
                !_notifiedJoinPlayers.Contains(p.SteamID))
            {
                hasUnnotifiedPlayer = true;
                break;
            }
        }

        if (hasUnnotifiedPlayer || !_wasBalancedInviteActive)
        {
            ScheduleDelayedJoinNotification();
        }
    }
}
