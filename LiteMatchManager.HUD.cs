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
        
        AddTimer(Config.RoundStartHudDuration, () =>
        {
            HUD_Clear(); 
        });

        return HookResult.Continue;
    }

    // 時間到，暴力清除畫面
    private void HUD_Clear()
    {
        _bShowingRoundStartHud = false;

        foreach (var p in Utilities.GetPlayers())
        {
            if (IsPlayerValidHUD(p))
            {
                p.PrintToCenterHtml("<font></font>");
            }
        }

        if (_hudGameRulesProxy != null && _hudGameRulesProxy.IsValid && _gameRules != null)
        {
            // 黑魔法：瞬間反轉，下一微秒您的主程式 OnTick 就會把它校正回來，藉此製造一次畫面刷新！
            _gameRules.GameRestart = !_gameRules.GameRestart; 
            Utilities.SetStateChanged(_hudGameRulesProxy, "CCSGameRulesProxy", "m_pGameRules");
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