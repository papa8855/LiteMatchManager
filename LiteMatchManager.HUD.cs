using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace LiteMatchManager;

public partial class LiteMatchManager
{
    private CCSGameRulesProxy? _hudGameRulesProxy = null;
    private bool _bShowingRoundStartHud = false;
    private bool _runThisTick = false; // SLAYER 專屬：交替 Tick 節流閥

    // 給檔案一換地圖時呼叫的重置
    private void HUD_OnMapStart()
    {
        _hudGameRulesProxy = null;
        _bShowingRoundStartHud = false;
    }

    // 給檔案一回合開始時呼叫的計時器
    private HookResult HUD_OnEventRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        if (!_isMatchLive) return HookResult.Continue;
        
        _bShowingRoundStartHud = true;
        
        // 100% SLAYER 原汁原味：時間到只切換布林值，不做任何空字串發送
        AddTimer(Config.RoundStartHudDuration, () =>
        {
            _bShowingRoundStartHud = false;
        });

        return HookResult.Continue;
    }

    // 每一 Tick 渲染計分板與執行 SLAYER 黑魔法
    private void HUD_OnTick()
    {
        // 抓取黑魔法需要的 Proxy
        if (_hudGameRulesProxy == null || !_hudGameRulesProxy.IsValid)
        {
            foreach (var proxy in Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules"))
            {
                _hudGameRulesProxy = proxy;
                break;
            }
        }

        // === 1. 顯示邏輯 (對應 SLAYER 的 bShowingServerGraphic 判斷) ===
        if (_bShowingRoundStartHud)
        {
            if (_cachedTeamT == null || !_cachedTeamT.IsValid || _cachedTeamCT == null || !_cachedTeamCT.IsValid)
            {
                foreach (var team in Utilities.FindAllEntitiesByDesignerName<CCSTeam>("cs_team_manager"))
                {
                    if (team.TeamNum == 2) _cachedTeamT = team;
                    if (team.TeamNum == 3) _cachedTeamCT = team;
                }
            }

            int scoreT = _cachedTeamT != null ? _cachedTeamT.Score : 0;
            int scoreCT = _cachedTeamCT != null ? _cachedTeamCT.Score : 0;
            string modeStr = _liveMatchTargetPlayers <= 2 ? "單 挑" : "團 戰";

            string line1 = string.Format(Config.HudHtml_RoundStart_Title, "對戰進度", modeStr);
            string line2 = string.Format(Config.HudHtml_RoundStart_TScore, scoreT);
            string line3 = string.Format(Config.HudHtml_RoundStart_CTScore, scoreCT);
            string fullHudHtml = line1 + line2 + line3;

            foreach (var player in Utilities.GetPlayers())
            {
                if (IsPlayerValidHUD(player))
                {
                    player.PrintToCenterHtml(fullHudHtml);
                }
            }
        }

        // === 2. SLAYER 核心 GameRestart 同步機制 (必須持續運行以維持客戶端不卡圖) ===
        _runThisTick = !_runThisTick;

        if (!_runThisTick) return;

        if (_hudGameRulesProxy == null || !_hudGameRulesProxy.IsValid) return;

        var gameRules = _hudGameRulesProxy.GameRules;
        if (gameRules == null) return;

        if (gameRules.WarmupPeriod) return;

        float currentTime = Server.CurrentTime;
        float restartTime = gameRules.RestartRoundTime;

        bool expectedState = restartTime < currentTime;

        if (gameRules.GameRestart != expectedState)
        {
            gameRules.GameRestart = expectedState;
            Utilities.SetStateChanged(_hudGameRulesProxy, "CCSGameRulesProxy", "m_pGameRules");
        }
    }

    // 專屬於 HUD 的防呆判斷
    private static bool IsPlayerValidHUD(CCSPlayerController? player)
    {
        return player != null
            && player.IsValid
            && !player.IsBot
            && player.Pawn != null
            && player.Pawn.IsValid
            && player.Connected == PlayerConnectedState.Connected
            && !player.IsHLTV;
    }
}
