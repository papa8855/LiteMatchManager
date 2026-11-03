using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LiteMatchManager;

// =========================================
// Elo 設定結構 (對應 LiteMatchManager.json 的 EloSettings)
// =========================================
public sealed class EloSettingsConfig
{
    [JsonPropertyName("EloSystemEnabled")] public bool EloSystemEnabled { get; set; } = true;
    [JsonPropertyName("StatsApiUrl")] public string StatsApiUrl { get; set; } = "https://c2t.clouds.tw/1v1/api_elo.php";
    [JsonPropertyName("StatsApiKey")] public string StatsApiKey { get; set; } = "TW_CS_Secret_2026";
    [JsonPropertyName("ClanTag_TierS")] public string ClanTag_TierS { get; set; } = " 傳 奇";
    [JsonPropertyName("ClanTag_TierA")] public string ClanTag_TierA { get; set; } = " 頂 尖";
    [JsonPropertyName("ClanTag_TierB")] public string ClanTag_TierB { get; set; } = " 菁 英 ";
    [JsonPropertyName("ClanTag_TierC")] public string ClanTag_TierC { get; set; } = " 專 業 ";
    [JsonPropertyName("ClanTag_TierD")] public string ClanTag_TierD { get; set; } = " 菜 鳥 ";
}

// =========================================
// 遠端 PHP Elo API 傳輸與回傳結構
// =========================================
public sealed class EloPlayerMatchStat
{
    [JsonPropertyName("steamid")] public string SteamId { get; set; } = "";
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("kills")] public int Kills { get; set; }
    [JsonPropertyName("deaths")] public int Deaths { get; set; }
    [JsonPropertyName("hs")] public int Hs { get; set; }
    [JsonPropertyName("online")] public bool Online { get; set; } = true;
}

public sealed class EloPlayerResult
{
    [JsonPropertyName("steamid")] public string SteamId { get; set; } = "";
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("old_elo")] public int OldElo { get; set; }
    [JsonPropertyName("new_elo")] public int NewElo { get; set; }
    [JsonPropertyName("delta")] public int Delta { get; set; }
    [JsonPropertyName("rank")] public string Rank { get; set; } = "";
    [JsonPropertyName("hs_rate")] public double HsRate { get; set; }
}

public sealed class EloReportPayload
{
    [JsonPropertyName("api_key")] public string ApiKey { get; set; } = "";
    [JsonPropertyName("map_name")] public string MapName { get; set; } = "";
    [JsonPropertyName("mode")] public string Mode { get; set; } = "1v1";
    [JsonPropertyName("target_score")] public int TargetScore { get; set; } = 30;
    [JsonPropertyName("winner_score")] public int WinnerScore { get; set; }
    [JsonPropertyName("loser_score")] public int LoserScore { get; set; }

    [JsonPropertyName("winner_steamid")] public string WinnerSteamId { get; set; } = "";
    [JsonPropertyName("winner_name")] public string WinnerName { get; set; } = "";
    [JsonPropertyName("winner_kills")] public int WinnerKills { get; set; }
    [JsonPropertyName("winner_hs")] public int WinnerHs { get; set; }
    [JsonPropertyName("loser_steamid")] public string LoserSteamId { get; set; } = "";
    [JsonPropertyName("loser_name")] public string LoserName { get; set; } = "";
    [JsonPropertyName("loser_kills")] public int LoserKills { get; set; }
    [JsonPropertyName("loser_hs")] public int LoserHs { get; set; }

    [JsonPropertyName("winners")] public List<EloPlayerMatchStat> Winners { get; set; } = [];
    [JsonPropertyName("losers")] public List<EloPlayerMatchStat> Losers { get; set; } = [];
}

public sealed class EloReportResponse
{
    [JsonPropertyName("success")] public bool Success { get; set; }
    [JsonPropertyName("mode")] public string Mode { get; set; } = "1v1";

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

    [JsonPropertyName("winners")] public List<EloPlayerResult>? Winners { get; set; }
    [JsonPropertyName("losers")] public List<EloPlayerResult>? Losers { get; set; }
}

public sealed class EloQueryResponse
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

public sealed class EloTopPlayerItem
{
    [JsonPropertyName("rank_pos")] public int RankPos { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("elo")] public int Elo { get; set; }
    [JsonPropertyName("rank_title")] public string RankTitle { get; set; } = "";
}

public sealed class EloTopResponse
{
    [JsonPropertyName("success")] public bool Success { get; set; }
    [JsonPropertyName("top_players")] public List<EloTopPlayerItem>? TopPlayers { get; set; }
}

// 【.NET 10 0-GC JSON Source Generator】
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(EloReportPayload))]
[JsonSerializable(typeof(EloReportResponse))]
[JsonSerializable(typeof(EloQueryResponse))]
[JsonSerializable(typeof(EloTopResponse))]
internal partial class EloJsonContext : JsonSerializerContext
{
}

public partial class LiteMatchManager
{
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

    private static async Task<T?> FetchJsonZeroGcAsync<T>(string url, System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> typeInfo)
    {
        using var response = await SharedHttpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) return default;

        await using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        byte[] buffer = ArrayPool<byte>.Shared.Rent(4096);
        try
        {
            int totalRead = 0;
            int bytesRead;
            while ((bytesRead = await stream.ReadAsync(buffer.AsMemory(totalRead, buffer.Length - totalRead)).ConfigureAwait(false)) > 0)
            {
                totalRead += bytesRead;
                if (totalRead == buffer.Length)
                {
                    byte[] larger = ArrayPool<byte>.Shared.Rent(buffer.Length * 2);
                    Buffer.BlockCopy(buffer, 0, larger, 0, totalRead);
                    ArrayPool<byte>.Shared.Return(buffer);
                    buffer = larger;
                }
            }
            return totalRead > 0 ? JsonSerializer.Deserialize(buffer.AsSpan(0, totalRead), typeInfo) : default;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public MemoryFunctionVoid<CCSPlayerController, uint, string>? SetClanFunc;
    private bool _isSetClanHooked = false;

    private readonly ConcurrentDictionary<ulong, string> _playerRankClanTag = new();
    private readonly ConcurrentDictionary<ulong, byte> _fetchingClanTags = new();

    private readonly Dictionary<ulong, int> _matchKills = new(8);
    private readonly Dictionary<ulong, int> _matchDeaths = new(8);
    private readonly Dictionary<ulong, int> _matchHeadshots = new(8);

    private readonly Dictionary<ulong, (int Team, string Name)> _matchParticipants = new(8);
    private readonly Dictionary<ulong, (int Team, string Name)> _onlineTeamMapCache = new(8);
    private readonly HashSet<ulong> _initialStarters = new(8);

    private bool _hasSubmittedElo = false;

    private const float RankQueryCooldownSeconds = 60.0f;
    private readonly Dictionary<ulong, float> _rankQueryCooldown = new(64);
    private readonly ConcurrentDictionary<ulong, (EloQueryResponse Data, float ExpireTime)> _rankMemoryCache = new();

    private readonly Dictionary<ulong, float> _topQueryCooldown = new(64);
    private List<EloTopPlayerItem>? _topMemoryCache = null;
    private float _topCacheExpireTime = 0f;

    // 由主程式 OnConfigParsed 呼叫：根據 EloSystemEnabled 開關決定啟用或徹底關閉所有積分運作
    private void OnEloConfigParsed(LiteMatchConfig config)
    {
        if (!config.EloSettings.EloSystemEnabled)
        {
            DisableEloSystemCompletely();
        }
    }

    private void DisableEloSystemCompletely()
    {
        UnloadEloClanTagSystem();
        _playerRankClanTag.Clear();
        _fetchingClanTags.Clear();
        _matchKills.Clear();
        _matchDeaths.Clear();
        _matchHeadshots.Clear();
        _matchParticipants.Clear();
        _onlineTeamMapCache.Clear();
        _initialStarters.Clear();
        _rankQueryCooldown.Clear();
        _rankMemoryCache.Clear();
        _topQueryCooldown.Clear();
        _topMemoryCache = null;
        _topCacheExpireTime = 0f;
        _hasSubmittedElo = false;
    }

    private void InitEloClanTagSystem()
    {
        if (!Config.EloSettings.EloSystemEnabled || _isSetClanHooked) return;

        try
        {
            SetClanFunc ??= new MemoryFunctionVoid<CCSPlayerController, uint, string>("CCSPlayerController::SetClan");
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

    private HookResult OnSetClanPre(DynamicHook hook)
    {
        return Config.EloSettings.EloSystemEnabled ? HookResult.Handled : HookResult.Continue;
    }

    private string GetConfiguredClanTagByElo(int elo)
    {
        var eloCfg = Config.EloSettings;
        if (elo >= 1800) return eloCfg.ClanTag_TierS ?? "";
        if (elo >= 1500) return eloCfg.ClanTag_TierA ?? "";
        if (elo >= 1250) return eloCfg.ClanTag_TierB ?? "";
        if (elo >= 1000) return eloCfg.ClanTag_TierC ?? "";
        return eloCfg.ClanTag_TierD ?? "";
    }

    private void ApplyPlayerRankClanTag(CCSPlayerController? player)
    {
        if (!Config.EloSettings.EloSystemEnabled) return;
        if (player is not { IsValid: true, Handle: not 0, IsBot: false, IsHLTV: false, Connected: PlayerConnectedState.Connected, SteamID: > 0 })
            return;

        string targetTag = _playerRankClanTag.TryGetValue(player.SteamID, out string? savedTag) && !string.IsNullOrEmpty(savedTag)
            ? savedTag
            : "";

        if (player.Clan != targetTag)
        {
            player.Clan = targetTag;
            Utilities.SetStateChanged(player, "CCSPlayerController", "m_szClan");
        }
    }

    private void ApplyPlayerRankClanTagBySteamId(ulong steamId)
    {
        if (!Config.EloSettings.EloSystemEnabled || _isServerShuttingDown || steamId == 0) return;

        ReadOnlySpan<CCSPlayerController> players = CollectionsMarshal.AsSpan(_serverPlayersCache);
        for (int i = 0; i < players.Length; i++)
        {
            var p = players[i];
            if (p is { IsValid: true, Handle: not 0, IsBot: false, SteamID: var sid } && sid == steamId)
            {
                ApplyPlayerRankClanTag(p);
                break;
            }
        }
    }

    private void FetchPlayerClanTagOnConnect(CCSPlayerController? player)
    {
        if (!Config.EloSettings.EloSystemEnabled || _isServerShuttingDown) return;
        if (player is not { IsValid: true, Handle: not 0, IsBot: false, IsHLTV: false, SteamID: > 0 }) return;
        if (string.IsNullOrWhiteSpace(Config.EloSettings.StatsApiUrl)) return;

        ulong steamId = player.SteamID;
        float now = Server.CurrentTime;

        if (_playerRankClanTag.ContainsKey(steamId))
        {
            ApplyPlayerRankClanTag(player);
            return;
        }

        if (!_fetchingClanTags.TryAdd(steamId, 1)) return;

        string apiUrl = Config.EloSettings.StatsApiUrl;
        _ = Task.Run(async () =>
        {
            try
            {
                if (!Config.EloSettings.EloSystemEnabled) return;

                string url = $"{apiUrl}?action=query&steamid={steamId}";
                var result = await FetchJsonZeroGcAsync(url, EloJsonContext.Default.EloQueryResponse).ConfigureAwait(false);
                if (result is not { Success: true } || !Config.EloSettings.EloSystemEnabled) return;

                _rankMemoryCache[steamId] = (result, now + 120.0f);
                _playerRankClanTag[steamId] = result.Found ? GetConfiguredClanTagByElo(result.Elo) : "";

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
        if (!Config.EloSettings.EloSystemEnabled) return;
        _playerRankClanTag.TryRemove(steamId, out _);
        _fetchingClanTags.TryRemove(steamId, out _);
    }

    private void ResetEloMatchStats()
    {
        if (!Config.EloSettings.EloSystemEnabled) return;

        _matchKills.Clear();
        _matchDeaths.Clear();
        _matchHeadshots.Clear();
        _matchParticipants.Clear();
        _onlineTeamMapCache.Clear();
        _initialStarters.Clear();
        _hasSubmittedElo = false;

        if (_isMatchLive)
        {
            ReadOnlySpan<CCSPlayerController> players = CollectionsMarshal.AsSpan(_serverPlayersCache);
            for (int i = 0; i < players.Length; i++)
            {
                var p = players[i];
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
        if (!Config.EloSettings.EloSystemEnabled || !_isMatchLive || _isCountdownActive) return;

        if (attacker is { IsValid: true, IsBot: false, SteamID: > 0 } &&
            victim is { IsValid: true } &&
            attacker.SteamID != victim.SteamID)
        {
            ulong aId = attacker.SteamID;
            CollectionsMarshal.GetValueRefOrAddDefault(_matchKills, aId, out _)++;

            if (isHeadshot)
            {
                CollectionsMarshal.GetValueRefOrAddDefault(_matchHeadshots, aId, out _)++;
            }

            if (attacker.TeamNum is 2 or 3)
            {
                ref var aEntry = ref CollectionsMarshal.GetValueRefOrAddDefault(_matchParticipants, aId, out bool aExists);
                if (!aExists)
                {
                    aEntry = (attacker.TeamNum, attacker.PlayerName ?? "Player");
                }
                else if (aEntry.Team != attacker.TeamNum)
                {
                    aEntry.Team = attacker.TeamNum;
                }
            }

            if (!victim.IsBot && victim.SteamID > 0)
            {
                ulong vId = victim.SteamID;
                CollectionsMarshal.GetValueRefOrAddDefault(_matchDeaths, vId, out _)++;

                if (victim.TeamNum is 2 or 3)
                {
                    ref var vEntry = ref CollectionsMarshal.GetValueRefOrAddDefault(_matchParticipants, vId, out bool vExists);
                    if (!vExists)
                    {
                        vEntry = (victim.TeamNum, victim.PlayerName ?? "Player");
                    }
                    else if (vEntry.Team != victim.TeamNum)
                    {
                        vEntry.Team = victim.TeamNum;
                    }
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
        if (!Config.EloSettings.EloSystemEnabled ||
            _hasSubmittedElo ||
            (_liveMatchTargetPlayers != 2 && _liveMatchTargetPlayers != 4) ||
            winnerScore < Config.MatchSettings.FinalMatchWinScore ||
            scoreT == scoreCT ||
            string.IsNullOrWhiteSpace(Config.EloSettings.StatsApiUrl))
        {
            return;
        }

        _onlineTeamMapCache.Clear();
        ReadOnlySpan<CCSPlayerController> players = CollectionsMarshal.AsSpan(_serverPlayersCache);
        for (int i = 0; i < players.Length; i++)
        {
            var p = players[i];
            if (p is { IsValid: true, Handle: not 0, IsBot: false, IsHLTV: false, Connected: PlayerConnectedState.Connected, SteamID: > 0, TeamNum: 2 or 3 })
            {
                string pName = _matchParticipants.TryGetValue(p.SteamID, out var cached) && !string.IsNullOrEmpty(cached.Name)
                    ? cached.Name
                    : (p.PlayerName ?? "Player");

                _onlineTeamMapCache[p.SteamID] = (p.TeamNum, pName);

                int engagements = _matchKills.GetValueOrDefault(p.SteamID) + _matchDeaths.GetValueOrDefault(p.SteamID);
                if (_initialStarters.Contains(p.SteamID) || engagements >= 3)
                {
                    _matchParticipants[p.SteamID] = (p.TeamNum, pName);
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

            bool isCurrentlyOnline = _onlineTeamMapCache.TryGetValue(sid, out var curInfo) && curInfo.Team == info.Team;
            bool isStarter = _initialStarters.Contains(sid);

            if (!isStarter && engagements < 10)
            {
                continue;
            }

            if (info.Team == winTeam)
            {
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

        if (_liveMatchTargetPlayers == 4 && (winnersList.Count < 1 || losersList.Count < 1))
        {
            return;
        }

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

        for (int i = 0; i < winnersList.Count; i++)
        {
            if (ulong.TryParse(winnersList[i].SteamId.AsSpan(), out ulong id)) _rankMemoryCache.TryRemove(id, out _);
        }
        for (int i = 0; i < losersList.Count; i++)
        {
            if (ulong.TryParse(losersList[i].SteamId.AsSpan(), out ulong id)) _rankMemoryCache.TryRemove(id, out _);
        }
        _topMemoryCache = null;
        _topCacheExpireTime = 0f;

        var firstWinner = winnersList[0];
        var firstLoser = losersList[0];

        var payload = new EloReportPayload
        {
            ApiKey        = Config.EloSettings.StatsApiKey,
            MapName       = Server.MapName ?? "Unknown",
            Mode          = _liveMatchTargetPlayers == 4 ? "2v2" : "1v1",
            TargetScore   = Config.MatchSettings.FinalMatchWinScore,
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

        string apiUrl = Config.EloSettings.StatsApiUrl;
        _ = Task.Run(() => SendEloReportAsync(apiUrl, payload));
    }

    private void PrintPlayerRankToPrivateChat(CCSPlayerController player, string playerName, EloQueryResponse result)
    {
        if (!result.Found)
        {
            player.PrintToChat($" {_cachedPrefix} {ChatColors.Gold}{playerName} {ChatColors.White} | 目前段位：{ChatColors.LightBlue}{result.RankTitle} {ChatColors.White} | 初始積分：{ChatColors.Lime}900");
            player.PrintToChat($" {_cachedPrefix} {ChatColors.White}戰績狀態：{ChatColors.Grey}尚未完成 30 回合排位定級賽");
            player.PrintToCenter($"段位：尚未定級 (30回合定級賽中)");
        }
        else
        {
            player.PrintToChat($" {_cachedPrefix} {ChatColors.Gold}{playerName} {ChatColors.White} | 排名：{ChatColors.Lime}第 {result.RankPos} 名 {ChatColors.White} | 段位：{ChatColors.Gold}{result.RankTitle}");
            player.PrintToChat($" {_cachedPrefix} {ChatColors.White}積分：{ChatColors.Green}{result.Elo} {ChatColors.White} | 戰績：{ChatColors.Lime}{result.Wins} 勝 {ChatColors.DarkRed} {result.Losses} 敗 {ChatColors.Orange}{result.WinRate} % {ChatColors.White} | 爆頭：{ChatColors.Orange}{result.HsRate} %");
            player.PrintToCenter($"你 的 段 位：{result.RankTitle} | 積 分：{result.Elo} 分");
        }
    }

    private void TriggerPlayerRankQuery(CCSPlayerController player)
    {
        if (!Config.EloSettings.EloSystemEnabled || string.IsNullOrWhiteSpace(Config.EloSettings.StatsApiUrl) || player.SteamID == 0)
            return;

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

        string apiUrl = Config.EloSettings.StatsApiUrl;
        _ = Task.Run(() => QueryPlayerRankAsync(sId, pName, apiUrl, now));
    }

    private void PrintTopPlayersToPrivateChat(CCSPlayerController player, List<EloTopPlayerItem>? topPlayers)
    {
        if (topPlayers == null || topPlayers.Count == 0)
        {
            player.PrintToChat($" {_cachedPrefix} {ChatColors.Grey}目前尚無排位完賽紀錄");
            return;
        }

        for (int i = 0; i < topPlayers.Count; i++)
        {
            var item = topPlayers[i];
            player.PrintToChat($" {_cachedPrefix} {ChatColors.Gold}{item.Name} {ChatColors.White}| 排名：{ChatColors.Lime}第 {item.RankPos} 名 {ChatColors.White}| 段位：{ChatColors.Gold}{item.RankTitle}{ChatColors.White} | 積分：{ChatColors.Green}{item.Elo}");
        }
    }

    private void TriggerTopRankQuery(CCSPlayerController player)
    {
        if (!Config.EloSettings.EloSystemEnabled || string.IsNullOrWhiteSpace(Config.EloSettings.StatsApiUrl) || player.SteamID == 0)
            return;

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

        string apiUrl = Config.EloSettings.StatsApiUrl;
        _ = Task.Run(() => QueryTopRankAsync(sId, apiUrl, now));
    }

    private async Task QueryTopRankAsync(ulong steamId, string apiUrl, float queryTime)
    {
        if (!Config.EloSettings.EloSystemEnabled) return;

        const int maxRetries = 2;
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                string url = $"{apiUrl}?action=top";
                var result = await FetchJsonZeroGcAsync(url, EloJsonContext.Default.EloTopResponse).ConfigureAwait(false);
                if (result is not { Success: true } || !Config.EloSettings.EloSystemEnabled)
                {
                    if (attempt < maxRetries) { await Task.Delay(500).ConfigureAwait(false); continue; }
                    Server.NextFrame(() => _topQueryCooldown.Remove(steamId));
                    return;
                }

                _topMemoryCache = result.TopPlayers ?? [];
                _topCacheExpireTime = queryTime + 60.0f;

                Server.NextFrame(() =>
                {
                    if (_isServerShuttingDown || !Config.EloSettings.EloSystemEnabled) return;

                    ReadOnlySpan<CCSPlayerController> players = CollectionsMarshal.AsSpan(_serverPlayersCache);
                    for (int i = 0; i < players.Length; i++)
                    {
                        var p = players[i];
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
                    await Task.Delay(500).ConfigureAwait(false);
                }
                else
                {
                    Console.WriteLine($"[LiteMatchManager] Top Query Error: {ex.Message}");
                    Server.NextFrame(() => _topQueryCooldown.Remove(steamId));
                }
            }
        }
    }

    private async Task SendEloReportAsync(string apiUrl, EloReportPayload payload)
    {
        if (!Config.EloSettings.EloSystemEnabled) return;

        const int maxRetries = 3;
        byte[] jsonBytes = JsonSerializer.SerializeToUtf8Bytes(payload, EloJsonContext.Default.EloReportPayload);

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                if (!Config.EloSettings.EloSystemEnabled) return;

                using var content = new ByteArrayContent(jsonBytes);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

                using var response = await SharedHttpClient.PostAsync(apiUrl, content).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[LiteMatchManager] Elo API HTTP Status Error ({(int)response.StatusCode}), Attempt {attempt}/{maxRetries}");
                    if (attempt < maxRetries)
                    {
                        await Task.Delay(1000).ConfigureAwait(false);
                        continue;
                    }
                    return;
                }

                byte[] resBytes = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                var result = JsonSerializer.Deserialize(resBytes.AsSpan(), EloJsonContext.Default.EloReportResponse);
                if (result is not { Success: true } || !Config.EloSettings.EloSystemEnabled) return;

                if (result.Winners is { Count: > 0 } && result.Losers is { Count: > 0 })
                {
                    for (int i = 0; i < result.Winners.Count; i++)
                    {
                        var w = result.Winners[i];
                        if (ulong.TryParse(w.SteamId.AsSpan(), out ulong sid))
                            _playerRankClanTag[sid] = GetConfiguredClanTagByElo(w.NewElo);
                    }
                    for (int i = 0; i < result.Losers.Count; i++)
                    {
                        var l = result.Losers[i];
                        if (ulong.TryParse(l.SteamId.AsSpan(), out ulong sid))
                            _playerRankClanTag[sid] = GetConfiguredClanTagByElo(l.NewElo);
                    }

                    Server.NextFrame(() =>
                    {
                        if (_isServerShuttingDown || !Config.EloSettings.EloSystemEnabled) return;

                        for (int i = 0; i < result.Winners.Count; i++)
                        {
                            var w = result.Winners[i];
                            Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Gold}👑 勝方：{ChatColors.Lime}{w.Name} {ChatColors.Gold}{w.Rank} {ChatColors.White}積分：{ChatColors.Green}{w.NewElo} + {w.Delta} ▲ {ChatColors.Grey}| 爆頭率 {w.HsRate}%");
                            if (ulong.TryParse(w.SteamId.AsSpan(), out ulong sid)) ApplyPlayerRankClanTagBySteamId(sid);
                        }
                        for (int i = 0; i < result.Losers.Count; i++)
                        {
                            var l = result.Losers[i];
                            Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Grey}💀 敗方：{ChatColors.White}{l.Name} {ChatColors.LightBlue}{l.Rank} {ChatColors.White}積分：{ChatColors.Red}{l.NewElo} - {l.Delta} ▼ {ChatColors.Grey}| 爆頭率 {l.HsRate}%");
                            if (ulong.TryParse(l.SteamId.AsSpan(), out ulong sid)) ApplyPlayerRankClanTagBySteamId(sid);
                        }
                    });
                }
                else
                {
                    if (ulong.TryParse(payload.WinnerSteamId.AsSpan(), out ulong wSteamId))
                        _playerRankClanTag[wSteamId] = GetConfiguredClanTagByElo(result.WinnerNewElo);
                    if (ulong.TryParse(payload.LoserSteamId.AsSpan(), out ulong lSteamId))
                        _playerRankClanTag[lSteamId] = GetConfiguredClanTagByElo(result.LoserNewElo);

                    Server.NextFrame(() =>
                    {
                        if (_isServerShuttingDown || !Config.EloSettings.EloSystemEnabled) return;
                        Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Gold}👑 勝方：{ChatColors.Lime}{payload.WinnerName} {ChatColors.Gold}{result.WinnerRank} {ChatColors.White}積分：{ChatColors.Green}{result.WinnerNewElo} + {result.WinnerDelta} ▲ {ChatColors.Grey}| 爆頭率 {result.WinnerHsRate}%");
                        Server.PrintToChatAll($" {_cachedPrefix} {ChatColors.Grey}💀 敗方：{ChatColors.White}{payload.LoserName} {ChatColors.Gold}{result.LoserRank} {ChatColors.White}積分：{ChatColors.Red}{result.LoserNewElo} - {result.LoserDelta} ▼ {ChatColors.Grey}| 爆頭率 {result.LoserHsRate}%");

                        if (wSteamId > 0) ApplyPlayerRankClanTagBySteamId(wSteamId);
                        if (lSteamId > 0) ApplyPlayerRankClanTagBySteamId(lSteamId);
                    });
                }
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LiteMatchManager] Elo API Error (Attempt {attempt}/{maxRetries}): {ex.Message}");
                if (attempt < maxRetries)
                {
                    await Task.Delay(1000).ConfigureAwait(false);
                }
            }
        }
    }

    private async Task QueryPlayerRankAsync(ulong steamId, string playerName, string apiUrl, float queryTime)
    {
        if (!Config.EloSettings.EloSystemEnabled) return;

        const int maxRetries = 2;
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                string url = $"{apiUrl}?action=query&steamid={steamId}";
                var result = await FetchJsonZeroGcAsync(url, EloJsonContext.Default.EloQueryResponse).ConfigureAwait(false);
                if (result is not { Success: true } || !Config.EloSettings.EloSystemEnabled)
                {
                    if (attempt < maxRetries) { await Task.Delay(500).ConfigureAwait(false); continue; }
                    Server.NextFrame(() => _rankQueryCooldown.Remove(steamId));
                    return;
                }

                _rankMemoryCache[steamId] = (result, queryTime + 120.0f);
                _playerRankClanTag[steamId] = result.Found
                    ? GetConfiguredClanTagByElo(result.Elo)
                    : "";

                Server.NextFrame(() =>
                {
                    if (_isServerShuttingDown || !Config.EloSettings.EloSystemEnabled) return;

                    ReadOnlySpan<CCSPlayerController> players = CollectionsMarshal.AsSpan(_serverPlayersCache);
                    for (int i = 0; i < players.Length; i++)
                    {
                        var p = players[i];
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
                    await Task.Delay(500).ConfigureAwait(false);
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