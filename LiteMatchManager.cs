using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
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
// 遠端 PHP Elo API 傳輸與回傳結構 (同時支援 1v1 與 2v2)
// =========================================
public class EloPlayerMatchStat
{
    [JsonPropertyName("steamid")] public string SteamId { get; set; } = "";
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("kills")] public int Kills { get; set; }
    [JsonPropertyName("deaths")] public int Deaths { get; set; }
    [JsonPropertyName("hs")] public int Hs { get; set; }
    [JsonPropertyName("online")] public bool Online { get; set; } = true;
}

public class EloPlayerResult
{
    [JsonPropertyName("steamid")] public string SteamId { get; set; } = "";
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("old_elo")] public int OldElo { get; set; }
    [JsonPropertyName("new_elo")] public int NewElo { get; set; }
    [JsonPropertyName("delta")] public int Delta { get; set; }
    [JsonPropertyName("rank")] public string Rank { get; set; } = "";
    [JsonPropertyName("hs_rate")] public double HsRate { get; set; }
}

public class EloReportPayload
{
    [JsonPropertyName("api_key")] public string ApiKey { get; set; } = "";
    [JsonPropertyName("map_name")] public string MapName { get; set; } = "";
    [JsonPropertyName("mode")] public string Mode { get; set; } = "1v1";
    [JsonPropertyName("target_score")] public int TargetScore { get; set; } = 30;
    [JsonPropertyName("winner_score")] public int WinnerScore { get; set; }
    [JsonPropertyName("loser_score")] public int LoserScore { get; set; }

    // 1v1 傳統欄位 (保持向後相容)
    [JsonPropertyName("winner_steamid")] public string WinnerSteamId { get; set; } = "";
    [JsonPropertyName("winner_name")] public string WinnerName { get; set; } = "";
    [JsonPropertyName("winner_kills")] public int WinnerKills { get; set; }
    [JsonPropertyName("winner_hs")] public int WinnerHs { get; set; }
    [JsonPropertyName("loser_steamid")] public string LoserSteamId { get; set; } = "";
    [JsonPropertyName("loser_name")] public string LoserName { get; set; } = "";
    [JsonPropertyName("loser_kills")] public int LoserKills { get; set; }
    [JsonPropertyName("loser_hs")] public int LoserHs { get; set; }

    // 1v1 & 2v2 通用雙陣營玩家清單
    [JsonPropertyName("winners")] public List<EloPlayerMatchStat> Winners { get; set; } = [];
    [JsonPropertyName("losers")] public List<EloPlayerMatchStat> Losers { get; set; } = [];
}

public class EloReportResponse
{
    [JsonPropertyName("success")] public bool Success { get; set; }
    [JsonPropertyName("mode")] public string Mode { get; set; } = "1v1";

    // 1v1 回傳欄位
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

    // 2v2 / 多人回傳清單
    [JsonPropertyName("winners")] public List<EloPlayerResult>? Winners { get; set; }
    [JsonPropertyName("losers")] public List<EloPlayerResult>? Losers { get; set; }
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

// 【新增】：!top / !排行 查詢前三名專用回傳結構
public class EloTopPlayerItem
{
    [JsonPropertyName("rank_pos")] public int RankPos { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("elo")] public int Elo { get; set; }
    [JsonPropertyName("rank_title")] public string RankTitle { get; set; } = "";
}

public class EloTopResponse
{
    [JsonPropertyName("success")] public bool Success { get; set; }
    [JsonPropertyName("top_players")] public List<EloTopPlayerItem>? TopPlayers { get; set; }
}

// 使用 partial class 將段位積分與記分板 ClanTag 系統完全獨立出主程式檔案
public partial class LiteMatchManager
{
    // 【優化 4：0-GC JSON 串流解析】共用靜態 JsonSerializerOptions，避免每次反序列化重新建立 Metadata 快取
    private static readonly JsonSerializerOptions SharedJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // 【核心修復】：修正 SocketsHttpHandler 連線池參數，防止遠端主機 (Apache/Nginx/iFastNet) 提早掐斷 Keep-Alive 導致 POST 撞上死連線報錯
    private static readonly HttpClient SharedHttpClient = CreateHttpClient();
    private static HttpClient CreateHttpClient()
    {
        var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            PooledConnectionIdleTimeout = TimeSpan.FromSeconds(15),
            MaxConnectionsPerServer = 6,
            ConnectTimeout = TimeSpan.FromSeconds(5)
        };
        var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36");
        return client;
    }

    // =========================================================================
    // 【100% 比照 MatchZy 穩定版 ClanTag 架構】：
    // 只用 SetClanFunc.Hook 阻擋引擎覆寫 (HookResult.Handled)，
    // 絕不呼叫危險的 SetClanFunc.Invoke()，改用原生 player.Clan + SetStateChanged，徹底杜絕閃退！
    // =========================================================================
    public MemoryFunctionVoid<CCSPlayerController, uint, string>? SetClanFunc;
    private bool _isSetClanHooked = false;

    // 儲存玩家的段位標籤 (已滿 30 勝者存設定檔標籤；未滿 30 勝者存 "" 空字串，避免重複發送 HTTP 請求)
    private readonly ConcurrentDictionary<ulong, string> _playerRankClanTag = new();

    // 【優化 3：併發防重鎖】防止同一玩家在網路回應期間重複觸發背景 API 請求
    private readonly ConcurrentDictionary<ulong, byte> _fetchingClanTags = new();

    // 【0-GC 單場擊殺、死亡與爆頭計數】：僅在玩家死亡時執行 int +1，對槍過程 0 負擔
    private readonly Dictionary<ulong, int> _matchKills = new(8);
    private readonly Dictionary<ulong, int> _matchDeaths = new(8);
    private readonly Dictionary<ulong, int> _matchHeadshots = new(8);

    // 【優化 5：支援 1v1 與 2v2 防敗方秒退吞分備份】記錄本場參賽者的隊伍與名稱
    private readonly Dictionary<ulong, (int Team, string Name)> _matchParticipants = new(8);

    // 【新增防弊】：記錄「開賽時正式按準備(Ready)的先發玩家」，用來區分先發與中途補位路人
    private readonly HashSet<ulong> _initialStarters = new(8);

    // 【防範 1】：單場結算防重複上傳鎖
    private bool _hasSubmittedElo = false;

    // 【!rank 查詢冷卻紀錄】：每位玩家每 60 秒只能查詢一次
    private const float RankQueryCooldownSeconds = 60.0f;
    private readonly Dictionary<ulong, float> _rankQueryCooldown = new(64);

    // 【極速快取】：將查過的段位暫存於本地記憶體，達成 0 毫秒瞬間顯示
    private readonly ConcurrentDictionary<ulong, (EloQueryResponse Data, float ExpireTime)> _rankMemoryCache = new();

    // 【新增：!top / !排行 查詢冷卻與本地快取】
    private readonly Dictionary<ulong, float> _topQueryCooldown = new(64);
    private List<EloTopPlayerItem>? _topMemoryCache = null;
    private float _topCacheExpireTime = 0f;

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

    private void RemovePlayerClanTagCache(ulong steamId)
    {
        _playerRankClanTag.TryRemove(steamId, out _);
        _fetchingClanTags.TryRemove(steamId, out _);
    }

    private void ResetEloMatchStats()
    {
        _matchKills.Clear();
        _matchDeaths.Clear();
        _matchHeadshots.Clear();
        _matchParticipants.Clear();
        _initialStarters.Clear();
        _hasSubmittedElo = false;

        // 若此時正要進入正賽 (_isMatchLive == true)，預先將場上所有已準備參賽者寫入名冊備份並標記為先發
        if (_isMatchLive)
        {
            foreach (var p in _serverPlayersCache)
            {
                if (p is { IsValid: true, Handle: not 0, IsBot: false, IsHLTV: false, SteamID: > 0, TeamNum: 2 or 3 })
                {
                    if (_readyPlayers.Contains(p.SteamID))
                    {
                        _matchParticipants[p.SteamID] = (p.TeamNum, p.PlayerName ?? "Player");
                        _initialStarters.Add(p.SteamID);
                    }
                }
            }
        }

        if (!_isMatchLive && Server.CurrentTime < 30.0f)
        {
            _rankQueryCooldown.Clear();
            _rankMemoryCache.Clear();
            _topQueryCooldown.Clear();
            _topMemoryCache = null;
            _topCacheExpireTime = 0f;
        }
    }

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

            if (attacker.TeamNum is 2 or 3)
            {
                _matchParticipants[aId] = (attacker.TeamNum, attacker.PlayerName ?? "Player");
            }

            if (!victim.IsBot && victim.SteamID > 0)
            {
                ulong vId = victim.SteamID;
                _matchDeaths[vId] = _matchDeaths.GetValueOrDefault(vId) + 1;

                if (victim.TeamNum is 2 or 3)
                {
                    _matchParticipants[vId] = (victim.TeamNum, victim.PlayerName ?? "Player");
                }
            }
        }
    }

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
            (_liveMatchTargetPlayers != 2 && _liveMatchTargetPlayers != 4) ||
            winnerScore < Config.FinalMatchWinScore ||
            scoreT == scoreCT ||
            string.IsNullOrWhiteSpace(Config.StatsApiUrl))
        {
            return;
        }

        // 建立結算當下「仍在線上且在 T/CT 隊伍中」的玩家字典
        var onlineTeamMap = new Dictionary<ulong, (int Team, string Name)>(8);
        foreach (var p in _serverPlayersCache)
        {
            if (p is { IsValid: true, Handle: not 0, IsBot: false, IsHLTV: false, Connected: PlayerConnectedState.Connected, SteamID: > 0, TeamNum: 2 or 3 })
            {
                onlineTeamMap[p.SteamID] = (p.TeamNum, p.PlayerName ?? "Player");

                // 【核心修復 1】：不要隨便把剛進場 0 殺 0 死的路人加進 _matchParticipants！
                // 只有當他是「開賽先發玩家」或者「已經在場上參與至少 3 次交戰 (Kills+Deaths >= 3)」的實質補位玩家，才更新進參賽名冊
                int engagements = _matchKills.GetValueOrDefault(p.SteamID) + _matchDeaths.GetValueOrDefault(p.SteamID);
                if (_initialStarters.Contains(p.SteamID) || engagements >= 3)
                {
                    _matchParticipants[p.SteamID] = (p.TeamNum, p.PlayerName ?? "Player");
                }
            }
        }

        bool isTWinner = scoreT > scoreCT;
        int winTeam = isTWinner ? 2 : 3;
        int loseTeam = isTWinner ? 3 : 2;
        int maxPerTeam = _liveMatchTargetPlayers == 4 ? 2 : 1;

        var winnersList = new List<EloPlayerMatchStat>(4);
        var losersList = new List<EloPlayerMatchStat>(4);

        foreach (var (sid, info) in _matchParticipants)
        {
            if (info.Team != winTeam && info.Team != loseTeam) continue;

            int k = _matchKills.GetValueOrDefault(sid);
            int d = _matchDeaths.GetValueOrDefault(sid);
            int hs = _matchHeadshots.GetValueOrDefault(sid);
            int engagements = k + d;

            // 檢查該玩家在結算當下是否仍在該隊伍線上
            bool isCurrentlyOnline = onlineTeamMap.TryGetValue(sid, out var curInfo) && curInfo.Team == info.Team;
            bool isStarter = _initialStarters.Contains(sid);

            // 【核心修復 2】：如果不是開賽先發，而是中途進來補位的玩家 (如 C 玩家)，
            // 他的總交戰數 (Kills + Deaths) 必須 >= 4 (代表至少實質打了 4 回合以上)，否則視為尾盤湊數補位，不加減分！
            if (!isStarter && engagements < 10)
            {
                continue;
            }

            if (info.Team == winTeam)
            {
                // 【核心修復 3】：獲勝方的玩家 (如 A 玩家) 如果中途離線跳車 (結算時不在線上)，直接取消他的加分資格！
                if (!isCurrentlyOnline)
                {
                    continue;
                }

                winnersList.Add(new EloPlayerMatchStat
                {
                    SteamId = sid.ToString(),
                    Name = info.Name,
                    Kills = k,
                    Deaths = d,
                    Hs = hs,
                    Online = true
                });
            }
            else
            {
                // 落敗方：無論還在線上，或是先發玩家打到一半見苗頭不對秒退跳車 (isStarter)，通通保留在敗方名單扣分！
                losersList.Add(new EloPlayerMatchStat
                {
                    SteamId = sid.ToString(),
                    Name = info.Name,
                    Kills = k,
                    Deaths = d,
                    Hs = hs,
                    Online = isCurrentlyOnline
                });
            }
        }

        // 【核心修復 4 (0-GC 原生排序與裁剪)】：若名單超過隊伍上限，使用 List.Sort 原地排序並裁剪，0 記憶體垃圾！
        if (winnersList.Count > maxPerTeam)
        {
            winnersList.Sort(static (a, b) =>
            {
                int cmp = b.Online.CompareTo(a.Online);
                if (cmp != 0) return cmp;
                cmp = (b.Kills + b.Deaths).CompareTo(a.Kills + a.Deaths);
                return cmp != 0 ? cmp : b.Kills.CompareTo(a.Kills);
            });
            winnersList.RemoveRange(maxPerTeam, winnersList.Count - maxPerTeam);
        }

        if (losersList.Count > maxPerTeam)
        {
            losersList.Sort(static (a, b) =>
            {
                int cmp = (b.Kills + b.Deaths).CompareTo(a.Kills + a.Deaths);
                return cmp != 0 ? cmp : b.Deaths.CompareTo(a.Deaths);
            });
            losersList.RemoveRange(maxPerTeam, losersList.Count - maxPerTeam);
        }

        if (winnersList.Count == 0 || losersList.Count == 0) return;

        // 注意：若 2v2 勝方有 1 人中離且補位者未滿 4 回合，允許剩下一名孤軍奮戰獲勝的隊友 (winnersList.Count >= 1) 照常領取勝利積分！
        if (_liveMatchTargetPlayers == 4 && (winnersList.Count < 1 || losersList.Count < 1))
        {
            return;
        }

        // 【0-GC 原生迴圈加總】：取代 LINQ 的 .Sum()
        int totalWinnerKills = 0;
        for (int i = 0; i < winnersList.Count; i++) totalWinnerKills += winnersList[i].Kills;

        int totalLoserKills = 0;
        for (int i = 0; i < losersList.Count; i++) totalLoserKills += losersList[i].Kills;

        if ((totalLoserKills < 3 && loserScore <= 1) ||
            (_liveMatchTargetPlayers == 4 && totalLoserKills < 5 && loserScore <= 1))
        {
            Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Orange}偵 測 到 本 場 無 效 數 據，本 場 不 計 入 排 位 積 分");
            return;
        }

        _hasSubmittedElo = true;

        foreach (var w in winnersList) if (ulong.TryParse(w.SteamId, out ulong id)) _rankMemoryCache.TryRemove(id, out _);
        foreach (var l in losersList) if (ulong.TryParse(l.SteamId, out ulong id)) _rankMemoryCache.TryRemove(id, out _);
        _topMemoryCache = null;
        _topCacheExpireTime = 0f;

        var firstWinner = winnersList[0];
        var firstLoser = losersList[0];

        var payload = new EloReportPayload
        {
            ApiKey        = Config.StatsApiKey,
            MapName       = Server.MapName ?? "Unknown",
            Mode          = _liveMatchTargetPlayers == 4 ? "2v2" : "1v1",
            TargetScore   = Config.FinalMatchWinScore,
            WinnerScore   = winnerScore,
            LoserScore    = loserScore,
            WinnerSteamId = firstWinner.SteamId,
            WinnerName    = firstWinner.Name,
            WinnerKills   = firstWinner.Kills,
            WinnerHs      = firstWinner.Hs,
            LoserSteamId  = firstLoser.SteamId,
            LoserName     = firstLoser.Name,
            LoserKills    = firstLoser.Kills,
            LoserHs       = firstLoser.Hs,
            Winners       = winnersList,
            Losers        = losersList
        };

        string apiUrl = Config.StatsApiUrl;
        _ = Task.Run(() => SendEloReportAsync(apiUrl, payload));
    }

    // 統一印出雙排段位資訊 (私密聊天 + 畫面正中間 HUD 顯示)
    private void PrintPlayerRankToPrivateChat(CCSPlayerController player, string playerName, EloQueryResponse result)
    {
        if (!result.Found)
        {
            player.PrintToChat($" {_cachedPrefix} {ChatColors.Gold}{playerName} {ChatColors.White} | 目前段位：{ChatColors.LightBlue}{result.RankTitle} {ChatColors.White} | 初始積分：{ChatColors.Lime}900");
            player.PrintToChat($" {_cachedPrefix} {ChatColors.White}戰績狀態：{ChatColors.Grey}尚未完成 30 回合排位定級賽");
            
            // 同步顯示在畫面中間 HUD
            player.PrintToCenter($"段位：尚未定級 (30回合定級賽中)");
        }
        else
        {
            player.PrintToChat($" {_cachedPrefix} {ChatColors.Gold}{playerName} {ChatColors.White} | 排名：{ChatColors.Lime}第 {result.RankPos} 名 {ChatColors.White} | 段位：{ChatColors.Gold}{result.RankTitle}");
            player.PrintToChat($" {_cachedPrefix} {ChatColors.White}積分：{ChatColors.Green}{result.Elo} {ChatColors.White} | 戰績：{ChatColors.Lime}{result.Wins} 勝 {ChatColors.DarkRed} {result.Losses} 敗 {ChatColors.Orange}{result.WinRate} % {ChatColors.White} | 爆頭：{ChatColors.Orange}{result.HsRate} %");
            
            // 同步顯示在畫面中間 HUD (帶有排名與積分)
            player.PrintToCenter($"你 的 段 位：{result.RankTitle} | 積 分：{result.Elo} 分");
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
            PrintPlayerRankToPrivateChat(player, pName, cached.Data);
            ApplyPlayerRankClanTag(player);
            return;
        }

        string apiUrl = Config.StatsApiUrl;
        _ = Task.Run(() => QueryPlayerRankAsync(sId, pName, apiUrl, now));
    }

    // 【修改】：統一印出前三名排行資訊 (加入積分顯示)
    private void PrintTopPlayersToPrivateChat(CCSPlayerController player, List<EloTopPlayerItem>? topPlayers)
    {
        if (topPlayers == null || topPlayers.Count == 0)
        {
            player.PrintToChat($" {_cachedPrefix} {ChatColors.Grey}目前尚無排位完賽紀錄");
            return;
        }

        foreach (var item in topPlayers)
        {
            player.PrintToChat($" {_cachedPrefix} {ChatColors.Gold}{item.Name} {ChatColors.White}| 排名：{ChatColors.Lime}第 {item.RankPos} 名 {ChatColors.White}| 段位：{ChatColors.Gold}{item.RankTitle}{ChatColors.White} | 積分：{ChatColors.Green}{item.Elo}");
        }
    }

    // 【新增】：觸發 !top / !排行 查詢
    private void TriggerTopRankQuery(CCSPlayerController player)
    {
        if (string.IsNullOrWhiteSpace(Config.StatsApiUrl) || player.SteamID == 0) return;

        ulong sId = player.SteamID;
        float now = Server.CurrentTime;

        if (_topQueryCooldown.TryGetValue(sId, out float nextAllowedTime))
        {
            float diff = nextAllowedTime - now;
            if (diff > 0f && diff <= RankQueryCooldownSeconds)
            {
                int remainingSeconds = (int)Math.Ceiling(diff);
                player.PrintToChat($" {_cachedPrefix} {ChatColors.Orange}查詢冷卻中，請於 {ChatColors.Lime}{remainingSeconds} 秒{ChatColors.Orange} 後再試");
                return;
            }
        }

        _topQueryCooldown[sId] = now + RankQueryCooldownSeconds;

        if (_topMemoryCache != null && now < _topCacheExpireTime && (_topCacheExpireTime - now) <= 125.0f)
        {
            PrintTopPlayersToPrivateChat(player, _topMemoryCache);
            return;
        }

        string apiUrl = Config.StatsApiUrl;
        _ = Task.Run(() => QueryTopRankAsync(sId, apiUrl, now));
    }

    private async Task QueryTopRankAsync(ulong steamId, string apiUrl, float queryTime)
    {
        const int maxRetries = 2;
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                string url = $"{apiUrl}?action=top";
                var result = await SharedHttpClient.GetFromJsonAsync<EloTopResponse>(url, SharedJsonOptions);
                if (result is not { Success: true })
                {
                    if (attempt < maxRetries) { await Task.Delay(500); continue; }
                    Server.NextFrame(() => _topQueryCooldown.Remove(steamId));
                    return;
                }

                _topMemoryCache = result.TopPlayers ?? [];
                _topCacheExpireTime = queryTime + 60.0f;

                Server.NextFrame(() =>
                {
                    if (_isServerShuttingDown) return;

                    foreach (var p in _serverPlayersCache)
                    {
                        if (p is { IsValid: true, Handle: not 0, SteamID: var sid } && sid == steamId)
                        {
                            PrintTopPlayersToPrivateChat(p, _topMemoryCache);
                            break;
                        }
                    }
                });
                return;
            }
            catch (Exception ex)
            {
                if (attempt < maxRetries)
                {
                    await Task.Delay(500);
                }
                else
                {
                    Console.WriteLine($"[LiteMatchManager] Top Query Error: {ex.Message}");
                    Server.NextFrame(() => _topQueryCooldown.Remove(steamId));
                }
            }
        }
    }

    // 【核心防護】：完賽積分上傳具備 3 次自動重試機制，徹底杜絕網路瞬斷或舊連線關閉導致玩家白打！
    private async Task SendEloReportAsync(string apiUrl, EloReportPayload payload)
    {
        const int maxRetries = 3;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                using var response = await SharedHttpClient.PostAsJsonAsync(apiUrl, payload, SharedJsonOptions);
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[LiteMatchManager] Elo API HTTP Status Error ({(int)response.StatusCode}), Attempt {attempt}/{maxRetries}");
                    if (attempt < maxRetries)
                    {
                        await Task.Delay(1000);
                        continue;
                    }
                    return;
                }

                var result = await response.Content.ReadFromJsonAsync<EloReportResponse>(SharedJsonOptions);
                if (result is not { Success: true }) return;

                if (result.Winners is { Count: > 0 } && result.Losers is { Count: > 0 })
                {
                    foreach (var w in result.Winners)
                    {
                        if (ulong.TryParse(w.SteamId, out ulong sid))
                            _playerRankClanTag[sid] = GetConfiguredClanTagByElo(w.NewElo);
                    }
                    foreach (var l in result.Losers)
                    {
                        if (ulong.TryParse(l.SteamId, out ulong sid))
                            _playerRankClanTag[sid] = GetConfiguredClanTagByElo(l.NewElo);
                    }

                    Server.NextFrame(() =>
                    {
                        if (_isServerShuttingDown) return;

                        foreach (var w in result.Winners)
                        {
                            Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Gold}👑 勝方：{ChatColors.Lime}{w.Name} {ChatColors.Gold}{w.Rank} {ChatColors.White}積分：{ChatColors.Green}{w.NewElo} + {w.Delta} ▲ {ChatColors.Grey}| 爆頭率 {w.HsRate}%");
                            if (ulong.TryParse(w.SteamId, out ulong sid)) ApplyPlayerRankClanTagBySteamId(sid);
                        }
                        foreach (var l in result.Losers)
                        {
                            Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Grey}💀 敗方：{ChatColors.White}{l.Name} {ChatColors.Gold}{l.Rank} {ChatColors.White}積分：{ChatColors.Red}{l.NewElo} - {l.Delta} ▼ {ChatColors.Grey}| 爆頭率 {l.HsRate}%");
                            if (ulong.TryParse(l.SteamId, out ulong sid)) ApplyPlayerRankClanTagBySteamId(sid);
                        }
                    });
                }
                else
                {
                    if (ulong.TryParse(payload.WinnerSteamId, out ulong wSteamId))
                        _playerRankClanTag[wSteamId] = GetConfiguredClanTagByElo(result.WinnerNewElo);
                    if (ulong.TryParse(payload.LoserSteamId, out ulong lSteamId))
                        _playerRankClanTag[lSteamId] = GetConfiguredClanTagByElo(result.LoserNewElo);

                    Server.NextFrame(() =>
                    {
                        if (_isServerShuttingDown) return;
                        Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Gold}👑 勝方：{ChatColors.Lime}{payload.WinnerName} {ChatColors.Gold}{result.WinnerRank} {ChatColors.White}積分：{ChatColors.Green}{result.WinnerNewElo} + {result.WinnerDelta} ▲ {ChatColors.Grey}| 爆頭率 {result.WinnerHsRate}%");
                        Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Grey}💀 敗方：{ChatColors.White}{payload.LoserName} {ChatColors.Gold}{result.LoserRank} {ChatColors.White}積分：{ChatColors.Red}{result.LoserNewElo} - {result.LoserDelta} ▼ {ChatColors.Grey}| 爆頭率 {result.LoserHsRate}%");

                        if (wSteamId > 0) ApplyPlayerRankClanTagBySteamId(wSteamId);
                        if (lSteamId > 0) ApplyPlayerRankClanTagBySteamId(lSteamId);
                    });
                }

                // 成功結算並印出戰報後，立刻結束迴圈
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LiteMatchManager] Elo API Error (Attempt {attempt}/{maxRetries}): {ex.Message}");
                if (attempt < maxRetries)
                {
                    await Task.Delay(1000); // 等待 1 秒後換一條全新連線自動重發
                }
            }
        }
    }

    private async Task QueryPlayerRankAsync(ulong steamId, string playerName, string apiUrl, float queryTime)
    {
        const int maxRetries = 2;
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                string url = $"{apiUrl}?action=query&steamid={steamId}";
                var result = await SharedHttpClient.GetFromJsonAsync<EloQueryResponse>(url, SharedJsonOptions);
                if (result is not { Success: true })
                {
                    if (attempt < maxRetries) { await Task.Delay(500); continue; }
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
                    
                    // 從伺服器在線玩家快取中找到該玩家的 Controller 物件來發送私密訊息
                    foreach (var p in _serverPlayersCache)
                    {
                        if (p is { IsValid: true, Handle: not 0, SteamID: var sid } && sid == steamId)
                        {
                            PrintPlayerRankToPrivateChat(p, playerName, result);
                            break;
                        }
                    }
                    
                    ApplyPlayerRankClanTagBySteamId(steamId);
                });
                return;
            }
            catch (Exception ex)
            {
                if (attempt < maxRetries)
                {
                    await Task.Delay(500);
                }
                else
                {
                    Console.WriteLine($"[LiteMatchManager] Rank Query Error: {ex.Message}");
                    Server.NextFrame(() => _rankQueryCooldown.Remove(steamId));
                }
            }
        }
    }
}
