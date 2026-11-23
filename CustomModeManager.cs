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
    [JsonPropertyName("VoteDurationSeconds")] public int VoteDurationSeconds { get; set; } = 30;
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
    private FrozenSet<string> _pistolModeCommandsSet = FrozenSet<string>.Empty;
    private FrozenSet<string> _sniperModeCommandsSet = FrozenSet<string>.Empty;
    private FrozenSet<string> _rifleModeCommandsSet = FrozenSet<string>.Empty;
    private FrozenSet<string> _rejectModeCommandsSet = FrozenSet<string>.Empty;

    // 正式開戰鎖定狀態
    private bool _isCustomModeLocked = false;
    private int _lockedCustomPhaseIndex = 0;

    // 暖場投票進行中狀態 (純文字倒數，不占用 HTML HUD)
    private bool _isCustomVoteActive = false;
    private int _pendingCustomPhaseIndex = -1;
    private ulong _customVoteProposerId = 0;
    private string _customVoteProposerName = "";
    private int _customVoteRequiredPlayers = 0;
    private int _customVoteRemainingSeconds = 0;
    private readonly HashSet<ulong> _customVoteAgreedPlayers = new(8);
    private readonly HashSet<ulong> _customVoteRosterSnapshot = new(8);
    private CounterStrikeSharp.API.Modules.Timers.Timer? _customVoteTimer;

    // 防洗頻與卡房限制 (凍結 !R 踢除計時但不歸零 + 個人冷卻 + 次數上限；僅於換圖重置，防斷線重連刷次數)
    private readonly Dictionary<ulong, float> _customProposeCooldown = new(64);
    private readonly Dictionary<ulong, int> _customProposalCount = new(64);

    private bool IsCustomModeVoteActive => _isCustomVoteActive;

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
            CancelCustomModeVote(silent: true);
            _isCustomModeLocked = false;
            _lockedCustomPhaseIndex = 0;
        }
    }

    private void UnloadCustomModeManager()
    {
        _customVoteTimer?.Kill();
        _customVoteTimer = null;
        _isCustomVoteActive = false;
        _isCustomModeLocked = false;
        _customVoteAgreedPlayers.Clear();
        _customVoteRosterSnapshot.Clear();
        _customProposeCooldown.Clear();
        _customProposalCount.Clear();
    }

    // 供主程式 ResetMatchState 呼叫；換圖時額外清空個人冷卻與暖場次數上限
    private void ResetCustomModeState(bool clearWarmupQuota = false)
    {
        CancelCustomModeVote(silent: true);
        _isCustomModeLocked = false;
        _lockedCustomPhaseIndex = 0;

        if (clearWarmupQuota)
        {
            _customProposeCooldown.Clear();
            _customProposalCount.Clear();
        }
    }

    // 【聊天室與中央純文字專用】：與 LiteMatchManager.json 分開，獨立控制空格排版
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

    // 【HTML HUD 看板專用】：維持讀取 LiteMatchManager.json 的 Duel_Name，與經典模式一致
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
            player.PrintToChat($" {_cachedPrefix} {ChatColors.Gold}您 無 法 從 旁 觀 者 模 式 發 起 或 參 與 模 式 投 票");
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
                HandleCustomModeVoteOrPropose(player, targetPhase);
            }
        });

        return true;
    }

    private void HandleCustomModeReject(CCSPlayerController player)
    {
        if (!_isCustomVoteActive)
        {
            player.PrintToChat($" {_cachedPrefix} {ChatColors.Grey}目 前 沒 有 進 行 中 的 指 定 模 式 邀 請");
            return;
        }

        string modeName = GetCustomModeDisplayName(_pendingCustomPhaseIndex);
        bool isProposer = player.SteamID == _customVoteProposerId;

        CancelCustomModeVote(silent: true, centerNotice: isProposer ? $"已 取 消 {modeName} 模 式 P K 邀請" : $"對 方 婉 拒 {modeName} 模 式，恢 復 標 準 對 戰");

        if (isProposer)
        {
            Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Gold}{player.PlayerName} {ChatColors.White}取 消 了 {ChatColors.Orange}{modeName}{ChatColors.White} 模 式 邀 請");
        }
        else
        {
            Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Gold}{player.PlayerName} {ChatColors.Orange}拒 絕 了 {ChatColors.White}{modeName} {ChatColors.Orange}模 式，恢 復 標 準 對 戰 等 待");
        }

        // 若場上全員原本都已 !R，婉拒指定模式後立即接續進入標準對戰，絕不卡死
        CheckMatchStart();
    }

    private void HandleCustomModeVoteOrPropose(CCSPlayerController player, int targetPhase)
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

        if (totalPlayers < 2 || activeT != activeCT || activeT > Config.MatchSettings.MaxPlayersPerTeam)
        {
            player.PrintToChat($" {_cachedPrefix} {ChatColors.Orange}雙 方 人 數 尚 未 齊 全，無 法 發 起 指 定 模 式 PK");
            return;
        }

        if (totalPlayers > 2 && !customCfg.AllowIn2v2)
        {
            player.PrintToChat($" {_cachedPrefix} {ChatColors.Orange}目 前 指 定 模 式 PK 僅 限 1 v 1 單 挑 開 放");
            return;
        }

        ulong steamId = player.SteamID;
        string targetModeName = GetCustomModeDisplayName(targetPhase);
        string cmdHint = GetCustomModePrimaryCommand(targetPhase);

        // 情況 A：場上已有進行中的模式提案
        if (_isCustomVoteActive)
        {
            if (targetPhase != _pendingCustomPhaseIndex)
            {
                string currentVoteName = GetCustomModeDisplayName(_pendingCustomPhaseIndex);
                string currentCmd = GetCustomModePrimaryCommand(_pendingCustomPhaseIndex);
                player.PrintToChat($" {_cachedPrefix} 目 前 正 在 投 票 {ChatColors.Gold}{currentVoteName}{ChatColors.White} 模 式，請 輸 入 {ChatColors.Lime}{currentCmd} {ChatColors.White}同 意 或 {ChatColors.Orange}!no {ChatColors.White}拒 絕");
                return;
            }

            if (!_customVoteAgreedPlayers.Add(steamId))
            {
                player.PrintToChat($" {_cachedPrefix} 你 已 同 意 {ChatColors.Gold}{targetModeName}{ChatColors.White} 模 式，等 待 其 他 玩 家 回 應 （ 剩 餘：{ChatColors.Lime}{_customVoteRemainingSeconds} 秒{ChatColors.White} ）");
                return;
            }

            int agreedCount = CountValidAgreedPlayers();
            Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Gold}{player.PlayerName} {ChatColors.White}同 意 {ChatColors.Green}{targetModeName} 模 式{ChatColors.White} PK （ 進 度：{ChatColors.Lime}{agreedCount} / {_customVoteRequiredPlayers}{ChatColors.White} ）");

            if (agreedCount >= _customVoteRequiredPlayers)
            {
                FinalizeCustomModeAgreement();
            }
            else
            {
                BroadcastCustomVoteCenterText();
            }
            return;
        }

        // 情況 B：發起全新模式提案（檢查單次暖場次數上限與個人冷卻）
        int maxProposals = customCfg.MaxProposalsPerWarmup;
        if (maxProposals > 0 && _customProposalCount.TryGetValue(steamId, out int usedCount) && usedCount >= maxProposals)
        {
            player.PrintToChat($" {_cachedPrefix} {ChatColors.Orange}暖 場 指 定 模 式 次 數 已 達 上 限（{maxProposals} 次），請 輸 入 {ChatColors.Lime}!R {ChatColors.Orange}準 備");
            return;
        }

        float now = Server.CurrentTime;
        if (_customProposeCooldown.TryGetValue(steamId, out float nextAllowed) && now < nextAllowed)
        {
            int waitSec = (int)Math.Ceiling(nextAllowed - now);
            player.PrintToChat($" {_cachedPrefix} {ChatColors.Orange}發 起 模 式 邀 請 冷 卻 中，請 於 {ChatColors.Lime}{waitSec} 秒{ChatColors.Orange} 後 再 試");
            return;
        }

        // 開始建立新提案：先清空殘留的暖場 HTML HUD，確保純文字倒數清晰不重疊
        ClearHudImmediate();

        _isCustomVoteActive = true;
        _pendingCustomPhaseIndex = targetPhase;
        _customVoteProposerId = steamId;
        _customVoteProposerName = player.PlayerName ?? "Player";
        _customVoteRequiredPlayers = totalPlayers;
        _customVoteRemainingSeconds = customCfg.VoteDurationSeconds > 0 ? customCfg.VoteDurationSeconds : 30;

        _customVoteAgreedPlayers.Clear();
        _customVoteAgreedPlayers.Add(steamId);

        // 記錄當下參戰名單快照，防止投票中途換人頂替
        _customVoteRosterSnapshot.Clear();
        ReadOnlySpan<CCSPlayerController> players = CollectionsMarshal.AsSpan(_serverPlayersCache);
        for (int i = 0; i < players.Length; i++)
        {
            var p = players[i];
            if (p is { IsValid: true, Handle: not 0, IsBot: false, IsHLTV: false, TeamNum: 2 or 3, SteamID: > 0 })
            {
                _customVoteRosterSnapshot.Add(p.SteamID);
            }
        }

        _customProposalCount[steamId] = _customProposalCount.GetValueOrDefault(steamId) + 1;
        if (customCfg.ProposeCooldownSeconds > 0)
        {
            _customProposeCooldown[steamId] = now + customCfg.ProposeCooldownSeconds;
        }

        // 暫緩發起人 5 秒初始提醒，避免搶佔中央純文字畫面
        _pendingInitialReminders.Remove(steamId);

        string scaleText = totalPlayers == 2 ? "1 v 1" : $"{activeT} v {activeCT}";
        Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Gold}{_customVoteProposerName} {ChatColors.White}發 起 {ChatColors.Lime}{scaleText} {ChatColors.Gold}{targetModeName} 模 式{ChatColors.Lime} {ChatColors.White}PK 邀 請");
        Server.PrintToChatAll($" {_cachedPrefix} 同 意 請 輸 入 {ChatColors.Lime}{cmdHint} {ChatColors.White}，拒 絕 請 輸 入 {ChatColors.Orange}!no");

        BroadcastCustomVoteCenterText();

        _customVoteTimer?.Kill();
        _customVoteTimer = AddTimer(1.0f, OnCustomVoteTick, TimerFlags.REPEAT);
    }

    private int CountValidAgreedPlayers()
    {
        int count = 0;
        ReadOnlySpan<CCSPlayerController> players = CollectionsMarshal.AsSpan(_serverPlayersCache);
        for (int i = 0; i < players.Length; i++)
        {
            var p = players[i];
            if (p is { IsValid: true, Handle: not 0, IsBot: false, IsHLTV: false, TeamNum: 2 or 3 } &&
                _customVoteAgreedPlayers.Contains(p.SteamID))
            {
                count++;
            }
        }
        return count;
    }

    private bool IsCustomVoteRosterStillIntact()
    {
        var (activeT, activeCT) = CountActiveCombatPlayers();
        int totalPlayers = activeT + activeCT;
        if (totalPlayers != _customVoteRequiredPlayers || activeT != activeCT)
            return false;

        int matched = 0;
        ReadOnlySpan<CCSPlayerController> players = CollectionsMarshal.AsSpan(_serverPlayersCache);
        for (int i = 0; i < players.Length; i++)
        {
            var p = players[i];
            if (p is { IsValid: true, Handle: not 0, IsBot: false, IsHLTV: false, TeamNum: 2 or 3, SteamID: > 0 })
            {
                if (!_customVoteRosterSnapshot.Contains(p.SteamID))
                    return false;
                matched++;
            }
        }
        return matched == _customVoteRosterSnapshot.Count;
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

    // 每秒推播一次純文字 PrintToCenter 倒數，不碰 OnTick、不開 HTML
    private void BroadcastCustomVoteCenterText()
    {
        if (!_isCustomVoteActive) return;

        string modeName = GetCustomModeDisplayName(_pendingCustomPhaseIndex);
        string cmdHint = GetCustomModePrimaryCommand(_pendingCustomPhaseIndex);
        int agreedCount = CountValidAgreedPlayers();

        string pendingMsg = _customVoteRequiredPlayers > 2
            ? $"對 方 邀 請【 {modeName} 模 式 】 PK ({agreedCount} / {_customVoteRequiredPlayers})\n同 意 請 輸 入 {cmdHint}，拒 絕 請 輸 入!no：{_customVoteRemainingSeconds} 秒"
            : $"對 方 邀 請【 {modeName} 模 式 】PK\n同 意 請 輸 入 {cmdHint}，拒 絕 請 輸 入!no： {_customVoteRemainingSeconds} 秒";

        string waitingMsg = $"已 同 意【 {modeName} 模 式 】PK ({agreedCount} / {_customVoteRequiredPlayers})，等 待 回 應：{_customVoteRemainingSeconds} 秒";

        ReadOnlySpan<CCSPlayerController> players = CollectionsMarshal.AsSpan(_serverPlayersCache);
        for (int i = 0; i < players.Length; i++)
        {
            var p = players[i];
            if (p is { IsValid: true, Handle: not 0, IsBot: false, IsHLTV: false, TeamNum: 2 or 3 })
            {
                p.PrintToCenter(_customVoteAgreedPlayers.Contains(p.SteamID) ? waitingMsg : pendingMsg);
            }
        }
    }

    private void OnCustomVoteTick()
    {
        if (!_isCustomVoteActive || _isMatchLive || _isServerShuttingDown)
        {
            CancelCustomModeVote(silent: true);
            return;
        }

        if (!IsCustomVoteRosterStillIntact())
        {
            CancelCustomModeVote(silent: false, reason: "場 上 人 數 變 動，指 定 模 式 邀 請 已 自 動 取 消", centerNotice: "人 數 變 動，指 定 模 式 邀 請 已 取 消");
            CheckMatchStart();
            return;
        }

        _customVoteRemainingSeconds--;
        if (_customVoteRemainingSeconds <= 0)
        {
            string modeName = GetCustomModeDisplayName(_pendingCustomPhaseIndex);
            CancelCustomModeVote(silent: true, centerNotice: $"{modeName} 模 式 P K 邀 請 已 逾 時 取 消");
            Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Orange}{ChatColors.White}{modeName} 模 式 {ChatColors.Orange}PK 邀 請 已 逾 時 取 消，恢 復 標 準 對 戰 等 待");

            // 若場上全員原本都已 !R，逾時後立即接續進入標準對戰，絕不卡死
            CheckMatchStart();
            return;
        }

        BroadcastCustomVoteCenterText();
    }

    private void FinalizeCustomModeAgreement()
    {
        int agreedPhase = _pendingCustomPhaseIndex;
        string modeName = GetCustomModeDisplayName(agreedPhase);

        _customVoteTimer?.Kill();
        _customVoteTimer = null;
        _isCustomVoteActive = false;
        _pendingCustomPhaseIndex = -1;
        _customVoteProposerId = 0;
        _customVoteProposerName = "";
        _customVoteRequiredPlayers = 0;
        _customVoteRemainingSeconds = 0;
        _customVoteAgreedPlayers.Clear();
        _customVoteRosterSnapshot.Clear();

        // 立刻覆寫中央純文字，避免 0.5 秒開戰延遲期間殘留舊倒數秒數
        BroadcastCenterTextToCombatPlayers($"全 員 同 意 {modeName} 模 式 P K 即 將 開 戰");

        // 鎖定指定模式並自動將全體參戰玩家設為已準備
        _isCustomModeLocked = true;
        _lockedCustomPhaseIndex = agreedPhase;

        ReadOnlySpan<CCSPlayerController> players = CollectionsMarshal.AsSpan(_serverPlayersCache);
        for (int i = 0; i < players.Length; i++)
        {
            var p = players[i];
            if (p is { IsValid: true, Handle: not 0, IsBot: false, IsHLTV: false, TeamNum: 2 or 3, SteamID: > 0 })
            {
                ulong sid = p.SteamID;
                _readyPlayers.Add(sid);
                _playerUnreadyTime.Remove(sid);
                _pendingInitialReminders.Remove(sid);
            }
        }

        Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Lime}全 員 同 意 本 場 鎖 定 {ChatColors.Gold}{modeName} 模 式{ChatColors.Lime} 搶 {ChatColors.Green}{Config.MatchSettings.FinalMatchWinScore} {ChatColors.Lime}勝");
        CheckMatchStart();

        // 極端邊界防呆：若因同幀人數突變導致未成功開賽，立即解除鎖定避免污染下一場標準 !R
        if (!_isMatchLive)
        {
            _isCustomModeLocked = false;
            _lockedCustomPhaseIndex = 0;
        }
    }

    private void CancelCustomModeVote(bool silent, string? reason = null, string? centerNotice = null)
    {
        bool wasActive = _isCustomVoteActive;

        _customVoteTimer?.Kill();
        _customVoteTimer = null;
        _isCustomVoteActive = false;
        _pendingCustomPhaseIndex = -1;
        _customVoteProposerId = 0;
        _customVoteProposerName = "";
        _customVoteRequiredPlayers = 0;
        _customVoteRemainingSeconds = 0;
        _customVoteAgreedPlayers.Clear();
        _customVoteRosterSnapshot.Clear();

        if (wasActive && !_isServerShuttingDown)
        {
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
    // 與 !R 準備 / 取消準備 / 離場變動 的連動掛鉤
    // =========================================================================
    // 回傳 true 代表發起人原本已 !R、本次打 !R 僅用於撤銷自己的指定模式邀請，無須重印「已準備」提示
    private bool OnCustomModePlayerReady(CCSPlayerController player)
    {
        if (!_isCustomVoteActive) return false;

        ulong steamId = player.SteamID;
        string modeName = GetCustomModeDisplayName(_pendingCustomPhaseIndex);
        string cmdHint = GetCustomModePrimaryCommand(_pendingCustomPhaseIndex);

        // 若發起人自己改打 !R，視同放棄指定模式提案、配合打標準三階段
        if (steamId == _customVoteProposerId)
        {
            CancelCustomModeVote(silent: true, centerNotice: $"發 起 人 已 取 消 {modeName} 模 式，改 為 標 準 對 戰");
            Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Gold}{player.PlayerName} {ChatColors.White}已 改 為 標 準 準 備（!R），取 消 {ChatColors.Orange}{modeName}{ChatColors.White} 邀 請");

            if (_readyPlayers.Contains(steamId))
            {
                CheckMatchStart();
                return true;
            }
            return false;
        }

        // 非發起人打 !R：保留其 !R 狀態（不搶開標準局），並提醒可輸入 !pkX 同意或 !no 拒絕
        player.PrintToChat($" {_cachedPrefix} 目 前 有 {ChatColors.Gold}{modeName} 模 式{ChatColors.White} PK 邀 請，輸 入 {ChatColors.Lime}{cmdHint} {ChatColors.White}同 意 或 {ChatColors.Orange}!no {ChatColors.White}拒 絕");
        return false;
    }

    private void OnCustomModePlayerUnready(ulong steamId)
    {
        if (!_isCustomVoteActive) return;

        if (steamId == _customVoteProposerId || _customVoteAgreedPlayers.Contains(steamId))
        {
            string modeName = GetCustomModeDisplayName(_pendingCustomPhaseIndex);
            CancelCustomModeVote(silent: false, reason: $"玩 家 取 消 準 備，{modeName} 模 式 邀 請 已 自 動 取 消", centerNotice: $"玩 家 取 消 準 備，{modeName} 模 式 邀 請 已 取 消");
        }
    }

    private void OnCustomModeWarmupRosterChanged()
    {
        if (!_isCustomVoteActive || _isMatchLive) return;

        if (!IsCustomVoteRosterStillIntact())
        {
            CancelCustomModeVote(silent: false, reason: "場 上 玩 家 變 動，指 定 模 式 邀 請 已 自 動 取 消", centerNotice: "場 上 玩 家 變 動，指 定 模 式 邀 請 已 取 消");
        }
    }
}
