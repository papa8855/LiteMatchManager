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
    // 【0-Timer 純事件驅動 ClanTag 系統】：攔截 CCSPlayerController::SetClan
    // 完全不需開 Timer 每秒掃描，引擎永遠無法用 Steam 群組標籤覆蓋我們的段位！
    // =========================================================================
    private const uint CustomClanId = 1;
    private MemoryFunctionVoid<CCSPlayerController, uint, string>? _setClanFunc;
    private bool _isSetClanHooked = false;
    private bool _allowInternalSetClan = false;

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
            _setClanFunc = new MemoryFunctionVoid<CCSPlayerController, uint, string>("CCSPlayerController::SetClan");
            _setClanFunc.Hook(OnSetClanPre, HookMode.Pre);
            _isSetClanHooked = true;
            Console.WriteLine("[LiteMatchManager] CCSPlayerController::SetClan 攔截器啟動成功 (0-Timer 常駐段位標籤)！");
        }
        catch (Exception ex)
        {
            _isSetClanHooked = false;
            Console.WriteLine($"[LiteMatchManager] 提示：未讀取到 CCSPlayerController::SetClan 簽名，將使用標準 m_szClan 同步 ({ex.Message})");
        }
    }

    private void UnloadEloClanTagSystem()
    {
        if (_isSetClanHooked && _setClanFunc != null)
        {
            try
            {
                _setClanFunc.Unhook(OnSetClanPre, HookMode.Pre);
            }
            catch { }
            _isSetClanHooked = false;
        }
    }

    // 攔截引擎自發的 Steam 群組標籤刷新 (GC persona data)，只有我們自己呼叫時才放行
    private HookResult OnSetClanPre(DynamicHook hook)
    {
        if (_allowInternalSetClan)
        {
            return HookResult.Continue;
        }
        // 直接阻斷引擎覆蓋標籤 (等同 C++ KHook::Action::Supersede)
        return HookResult.Handled;
    }

    // 將段位文字精簡為適合 CS2 Tab 記分板寬度的格式 (例如 "【 傳奇槍神 S+ 】" -> "傳奇槍神 S+")
    private static string FormatClanTagFromRankTitle(string rawRankTitle)
    {
        if (string.IsNullOrWhiteSpace(rawRankTitle)) return "";
        return rawRankTitle.AsSpan().Trim(" 【】『』─👑💎🔥⚔️🛡️\t\r\n").ToString();
    }

    // 設定單一玩家的記分板段位標籤 (若與目前相同則跳過，避免多餘的網路封包同步)
    private void ApplyPlayerRankClanTag(CCSPlayerController? player)
    {
        if (player is not { IsValid: true, IsBot: false, IsHLTV: false, SteamID: > 0 }) return;

        // 尚未打滿 30 勝定級賽的玩家：_playerRankClanTag 中不會有他，targetTag 為 "" (完全不顯示)
        string targetTag = _playerRankClanTag.TryGetValue(player.SteamID, out string? savedTag) && !string.IsNullOrEmpty(savedTag)
            ? savedTag
            : "";

        string currentTag = player.Clan ?? "";
        if (string.Equals(currentTag, targetTag, StringComparison.Ordinal)) return;

        if (_isSetClanHooked && _setClanFunc != null)
        {
            uint clanId = string.IsNullOrEmpty(targetTag) ? 0u : CustomClanId;
            try
            {
                _allowInternalSetClan = true;
                _setClanFunc.Invoke(player, clanId, targetTag);
            }
            finally
            {
                _allowInternalSetClan = false;
            }
        }

        // 雙重保險：同步更新 m_szClan 並通知狀態改變
        if (player.Clan != targetTag)
        {
            player.Clan = targetTag;
            Utilities.SetStateChanged(player, "CCSPlayerController", "m_szClan");
        }
    }

    // 當玩家連線進入伺服器時，背景自動靜默抓取他的段位並掛上 Tab 記分板 (不印聊天室訊息)
    private void FetchPlayerClanTagOnConnect(CCSPlayerController? player)
    {
        if (player is not { IsValid: true, IsBot: false, IsHLTV: false, SteamID: > 0 }) return;
        if (string.IsNullOrWhiteSpace(Config.StatsApiUrl)) return;

        ulong steamId = player.SteamID;

        // 若記憶體已經有該玩家的段位標籤，直接下一幀套用，免連網！
        if (_playerRankClanTag.ContainsKey(steamId))
        {
            Server.NextFrame(() => ApplyPlayerRankClanTag(player));
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

                if (result.Found && !string.IsNullOrWhiteSpace(result.RankTitle))
                {
                    string cleanTag = FormatClanTagFromRankTitle(result.RankTitle);
                    _playerRankClanTag[steamId] = cleanTag;
                }
                else
                {
                    // 未滿 30 勝定級賽：確保移除、不顯示任何標籤
                    _playerRankClanTag.TryRemove(steamId, out _);
                }

                Server.NextFrame(() =>
                {
                    if (_isServerShuttingDown) return;
                    foreach (var p in _serverPlayersCache)
                    {
                        if (p is { IsValid: true, SteamID: var sid } && sid == steamId)
                        {
                            ApplyPlayerRankClanTag(p);
                            break;
                        }
                    }
                });
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
            Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Gold}{playerName} {ChatColors.White}| 目前段位：{ChatColors.LightBlue}{result.RankTitle} {ChatColors.White}| 初始積分：{ChatColors.Lime}1000");
            Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.White}戰績狀態：{ChatColors.Grey}尚未完成 30 回合單挑定級賽");
        }
        else
        {
            Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Gold}{playerName} {ChatColors.White}| 排名：{ChatColors.Lime}第 {result.RankPos} 名 {ChatColors.White}| 段位：{ChatColors.Gold}{result.RankTitle}");
            Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.White}積分：{ChatColors.Green}{result.Elo} {ChatColors.White}| 戰績：{ChatColors.Lime}{result.Wins}勝 {result.Losses}敗 ({result.WinRate}%) {ChatColors.White}| 爆頭：{ChatColors.Orange}{result.HsRate}%");
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

            // 完賽後雙方皆已有戰績紀錄，立刻寫入最新段位標籤
            if (!string.IsNullOrWhiteSpace(result.WinnerRank))
                _playerRankClanTag[wSteamId] = FormatClanTagFromRankTitle(result.WinnerRank);
            if (!string.IsNullOrWhiteSpace(result.LoserRank))
                _playerRankClanTag[lSteamId] = FormatClanTagFromRankTitle(result.LoserRank);

            Server.NextFrame(() =>
            {
                if (_isServerShuttingDown) return;
                Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Gold}👑 勝方：{ChatColors.Lime}{payload.WinnerName} {ChatColors.Gold}{result.WinnerRank} {ChatColors.White}積分：{ChatColors.Green}{result.WinnerNewElo} (+{result.WinnerDelta} ▲) {ChatColors.Grey}| 爆頭率 {result.WinnerHsRate}%");
                Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Grey}💀 敗方：{ChatColors.White}{payload.LoserName} {ChatColors.LightBlue}{result.LoserRank} {ChatColors.White}積分：{ChatColors.Red}{result.LoserNewElo} (-{result.LoserDelta} ▼) {ChatColors.Grey}| 爆頭率 {result.LoserHsRate}%");

                // 同步刷新場上雙方的記分板標籤
                foreach (var p in _serverPlayersCache)
                {
                    if (p is { IsValid: true, SteamID: var sid } && (sid == wSteamId || sid == lSteamId))
                    {
                        ApplyPlayerRankClanTag(p);
                    }
                }
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

            // 若該玩家已打滿 30 勝上榜 (result.Found)，寫入段位標籤；否則確保不顯示
            if (result.Found && !string.IsNullOrWhiteSpace(result.RankTitle))
            {
                _playerRankClanTag[steamId] = FormatClanTagFromRankTitle(result.RankTitle);
            }
            else
            {
                _playerRankClanTag.TryRemove(steamId, out _);
            }

            Server.NextFrame(() =>
            {
                if (_isServerShuttingDown) return;
                PrintPlayerRankToChat(playerName, result);

                foreach (var p in _serverPlayersCache)
                {
                    if (p is { IsValid: true, SteamID: var sid } && sid == steamId)
                    {
                        ApplyPlayerRankClanTag(p);
                        break;
                    }
                }
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
