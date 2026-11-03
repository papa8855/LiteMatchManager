using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;

namespace LiteMatchManager;

// =========================================
// AFK 設定與繁體中文語系結構 (對應 LiteMatchManager.json)
// =========================================
public sealed class AFKSettingsConfig
{
    [JsonPropertyName("AFKManagerEnabled")] public bool AFKManagerEnabled { get; set; } = true;
    [JsonPropertyName("AfkPunishAfterWarnings")] public int AfkPunishAfterWarnings { get; set; } = 2;
    [JsonPropertyName("AfkPunishment")] public int AfkPunishment { get; set; } = 2;
    [JsonPropertyName("AfkWarnInterval")] public float AfkWarnInterval { get; set; } = 60.0f;
    [JsonPropertyName("SkipWarmup")] public bool SkipWarmup { get; set; } = false;
    [JsonPropertyName("Timer")] public float Timer { get; set; } = 10.0f;
}

public sealed class LocalizationConfig
{
    [JsonPropertyName("AFK")] public AfkLocalizationConfig AFK { get; set; } = new();
}

public sealed class AfkLocalizationConfig
{
    [JsonPropertyName("ChatPrefix")] public string ChatPrefix { get; set; } = "[ {Green}通 知{default} ] ";
    [JsonPropertyName("ChatKickMessage")] public string ChatKickMessage { get; set; } = "{grey}{playerName}{default} 因 {darkred}閒置 掛機{default} 被 {lime}踢除{default}。";
    [JsonPropertyName("ChatBombTransfer")] public string ChatBombTransfer { get; set; } = "炸彈轉移 → {red}{targetPlayerName}{default}，因為 {red}{playerName}{default} 處於 {red}掛機 狀態{default}。";
    [JsonPropertyName("ChatMoveMessage")] public string ChatMoveMessage { get; set; } = "{grey}{playerName}{default} 因 {darkred}閒置 掛機{default} 被 {lime}移動{default} 到觀戰。";
    [JsonPropertyName("ChatKillMessage")] public string ChatKillMessage { get; set; } = "{grey}{playerName}{default} 因 {darkred}閒置 掛機{default} 被 {lime}擊殺{default}。";
    [JsonPropertyName("ChatWarningKickMessage")] public string ChatWarningKickMessage { get; set; } = "您目前 {lime}閒置 掛機{default} 請移動 ，否則將在 {lime}{timeAmount}{default} 秒後被 踢除{default}";
    [JsonPropertyName("ChatWarningMoveMessage")] public string ChatWarningMoveMessage { get; set; } = "您目前 {lime}閒置 掛機{default} 請移動 ，否則將在 {lime}{timeAmount}{default} 秒後被 踢除{default}";
    [JsonPropertyName("ChatWarningKillMessage")] public string ChatWarningKillMessage { get; set; } = "您目前 {lime}閒置 掛機{default} 請移動 ，否則將在 {lime}{timeAmount}{default} 秒後被 踢除{default}";
    [JsonPropertyName("AntiCampSlayMessage")] public string AntiCampSlayMessage { get; set; } = "{grey}{playerName}{default} 因在 {green}@{zoneName}{default} 區域使用 {lime}{weaponName}{default} 武器露營而被 {lime}擊殺{default}。";
    [JsonPropertyName("AntiCampSlapMessage")] public string AntiCampSlapMessage { get; set; } = "{grey}{playerName}{default} 因在 {green}@{zoneName}{default} 區域使用 {lime}{weaponName}{default} 武器露營而被 {red}給予 {slapAmount} 點傷害{default}。";
    [JsonPropertyName("AntiCampSlayWarningMessage")] public string AntiCampSlayWarningMessage { get; set; } = "停止在 {green}@{zoneName}{default} 區域使用 {lime}{weaponName}{default} 武器露營，否則將在 {lightblue}{timeAmount}{default} 秒後被 {lime}擊殺{default}。";
    [JsonPropertyName("AntiCampSlapWarningMessage")] public string AntiCampSlapWarningMessage { get; set; } = "停止在 {green}@{zoneName}{default} 區域使用 {lime}{weaponName}{default} 武器露營，否則將在 {lightblue}{timeAmount}{default} 秒後被 {red}給予傷害{default}。";
}

public partial class LiteMatchManager
{
    private CCSGameRulesProxy? _afkGameRulesProxyEntity;
    private CCSGameRules? _afkGameRules;
    private CounterStrikeSharp.API.Modules.Timers.Timer? _afkTimer;

    // 預先分配 64 人空間，徹底消除動態擴容與 GC
    private readonly Dictionary<uint, AfkPlayerInfo> _afkPlayerInfo = new(64);

    // 預先編譯好顏色代碼的 AFK 訊息快取，執行期 0 依賴外部語系檔
    private string _cachedAfkKillMsg = "";
    private string _cachedAfkMoveMsg = "";
    private string _cachedAfkKickMsg = "";
    private string _cachedAfkWarnKillMsg = "";
    private string _cachedAfkWarnMoveMsg = "";
    private string _cachedAfkWarnKickMsg = "";

    private sealed class AfkPlayerInfo
    {
        public float AngleX, AngleY, AngleZ;
        public float OriginX, OriginY, OriginZ;
        public bool IsTracking;
        public float AfkTime;
        public int AfkWarningCount;

        public void Reset()
        {
            AfkTime = 0f;
            AfkWarningCount = 0;
            IsTracking = false;
        }
    }

    // 由主程式 OnConfigParsed 呼叫：預先編譯顏色標籤並決定是否啟動計時器
    private void OnAfkConfigParsed(LiteMatchConfig config)
    {
        var loc = config.Localization.AFK;
        string prefix = ReplaceColorTags(loc.ChatPrefix);

        _cachedAfkKillMsg     = prefix + ReplaceColorTags(loc.ChatKillMessage);
        _cachedAfkMoveMsg     = prefix + ReplaceColorTags(loc.ChatMoveMessage);
        _cachedAfkKickMsg     = prefix + ReplaceColorTags(loc.ChatKickMessage);
        _cachedAfkWarnKillMsg = prefix + ReplaceColorTags(loc.ChatWarningKillMessage);
        _cachedAfkWarnMoveMsg = prefix + ReplaceColorTags(loc.ChatWarningMoveMessage);
        _cachedAfkWarnKickMsg = prefix + ReplaceColorTags(loc.ChatWarningKickMessage);

        if (!config.AFKSettings.AFKManagerEnabled || _isMatchLive)
        {
            StopAfkManagerForLiveMatch();
            return;
        }

        StartAfkManagerWarmup();
    }

    // 啟動或重啟暖身期間的 AFK 計時器
    private void StartAfkManagerWarmup()
    {
        _afkTimer?.Kill();
        _afkTimer = null;

        if (!Config.AFKSettings.AFKManagerEnabled || _isMatchLive || _isServerShuttingDown)
            return;

        float interval = Config.AFKSettings.Timer > 0f ? Config.AFKSettings.Timer : 5.0f;
        _afkTimer = AddTimer(interval, AfkTimer_Callback, TimerFlags.REPEAT);
    }

    // 【核心優化】：當載入 LIVE.CFG (正式開賽) 時呼叫，完全停止 AFK 插件運作，0 Tick 負擔
    private void StopAfkManagerForLiveMatch()
    {
        _afkTimer?.Kill();
        _afkTimer = null;

        foreach (var (_, info) in _afkPlayerInfo)
        {
            info.Reset();
        }
    }

    private void UnloadAfkManager()
    {
        _afkTimer?.Kill();
        _afkTimer = null;
        _afkPlayerInfo.Clear();
        _afkGameRulesProxyEntity = null;
        _afkGameRules = null;
    }

    private CCSGameRules? GetSafeAfkGameRules()
    {
        if (_afkGameRulesProxyEntity is { IsValid: true, GameRules: { } rules })
        {
            _afkGameRules = rules;
            return rules;
        }

        _afkGameRulesProxyEntity = null;
        _afkGameRules = null;

        foreach (var entity in Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules"))
        {
            if (entity is { IsValid: true, GameRules: { } validRules })
            {
                _afkGameRulesProxyEntity = entity;
                _afkGameRules = validRules;
                return validRules;
            }
        }
        return null;
    }

    private static void SyncAfkPlayerBaseline(CCSPlayerController player, AfkPlayerInfo data)
    {
        if (player is { IsValid: true, PawnIsAlive: true } && player.PlayerPawn.Value is { IsValid: true } pawn)
        {
            var angles = pawn.EyeAngles;
            var origin = pawn.CBodyComponent?.SceneNode?.AbsOrigin;

            data.AngleX = angles?.X ?? 0f;
            data.AngleY = angles?.Y ?? 0f;
            data.AngleZ = angles?.Z ?? 0f;
            data.OriginX = origin?.X ?? 0f;
            data.OriginY = origin?.Y ?? 0f;
            data.OriginZ = origin?.Z ?? 0f;
            data.IsTracking = true;
        }
    }

    private void InitAfkManager(bool hotReload)
    {
        RegisterListener<Listeners.OnClientConnected>(playerSlot =>
        {
            if (!Config.AFKSettings.AFKManagerEnabled || _isMatchLive) return;

            uint index = (uint)playerSlot + 1;
            ref var info = ref CollectionsMarshal.GetValueRefOrAddDefault(_afkPlayerInfo, index, out bool exists);
            if (!exists || info is null)
            {
                info = new AfkPlayerInfo();
            }
            else
            {
                info.Reset();
            }
        });

        RegisterListener<Listeners.OnClientDisconnectPost>(playerSlot =>
        {
            _afkPlayerInfo.Remove((uint)playerSlot + 1);
        });

        RegisterEventHandler<EventRoundFreezeEnd>((@event, info) =>
        {
            if (!Config.AFKSettings.AFKManagerEnabled || _isMatchLive || _isServerShuttingDown)
                return HookResult.Continue;

            Server.NextFrame(() =>
            {
                if (!Config.AFKSettings.AFKManagerEnabled || _isMatchLive || _isServerShuttingDown) return;

                ReadOnlySpan<CCSPlayerController> players = CollectionsMarshal.AsSpan(_serverPlayersCache);
                for (int i = 0; i < players.Length; i++)
                {
                    var player = players[i];
                    if (player is { IsValid: true, IsBot: false, LifeState: (byte)LifeState_t.LIFE_ALIVE, TeamNum: 2 or 3 })
                    {
                        if (_afkPlayerInfo.TryGetValue(player.Index, out var data) && data is not null)
                        {
                            SyncAfkPlayerBaseline(player, data);
                        }
                    }
                }
            });
            return HookResult.Continue;
        });

        if (hotReload && Config.AFKSettings.AFKManagerEnabled && !_isMatchLive)
        {
            Server.NextFrame(() =>
            {
                GetSafeAfkGameRules();
                StartAfkManagerWarmup();
            });
        }
    }

    private void OnAfkMapStart()
    {
        _afkGameRulesProxyEntity = null;
        _afkGameRules = null;

        foreach (var (_, info) in _afkPlayerInfo)
        {
            info.Reset();
        }

        if (Config.AFKSettings.AFKManagerEnabled && !_isMatchLive)
        {
            AddTimer(1.0f, () =>
            {
                if (!_isServerShuttingDown && !_isMatchLive && Config.AFKSettings.AFKManagerEnabled)
                {
                    GetSafeAfkGameRules();
                    StartAfkManagerWarmup();
                }
            });
        }
    }

    private void OnAfkPlayerSpawn(CCSPlayerController player)
    {
        if (!Config.AFKSettings.AFKManagerEnabled || _isMatchLive || _isServerShuttingDown)
            return;

        if (player is not { IsValid: true, IsBot: false, Connected: PlayerConnectedState.Connected })
            return;

        uint idx = player.Index;
        ref var data = ref CollectionsMarshal.GetValueRefOrAddDefault(_afkPlayerInfo, idx, out bool exists);
        if (!exists || data is null)
        {
            data = new AfkPlayerInfo();
        }

        var capturedData = data;
        AddTimer(0.25f, () =>
        {
            if (!_isMatchLive && !_isServerShuttingDown && Config.AFKSettings.AFKManagerEnabled)
            {
                SyncAfkPlayerBaseline(player, capturedData);
            }
        });
    }

    private void AfkTimer_Callback()
    {
        var afkCfg = Config.AFKSettings;

        // 【核心防線】：一旦進入正賽 (_isMatchLive) 或功能關閉，立刻終止計時器並退出
        if (!afkCfg.AFKManagerEnabled || _isMatchLive || _isServerShuttingDown)
        {
            StopAfkManagerForLiveMatch();
            return;
        }

        var gameRules = GetSafeAfkGameRules();
        if (gameRules is null or { FreezePeriod: true } || (afkCfg.SkipWarmup && gameRules is { WarmupPeriod: true }))
            return;

        ReadOnlySpan<CCSPlayerController> players = CollectionsMarshal.AsSpan(_serverPlayersCache);
        for (int i = 0; i < players.Length; i++)
        {
            var player = players[i];
            if (player is not { IsValid: true, IsBot: false, Connected: PlayerConnectedState.Connected })
                continue;

            if (player.ControllingBot)
                continue;

            uint idx = player.Index;
            ref var data = ref CollectionsMarshal.GetValueRefOrAddDefault(_afkPlayerInfo, idx, out bool exists);
            if (!exists || data is null)
            {
                data = new AfkPlayerInfo();
            }

            if (player is { LifeState: (byte)LifeState_t.LIFE_ALIVE, TeamNum: 2 or 3 })
            {
                if (player.PlayerPawn.Value is not { IsValid: true } playerPawn)
                    continue;

                var angles = playerPawn.EyeAngles;
                var origin = playerPawn.CBodyComponent?.SceneNode?.AbsOrigin;

                float cAngX = angles?.X ?? 0f, cAngY = angles?.Y ?? 0f, cAngZ = angles?.Z ?? 0f;
                float cOrgX = origin?.X ?? 0f, cOrgY = origin?.Y ?? 0f, cOrgZ = origin?.Z ?? 0f;

                if (data.IsTracking &&
                    data.AngleX == cAngX && data.AngleY == cAngY &&
                    data.OriginX == cOrgX && data.OriginY == cOrgY)
                {
                    data.AfkTime += afkCfg.Timer;
                    if (data.AfkTime < afkCfg.AfkWarnInterval)
                        continue;

                    if (data.AfkWarningCount >= afkCfg.AfkPunishAfterWarnings)
                    {
                        string actionTemplate = afkCfg.AfkPunishment switch
                        {
                            0 => _cachedAfkKillMsg,
                            1 => _cachedAfkMoveMsg,
                            _ => _cachedAfkKickMsg
                        };
                        Server.PrintToChatAll(FormatAfkCachedMessage(player, actionTemplate, 0f));

                        if (afkCfg.AfkPunishment == 0)
                            playerPawn.CommitSuicide(false, true);
                        else if (afkCfg.AfkPunishment == 1)
                            player.ChangeTeam(CsTeam.Spectator);
                        else
                            Server.ExecuteCommand($"kickid {player.UserId}");

                        data.AfkWarningCount = 0;
                        data.AfkTime = 0f;
                        continue;
                    }

                    string warnTemplate = afkCfg.AfkPunishment switch
                    {
                        0 => _cachedAfkWarnKillMsg,
                        1 => _cachedAfkWarnMoveMsg,
                        _ => _cachedAfkWarnKickMsg
                    };
                    float remainingTime = (afkCfg.AfkPunishAfterWarnings - data.AfkWarningCount) * afkCfg.AfkWarnInterval;
                    player.PrintToChat(FormatAfkCachedMessage(player, warnTemplate, remainingTime));

                    data.AfkWarningCount++;
                    data.AfkTime = 0f;
                }
                else
                {
                    data.AfkTime = 0f;
                    data.AfkWarningCount = 0;

                    data.AngleX = cAngX; data.AngleY = cAngY; data.AngleZ = cAngZ;
                    data.OriginX = cOrgX; data.OriginY = cOrgY; data.OriginZ = cOrgZ;
                    data.IsTracking = true;
                }
            }
        }
    }

    private static string FormatAfkCachedMessage(CCSPlayerController player, string cachedTemplate, float timeAmount)
    {
        string msg = cachedTemplate.Replace("{playerName}", player.PlayerName ?? "Player");
        return timeAmount > 0f ? msg.Replace("{timeAmount}", $"{timeAmount:F0}") : msg;
    }
}