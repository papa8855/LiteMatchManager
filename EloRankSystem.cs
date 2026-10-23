using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LiteMatchManager;

// =========================================
// 遠端 PHP Elo API 傳輸與回傳結構
// =========================================
public class EloReportPayload
{
    [JsonPropertyName("api_key")] public string ApiKey { get; set; } = "";
    [JsonPropertyName("map_name")] public string MapName { get; set; } = "";
    [JsonPropertyName("target_score")] public int TargetScore { get; set; } = 30;
    [JsonPropertyName("winner_steamid")] public string WinnerSteamId { get; set; } = "";
    [JsonPropertyName("winner_name")] public string WinnerName { get; set; } = "";
    [JsonPropertyName("winner_score")] public int WinnerScore { get; set; }
    [JsonPropertyName("winner_kills")] public int WinnerKills { get; set; }
    [JsonPropertyName("winner_hs")] public int WinnerHs { get; set; }
    [JsonPropertyName("loser_steamid")] public string LoserSteamId { get; set; } = "";
    [JsonPropertyName("loser_name")] public string LoserName { get; set; } = "";
    [JsonPropertyName("loser_score")] public int LoserScore { get; set; }
    [JsonPropertyName("loser_kills")] public int LoserKills { get; set; }
    [JsonPropertyName("loser_hs")] public int LoserHs { get; set; }
}

public class EloReportResponse
{
    [JsonPropertyName("success")] public bool Success { get; set; }
    [JsonPropertyName("winner_old_elo")] public int WinnerOldElo { get; set; }
    [JsonPropertyName("winner_new_elo")] public int WinnerNewElo { get; set; }
    [JsonPropertyName("winner_delta")] public int WinnerDelta { get; set; }
    [JsonPropertyName("winner_rank")] public string WinnerRank { get; set; } = "";
    [JsonPropertyName("winner_hs_rate")] public double WinnerHsRate { get; set; }
    [JsonPropertyName("loser_old_elo")] public int LoserOldElo { get; set; }
    [JsonPropertyName("loser_new_elo")] public int LoserNewElo { get; set; }
    [JsonPropertyName("loser_delta")] public int LoserDelta { get; set; }
    [JsonPropertyName("loser_rank")] public string LoserRank { get; set; } = "";
    [JsonPropertyName("loser_hs_rate")] public double LoserHsRate { get; set; }
}

public class EloQueryResponse
{
    [JsonPropertyName("success")] public bool Success { get; set; }
    [JsonPropertyName("found")] public bool Found { get; set; }
    [JsonPropertyName("rank_pos")] public int RankPos { get; set; }
    [JsonPropertyName("elo")] public int Elo { get; set; }
    [JsonPropertyName("rank_title")] public string RankTitle { get; set; } = "";
    [JsonPropertyName("wins")] public int Wins { get; set; }
    [JsonPropertyName("losses")] public int Losses { get; set; }
    [JsonPropertyName("win_rate")] public double WinRate { get; set; }
    [JsonPropertyName("hs_rate")] public double HsRate { get; set; }
}

// 使用 partial class 將段位積分與記分板 ClanTag 系統完全獨立出主程式檔案
public partial class LiteMatchManager
{
    // 【優化 4：0-GC JSON 串流解析】共用靜態 JsonSerializerOptions，避免每次反序列化重新建立 Metadata 快取
    private static readonly JsonSerializerOptions SharedJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // 共用靜態 HttpClient (保持長連線 PooledConnection，消除第二次以後的 SSL 交握延遲)
    private static readonly HttpClient SharedHttpClient = CreateHttpClient();
    private static HttpClient CreateHttpClient()
    {
        var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(10),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
            MaxConnectionsPerServer = 4
        };
        var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) };
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36");
        client.DefaultRequestHeaders.ConnectionClose = false; // 強制 Keep-Alive 加速跨國連線
        return client;
    }

    // 儲存玩家的段位標籤 (已滿 30 勝者存設定檔標籤；未滿 30 勝者存 "" 空字串，避免重複發送 HTTP 請求)
    private readonly ConcurrentDictionary<ulong, string> _playerRankClanTag = new();

    // 【優化 3：併發防重鎖】防止同一玩家在網路回應期間重複觸發背景 API 請求
    private readonly ConcurrentDictionary<ulong, byte> _fetchingClanTags = new();

    // 【0-GC 單場擊殺與爆頭計數】：僅在玩家死亡時執行 int +1，對槍過程 0 負擔
    private readonly Dictionary<ulong, int> _matchKills = new(8);
    private readonly Dictionary<ulong, int> _matchHeadshots = new(8);

    // 【優化 5：防第 30 分敗方秒退吞分備份】記錄本場 1v1 雙方的 SteamID 與名稱
    private ulong _backupSteamIdT = 0;
    private string _backupNameT = "";
    private ulong _backupSteamIdCT = 0;
    private string _backupNameCT = "";

    // 【防範 1】：單場結算防重複上傳鎖
    private bool _hasSubmittedElo = false;

    // 【!rank 查詢冷卻紀錄】：每位玩家每 2 分鐘 / 120 秒只能查詢一次
    private const float RankQueryCooldownSeconds = 120.0f;
    private readonly Dictionary<ulong, float> _rankQueryCooldown = new(64);

    // 【極速快取】：將查過的段位暫存於本地記憶體，達成 0 毫秒瞬間顯示
    private readonly ConcurrentDictionary<ulong, (EloQueryResponse Data, float ExpireTime)> _rankMemoryCache = new();

    // =========================================================================
    // 【完美相容 game_fixes 架構】：
    // 徹底移除 MemoryFunctionVoid ("CCSPlayerController::SetClan") 底層 Detour Hook，
    // 改用 1.0 秒輕量巡檢 + 事件即時同步 m_szClan，100% 不與 game_fixes 搶記憶體位址！
    // =========================================================================
    private void InitEloClanTagSystem()
    {
        Console.WriteLine("[LiteMatchManager] 段位 ClanTag 系統啟動成功 (game_fixes 相容零衝突模式)！");
    }

    private void UnloadEloClanTagSystem()
    {
        _playerRankClanTag.Clear();
        _fetchingClanTags.Clear();
    }

    // 【由設定檔決定 ClanTag 顯示名稱】：根據玩家的 Elo 積分，直接回傳 LiteMatchManager.json 中你設定的 ClanTag 文字
    private string GetConfiguredClanTagByElo(int elo)
    {
        if (elo >= 1800) return Config.ClanTag_TierS ?? "";
        if (elo >= 1500) return Config.ClanTag_TierA ?? "";
        if (elo >= 1250) return Config.ClanTag_TierB ?? "";
        if (elo >= 1000) return Config.ClanTag_TierC ?? "";
        return Config.ClanTag_TierD ?? "";
    }

    // 【100% 原生安全寫法】：直接設定 player.Clan 並呼叫 Utilities.SetStateChanged
    private void ApplyPlayerRankClanTag(CCSPlayerController? player)
    {
        if (_isServerShuttingDown) return;
        if (player is not { IsValid: true, Handle: not 0, IsBot: false, IsHLTV: false, Connected: PlayerConnectedState.Connected, SteamID: > 0 })
            return;

        // 尚未打滿 30 勝定級賽的玩家：savedTag 為 ""，targetTag 保持 "" (按 Tab 完全不顯示)
        string targetTag = _playerRankClanTag.TryGetValue(player.SteamID, out string? savedTag) && !string.IsNullOrEmpty(savedTag)
            ? savedTag
            : "";

        if (player.Clan != targetTag)
        {
            player.Clan = targetTag;
            Utilities.SetStateChanged(player, "CCSPlayerController", "m_szClan");
        }
    }

    // 根據 SteamID 從快取清單中安全找出該名玩家並套用標籤 (防止跨執行緒指標失效)
    private void ApplyPlayerRankClanTagBySteamId(ulong steamId)
    {
        if (_isServerShuttingDown || steamId == 0) return;

        foreach (var p in _serverPlayersCache)
        {
            if (p is { IsValid: true, Handle: not 0, IsBot: false, SteamID: var sid } && sid == steamId)
            {
                ApplyPlayerRankClanTag(p);
                break;
            }
        }
    }

    // 當玩家連線進入伺服器時，背景自動靜默抓取他的段位並掛上 Tab 記分板 + 預熱 !rank 快取
    private void FetchPlayerClanTagOnConnect(CCSPlayerController? player)
    {
        if (_isServerShuttingDown) return;
        if (player is not { IsValid: true, Handle: not 0, IsBot: false, IsHLTV: false, SteamID: > 0 }) return;
        if (string.IsNullOrWhiteSpace(Config.StatsApiUrl)) return;

        ulong steamId = player.SteamID;
        float now = Server.CurrentTime;

        if (_playerRankClanTag.ContainsKey(steamId))
        {
            ApplyPlayerRankClanTag(player);
            return;
        }

        if (!_fetchingClanTags.TryAdd(steamId, 1)) return;

        string apiUrl = Config.StatsApiUrl;
        _ = Task.Run(async () =>
        {
            try
            {
                string url = $"{apiUrl}?action=query&steamid={steamId}";
                var result = await SharedHttpClient.GetFromJsonAsync<EloQueryResponse>(url, SharedJsonOptions);
                if (result is not { Success: true }) return;

                _rankMemoryCache[steamId] = (result, now + 120.0f);

                if (result.Found)
                {
                    _playerRankClanTag[steamId] = GetConfiguredClanTagByElo(result.Elo);
                }
                else
                {
                    _playerRankClanTag[steamId] = "";
                }

                Server.NextFrame(() => ApplyPlayerRankClanTagBySteamId(steamId));
            }
            catch { }
            finally
            {
                _fetchingClanTags.TryRemove(steamId, out _);
            }
        });
    }

    // 玩家斷線時清理快取
    private void RemovePlayerClanTagCache(ulong steamId)
    {
        _playerRankClanTag.TryRemove(steamId, out _);
        _fetchingClanTags.TryRemove(steamId, out _);
    }

    // 重置單場擊殺與爆頭統計
    private void ResetEloMatchStats()
    {
        _matchKills.Clear();
        _matchHeadshots.Clear();
        _hasSubmittedElo = false;

        _backupSteamIdT = 0;
        _backupNameT = "";
        _backupSteamIdCT = 0;
        _backupNameCT = "";

        try
        {
            if (!_isServerShuttingDown && !_isMatchLive && Server.CurrentTime < 30.0f)
            {
                _rankQueryCooldown.Clear();
                _rankMemoryCache.Clear();
            }
        }
        catch { }
    }

    // 記錄正賽期間的擊殺與爆頭 (並同步備份 1v1 雙方資訊以防最後一分秒退)
    private void RecordEloKill(CCSPlayerController? attacker, CCSPlayerController? victim, bool isHeadshot)
    {
        if (!_isMatchLive || _isCountdownActive) return;

        if (attacker is { IsValid: true, IsBot: false, SteamID: > 0 } &&
            victim is { IsValid: true } &&
            attacker.SteamID != victim.SteamID)
        {
            ulong aId = attacker.SteamID;
            _matchKills[aId] = _matchKills.GetValueOrDefault(aId) + 1;
            if (isHeadshot)
            {
                _matchHeadshots[aId] = _matchHeadshots.GetValueOrDefault(aId) + 1;
            }

            if (_liveMatchTargetPlayers == 2)
            {
                if (attacker.TeamNum == 2) { _backupSteamIdT = aId; _backupNameT = attacker.PlayerName ?? "Player"; }
                else if (attacker.TeamNum == 3) { _backupSteamIdCT = aId; _backupNameCT = attacker.PlayerName ?? "Player"; }

                if (!victim.IsBot && victim.SteamID > 0)
                {
                    if (victim.TeamNum == 2) { _backupSteamIdT = victim.SteamID; _backupNameT = victim.PlayerName ?? "Player"; }
                    else if (victim.TeamNum == 3) { _backupSteamIdCT = victim.SteamID; _backupNameCT = victim.PlayerName ?? "Player"; }
                }
            }
        }
    }

    // 檢查並觸發 30 勝完賽上傳
    private void TrySubmitEloMatchResult(
        int scoreT,
        int scoreCT,
        int winnerScore,
        int loserScore,
        string winnerName,
        string loserName,
        CCSPlayerController? playerT,
        CCSPlayerController? playerCT)
    {
        if (_hasSubmittedElo ||
            _liveMatchTargetPlayers != 2 ||
            winnerScore < Config.FinalMatchWinScore ||
            scoreT == scoreCT ||
            string.IsNullOrWhiteSpace(Config.StatsApiUrl))
        {
            return;
        }

        ulong steamIdT = playerT is { SteamID: > 0 } ? playerT.SteamID : _backupSteamIdT;
        ulong steamIdCT = playerCT is { SteamID: > 0 } ? playerCT.SteamID : _backupSteamIdCT;

        if (steamIdT == 0 || steamIdCT == 0 || steamIdT == steamIdCT)
        {
            return;
        }

        bool isTWinner = scoreT > scoreCT;
        ulong wSteamId = isTWinner ? steamIdT : steamIdCT;
        ulong lSteamId = isTWinner ? steamIdCT : steamIdT;

        string finalWinnerName = isTWinner
            ? (playerT is not null ? winnerName : (!string.IsNullOrEmpty(_backupNameT) ? _backupNameT : winnerName))
            : (playerCT is not null ? winnerName : (!string.IsNullOrEmpty(_backupNameCT) ? _backupNameCT : winnerName));

        string finalLoserName = isTWinner
            ? (playerCT is not null ? loserName : (!string.IsNullOrEmpty(_backupNameCT) ? _backupNameCT : loserName))
            : (playerT is not null ? loserName : (!string.IsNullOrEmpty(_backupNameT) ? _backupNameT : loserName));

        int wKills = _matchKills.GetValueOrDefault(wSteamId);
        int lKills = _matchKills.GetValueOrDefault(lSteamId);

        if (lKills == 0 && loserScore == 0)
        {
            Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Orange}偵測到敗方本場為 0 擊殺，本局不計入排位積分");
            return;
        }

        _hasSubmittedElo = true;

        _rankMemoryCache.TryRemove(wSteamId, out _);
        _rankMemoryCache.TryRemove(lSteamId, out _);

        var payload = new EloReportPayload
        {
            ApiKey        = Config.StatsApiKey,
            MapName       = Server.MapName ?? "Unknown",
            TargetScore   = Config.FinalMatchWinScore,
            WinnerSteamId = wSteamId.ToString(),
            WinnerName    = finalWinnerName,
            WinnerScore   = winnerScore,
            WinnerKills   = wKills,
            WinnerHs      = _matchHeadshots.GetValueOrDefault(wSteamId),
            LoserSteamId  = lSteamId.ToString(),
            LoserName     = finalLoserName,
            LoserScore    = loserScore,
            LoserKills    = lKills,
            LoserHs       = _matchHeadshots.GetValueOrDefault(lSteamId)
        };

        string apiUrl = Config.StatsApiUrl;
        _ = Task.Run(() => SendEloReportAsync(apiUrl, payload, wSteamId, lSteamId));
    }

    private void PrintPlayerRankToChat(string playerName, EloQueryResponse result)
    {
        if (!result.Found)
        {
            Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Gold}{playerName} {ChatColors.White} | 目前段位：{ChatColors.LightBlue}{result.RankTitle} {ChatColors.White} | 初始積分：{ChatColors.Lime}1000");
            Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.White}戰績狀態：{ChatColors.Grey}尚未完成 30 回合單挑定級賽");
        }
        else
        {
            Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Gold}{playerName} {ChatColors.White} | 排名：{ChatColors.Lime}第 {result.RankPos} 名 {ChatColors.White} | 段位：{ChatColors.Gold}{result.RankTitle}");
            Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.White}積分：{ChatColors.Green}{result.Elo} {ChatColors.White} | 戰績：{ChatColors.Lime}{result.Wins} 勝 {ChatColors.DarkRed} {result.Losses} 敗 {ChatColors.Orange}{result.WinRate} % {ChatColors.White} | 爆頭：{ChatColors.Orange}{result.HsRate} %");
        }
    }

    private void TriggerPlayerRankQuery(CCSPlayerController player)
    {
        if (string.IsNullOrWhiteSpace(Config.StatsApiUrl) || player.SteamID == 0) return;

        ulong sId = player.SteamID;
        float now = Server.CurrentTime;

        if (_rankQueryCooldown.TryGetValue(sId, out float nextAllowedTime))
        {
            float diff = nextAllowedTime - now;
            if (diff > 0f && diff <= RankQueryCooldownSeconds)
            {
                int remainingSeconds = (int)Math.Ceiling(diff);
                player.PrintToChat($" {_cachedPrefix} {ChatColors.Orange}查詢冷卻中，請於 {ChatColors.Lime}{remainingSeconds} 秒{ChatColors.Orange} 後再試");
                return;
            }
        }

        _rankQueryCooldown[sId] = now + RankQueryCooldownSeconds;

        string pName = player.PlayerName ?? "Player";

        if (_rankMemoryCache.TryGetValue(sId, out var cached) && now < cached.ExpireTime && (cached.ExpireTime - now) <= 125.0f)
        {
            PrintPlayerRankToChat(pName, cached.Data);
            ApplyPlayerRankClanTag(player);
            return;
        }

        string apiUrl = Config.StatsApiUrl;
        _ = Task.Run(() => QueryPlayerRankAsync(sId, pName, apiUrl, now));
    }

    private async Task SendEloReportAsync(string apiUrl, EloReportPayload payload, ulong wSteamId, ulong lSteamId)
    {
        try
        {
            using var response = await SharedHttpClient.PostAsJsonAsync(apiUrl, payload, SharedJsonOptions);
            if (!response.IsSuccessStatusCode) return;

            var result = await response.Content.ReadFromJsonAsync<EloReportResponse>(SharedJsonOptions);
            if (result is not { Success: true }) return;

            _playerRankClanTag[wSteamId] = GetConfiguredClanTagByElo(result.WinnerNewElo);
            _playerRankClanTag[lSteamId] = GetConfiguredClanTagByElo(result.LoserNewElo);

            Server.NextFrame(() =>
            {
                if (_isServerShuttingDown) return;
                Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Gold}👑 勝方：{ChatColors.Lime}{payload.WinnerName} {ChatColors.Gold}{result.WinnerRank} {ChatColors.White}積分：{ChatColors.Green}{result.WinnerNewElo} (+{result.WinnerDelta} ▲) {ChatColors.Grey}| 爆頭率 {result.WinnerHsRate}%");
                Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Grey}💀 敗方：{ChatColors.White}{payload.LoserName} {ChatColors.LightBlue}{result.LoserRank} {ChatColors.White}積分：{ChatColors.Red}{result.LoserNewElo} (-{result.LoserDelta} ▼) {ChatColors.Grey}| 爆頭率 {result.LoserHsRate}%");

                ApplyPlayerRankClanTagBySteamId(wSteamId);
                ApplyPlayerRankClanTagBySteamId(lSteamId);
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LiteMatchManager] Elo API Error: {ex.Message}");
        }
    }

    private async Task QueryPlayerRankAsync(ulong steamId, string playerName, string apiUrl, float queryTime)
    {
        try
        {
            string url = $"{apiUrl}?action=query&steamid={steamId}";
            var result = await SharedHttpClient.GetFromJsonAsync<EloQueryResponse>(url, SharedJsonOptions);
            if (result is not { Success: true })
            {
                Server.NextFrame(() => _rankQueryCooldown.Remove(steamId));
                return;
            }

            _rankMemoryCache[steamId] = (result, queryTime + 120.0f);

            _playerRankClanTag[steamId] = result.Found
                ? GetConfiguredClanTagByElo(result.Elo)
                : "";

            Server.NextFrame(() =>
            {
                if (_isServerShuttingDown) return;
                PrintPlayerRankToChat(playerName, result);
                ApplyPlayerRankClanTagBySteamId(steamId);
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LiteMatchManager] Rank Query Error: {ex.Message}");
            Server.NextFrame(() => _rankQueryCooldown.Remove(steamId));
        }
    }
}
