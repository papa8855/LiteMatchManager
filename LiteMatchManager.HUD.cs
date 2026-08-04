using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace LiteMatchManager;

public partial class LiteMatchManager
{
    private CCSGameRulesProxy? _hudGameRulesProxy = null;
    private bool _bShowingRoundStartHud = false;

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
        
        // 精準讀取設定秒數 (預設 2 秒)
        AddTimer(Config.RoundStartHudDuration, () =>
        {
            HUD_Clear(); 
        });

        return HookResult.Continue;
    }

    // 時間到，暴力清除畫面 (精準瞬間消失，拒絕淡出)
    private void HUD_Clear()
    {
        _bShowingRoundStartHud = false;

        // 1. 發送空字串洗掉文字內容
        foreach (var p in Utilities.GetPlayers())
        {
            if (IsPlayerValidHUD(p))
            {
                p.PrintToCenterHtml("<font></font>");
            }
        }

        // 2. 黑魔法：瞬間反轉 GameRestart 狀態並同步給客戶端，強行炸掉殘留的黑底框！
        if (_hudGameRulesProxy != null && _hudGameRulesProxy.IsValid)
        {
            var gameRules = _hudGameRulesProxy.GameRules;
            if (gameRules != null)
            {
                gameRules.GameRestart = !gameRules.GameRestart; 
                Utilities.SetStateChanged(_hudGameRulesProxy, "CCSGameRulesProxy", "m_pGameRules");
            }
        }
    }

    // 每一 Tick 渲染計分板
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

        if (!_bShowingRoundStartHud) return;

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

        // 【確保顯示】在凍結時間內同步狀態，防止計分板被系統卡住出不來
        if (_hudGameRulesProxy != null && _hudGameRulesProxy.IsValid)
        {
            var gameRules = _hudGameRulesProxy.GameRules;
            if (gameRules != null && !gameRules.WarmupPeriod)
            {
                float currentTime = Server.CurrentTime;
                float restartTime = gameRules.RestartRoundTime;
                bool expectedState = restartTime < currentTime;

                if (gameRules.GameRestart != expectedState)
                {
                    gameRules.GameRestart = expectedState;
                    Utilities.SetStateChanged(_hudGameRulesProxy, "CCSGameRulesProxy", "m_pGameRules");
                }
            }
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
