using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
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

    // =========================================================================
    // 【100% 比照 MatchZy 穩定版 ClanTag 架構】：
    // 只用 SetClanFunc.Hook 阻擋引擎覆寫 (HookResult.Handled)，
    // 絕不呼叫危險的 SetClanFunc.Invoke()，改用原生 player.Clan + SetStateChanged，徹底杜絕閃退！
    // =========================================================================
    public MemoryFunctionVoid<CCSPlayerController, uint, string>? SetClanFunc;
    private bool _isSetClanHooked = false;

    // 儲存已打滿 30 勝上榜玩家的段位標籤 (未滿 30 勝者不存入，保持空標籤)
    private readonly ConcurrentDictionary<ulong, string> _playerRankClanTag = new();

    // 【0-GC 單場擊殺與爆頭計數】：僅在玩家死亡時執行 int +1，對槍過程 0 負擔
    private readonly Dictionary<ulong, int> _matchKills = new(8);
    private readonly Dictionary<ulong, int> _matchHeadshots = new(8);

    // 【防範 1】：單場結算防重複上傳鎖
    private bool _hasSubmittedElo = false;

    // 【新增】：!rank 查詢冷卻紀錄 (每位玩家每 2 分鐘 / 120 秒只能查詢一次)
    private const float RankQueryCooldownSeconds = 120.0f;
    private readonly Dictionary<ulong, float> _rankQueryCooldown = new(64);

    // 【極速快取】：將查過的段位暫存於本地記憶體，達成 0 毫秒瞬間顯示
    private readonly ConcurrentDictionary<ulong, (EloQueryResponse Data, float ExpireTime)> _rankMemoryCache = new();

    // =========================================================================
    // 初始化與卸載 SetClan 記憶體攔截器 (由主程式 Load / Unload 呼叫)
    // =========================================================================
    private void InitEloClanTagSystem()
    {
        try
        {
            SetClanFunc = new MemoryFunctionVoid<CCSPlayerController, uint, string>("CCSPlayerController::SetClan");
            SetClanFunc.Hook(OnSetClanPre, HookMode.Pre);
            _isSetClanHooked = true;
            Console.WriteLine("[LiteMatchManager] CCSPlayerController::SetClan 攔截盾牌啟動成功 (MatchZy 同款安全模式)！");
        }
        catch (Exception ex)
        {
            _isSetClanHooked = false;
            Console.WriteLine($"[LiteMatchManager] 提示：未掛載 CCSPlayerController::SetClan，將使用標準 m_szClan 同步 ({ex.Message})");
        }
    }

    private void UnloadEloClanTagSystem()
    {
        if (_isSetClanHooked && SetClanFunc != null)
        {
            try
            {
                SetClanFunc.Unhook(OnSetClanPre, HookMode.Pre);
            }
            catch { }
            _isSetClanHooked = false;
        }
    }

    // 【與 MatchZy 完全相同】：單純沒收 CS2 引擎修改標籤的權限，不跨 C++ 封裝任何指標，0 閃退風險！
    private HookResult OnSetClanPre(DynamicHook hook)
    {
        return HookResult.Handled;
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

    // 【100% 採用 MatchZy 原生安全寫法】：直接設定 player.Clan 並呼叫 Utilities.SetStateChanged
    private void ApplyPlayerRankClanTag(CCSPlayerController? player)
    {
        if (player is not { IsValid: true, Handle: not 0, IsBot: false, IsHLTV: false, Connected: PlayerConnectedState.Connected, SteamID: > 0 })
            return;

        // 尚未打滿 30 勝定級賽的玩家：_playerRankClanTag 中不會有他，targetTag 為 "" (完全不顯示)
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

    // 當玩家連線進入伺服器時，背景自動靜默抓取他的段位並掛上 Tab 記分板 (不印聊天室訊息)
    private void FetchPlayerClanTagOnConnect(CCSPlayerController? player)
    {
        if (_isServerShuttingDown) return;
        if (player is not { IsValid: true, Handle: not 0, IsBot: false, IsHLTV: false, SteamID: > 0 }) return;
        if (string.IsNullOrWhiteSpace(Config.StatsApiUrl)) return;

        // 先把純數值 ulong steamId 複製出來，絕對不要在背景 Task 裡面碰 CCSPlayerController 指標！
        ulong steamId = player.SteamID;

        // 若記憶體已經有該玩家的段位標籤，直接套用，免連網！
        if (_playerRankClanTag.ContainsKey(steamId))
        {
            ApplyPlayerRankClanTag(player);
            return;
        }

        string apiUrl = Config.StatsApiUrl;
        _ = Task.Run(async () =>
        {
            try
            {
                string url = $"{apiUrl}?action=query&steamid={steamId}";
                string respBody = await SharedHttpClient.GetStringAsync(url);
                var result = JsonSerializer.Deserialize<EloQueryResponse>(respBody);
                if (result is not { Success: true }) return;

                if (result.Found)
                {
                    // 已完成 30 勝定級賽：讀取你在設定檔 LiteMatchManager.json 自訂的 ClanTag 名稱
                    string customTag = GetConfiguredClanTagByElo(result.Elo);
                    if (!string.IsNullOrEmpty(customTag))
                        _playerRankClanTag[steamId] = customTag;
                    else
                        _playerRankClanTag.TryRemove(steamId, out _);
                }
                else
                {
                    // 未滿 30 勝定級賽：確保移除、按 Tab 旁邊什麼都不顯示
                    _playerRankClanTag.TryRemove(steamId, out _);
                }

                Server.NextFrame(() => ApplyPlayerRankClanTagBySteamId(steamId));
            }
            catch { }
        });
    }

    // 玩家斷線時清理快取
    private void RemovePlayerClanTagCache(ulong steamId)
    {
        _playerRankClanTag.TryRemove(steamId, out _);
    }

    // 重置單場擊殺與爆頭統計
    private void ResetEloMatchStats()
    {
        _matchKills.Clear();
        _matchHeadshots.Clear();
        _hasSubmittedElo = false;

        if (Server.CurrentTime < 10.0f)
        {
            _rankQueryCooldown.Clear();
            _rankMemoryCache.Clear();
        }
    }

    // 記錄正賽期間的擊殺與爆頭
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
        }
    }

    // 檢查並觸發 30 勝完賽上傳 (僅限 1v1 且打滿 FinalMatchWinScore)
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
            playerT is null ||
            playerCT is null ||
            string.IsNullOrWhiteSpace(Config.StatsApiUrl))
        {
            return;
        }

        var winnerPlayer = scoreT > scoreCT ? playerT : playerCT;
        var loserPlayer  = scoreT > scoreCT ? playerCT : playerT;

        ulong wSteamId = winnerPlayer.SteamID;
        ulong lSteamId = loserPlayer.SteamID;

        int wKills = _matchKills.GetValueOrDefault(wSteamId);
        int lKills = _matchKills.GetValueOrDefault(lSteamId);

        // 【防範 3：防掛網洗分保護】若敗方整場 0 擊殺且 0 勝分，視為掛網送頭局，不予計算積分
        if (lKills == 0 && loserScore == 0)
        {
            Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Orange}偵測到敗方本場為 0 擊殺，本局不計入排位積分");
            return;
        }

        _hasSubmittedElo = true; // 鎖定本場已上傳，防止重複觸發

        // 完賽時清空雙方本地快取，確保下次查詢拿最新分數
        _rankMemoryCache.TryRemove(wSteamId, out _);
        _rankMemoryCache.TryRemove(lSteamId, out _);

        var payload = new EloReportPayload
        {
            ApiKey        = Config.StatsApiKey,
            MapName       = Server.MapName ?? "Unknown",
            TargetScore   = Config.FinalMatchWinScore,
            WinnerSteamId = wSteamId.ToString(),
            WinnerName    = winnerName,
            WinnerScore   = winnerScore,
            WinnerKills   = wKills,
            WinnerHs      = _matchHeadshots.GetValueOrDefault(wSteamId),
            LoserSteamId  = lSteamId.ToString(),
            LoserName     = loserName,
            LoserScore    = loserScore,
            LoserKills    = lKills,
            LoserHs       = _matchHeadshots.GetValueOrDefault(lSteamId)
        };

        string apiUrl = Config.StatsApiUrl;
        _ = Task.Run(() => SendEloReportAsync(apiUrl, payload, wSteamId, lSteamId));
    }

    // 統一印出雙排段位資訊
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
            Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.White}積分：{ChatColors.Green}{result.Elo} {ChatColors.White} | 戰績：{ChatColors.Lime}{result.Wins} 勝 {ChatColors.DarkRed} {result.Losses} 敗 ( {result.WinRate} %) {ChatColors.White} | 爆頭：{ChatColors.Orange}{result.HsRate} %");
        }
    }

    // 觸發玩家 !rank / !elo 指令查詢 (含 2 分鐘冷卻判斷 + 本地極速快取)
    private void TriggerPlayerRankQuery(CCSPlayerController player)
    {
        if (string.IsNullOrWhiteSpace(Config.StatsApiUrl) || player.SteamID == 0) return;

        ulong sId = player.SteamID;
        float now = Server.CurrentTime;

        // 檢查是否仍在 2 分鐘 (120 秒) 冷卻時間內
        if (_rankQueryCooldown.TryGetValue(sId, out float nextAllowedTime) && now < nextAllowedTime)
        {
            int remainingSeconds = (int)Math.Ceiling(nextAllowedTime - now);
            player.PrintToChat($" {_cachedPrefix} {ChatColors.Orange}查詢冷卻中，請於 {ChatColors.Lime}{remainingSeconds} 秒{ChatColors.Orange} 後再試");
            return;
        }

        // 先寫入冷卻鎖 (若網路失敗會在 catch 中自動退還)
        _rankQueryCooldown[sId] = now + RankQueryCooldownSeconds;

        string pName = player.PlayerName ?? "Player";

        // 若本地記憶體快取內有新鮮資料，直接 0 毫秒瞬間印出，不用等遠端 iFastNet！
        if (_rankMemoryCache.TryGetValue(sId, out var cached) && now < cached.ExpireTime)
        {
            PrintPlayerRankToChat(pName, cached.Data);
            ApplyPlayerRankClanTag(player);
            return;
        }

        string apiUrl = Config.StatsApiUrl;
        _ = Task.Run(() => QueryPlayerRankAsync(sId, pName, apiUrl, now));
    }

    // 【背景異步上傳】：不阻塞 CS2 主執行緒，收到 iFastNet 回傳後於 NextFrame 印出段位並即時更新雙方記分板 ClanTag
    private async Task SendEloReportAsync(string apiUrl, EloReportPayload payload, ulong wSteamId, ulong lSteamId)
    {
        try
        {
            string json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await SharedHttpClient.PostAsync(apiUrl, content);
            if (!response.IsSuccessStatusCode) return;

            string respBody = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<EloReportResponse>(respBody);
            if (result is not { Success: true }) return;

            // 完賽後雙方皆已完成定級，依照最新 Elo 分數直接套用 LiteMatchManager.json 自訂的 ClanTag 名稱
            _playerRankClanTag[wSteamId] = GetConfiguredClanTagByElo(result.WinnerNewElo);
            _playerRankClanTag[lSteamId] = GetConfiguredClanTagByElo(result.LoserNewElo);

            Server.NextFrame(() =>
            {
                if (_isServerShuttingDown) return;
                Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Gold}👑 勝方：{ChatColors.Lime}{payload.WinnerName} {ChatColors.Gold}{result.WinnerRank} {ChatColors.White}積分：{ChatColors.Green}{result.WinnerNewElo} (+{result.WinnerDelta} ▲) {ChatColors.Grey}| 爆頭率 {result.WinnerHsRate}%");
                Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Grey}💀 敗方：{ChatColors.White}{payload.LoserName} {ChatColors.LightBlue}{result.LoserRank} {ChatColors.White}積分：{ChatColors.Red}{result.LoserNewElo} (-{result.LoserDelta} ▼) {ChatColors.Grey}| 爆頭率 {result.LoserHsRate}%");

                // 同步刷新場上雙方的記分板標籤
                ApplyPlayerRankClanTagBySteamId(wSteamId);
                ApplyPlayerRankClanTagBySteamId(lSteamId);
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LiteMatchManager] Elo API Error: {ex.Message}");
        }
    }

    // 【背景異步查詢】：處理玩家輸入 !rank / !elo 查詢個人段位 (雙排整齊顯示版 + 失敗退還冷卻 + 同步記分板標籤)
    private async Task QueryPlayerRankAsync(ulong steamId, string playerName, string apiUrl, float queryTime)
    {
        try
        {
            string url = $"{apiUrl}?action=query&steamid={steamId}";
            string respBody = await SharedHttpClient.GetStringAsync(url);
            var result = JsonSerializer.Deserialize<EloQueryResponse>(respBody);
            if (result is not { Success: true })
            {
                Server.NextFrame(() => _rankQueryCooldown.Remove(steamId));
                return;
            }

            // 存入快取 120 秒
            _rankMemoryCache[steamId] = (result, queryTime + 120.0f);

            // 若該玩家已打滿 30 勝上榜 (result.Found)，依照 Elo 套用設定檔自訂的 ClanTag；否則確保不顯示
            if (result.Found)
            {
                string customTag = GetConfiguredClanTagByElo(result.Elo);
                if (!string.IsNullOrEmpty(customTag))
                    _playerRankClanTag[steamId] = customTag;
                else
                    _playerRankClanTag.TryRemove(steamId, out _);
            }
            else
            {
                _playerRankClanTag.TryRemove(steamId, out _);
            }

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
            // 【防範 2】：若遠端連線逾時，自動退還玩家的冷卻時間
            Server.NextFrame(() => _rankQueryCooldown.Remove(steamId));
        }
    }
}
