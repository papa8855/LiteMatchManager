using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace LiteMatchManager;

public partial class LiteMatchManager
{
    // ==========================================
    // 這個函數由主程式的 OnTick 每秒呼叫，負責渲染第二個 HUD
    // ==========================================
    private void UpdateRoundStartHud()
    {
        // 只有 _isMatchLive (主程式開賽) 且回合剛開始，才會是 true
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
            if (!IsPlayerValid(player))
                continue;

            player.PrintToCenterHtml(fullHudHtml);
        }
    }

    // ==========================================
    // 這個函數由主程式的計時器 (2秒) 呼叫，負責暴力清除殘影
    // ==========================================
    private void ClearRoundStartHud()
    {
        _bShowingRoundStartHud = false;

        // 1. 發送空白標籤，立刻覆蓋當前畫面的 30 勝計分板
        foreach (var p in Utilities.GetPlayers())
        {
            if (IsPlayerValid(p))
            {
                p.PrintToCenterHtml("<font></font>");
            }
        }

        // 2. 引爆黑魔法：強制客戶端同步 GameRules 狀態，斬斷殘影
        if (_gameRulesProxy != null && _gameRulesProxy.IsValid)
        {
            Utilities.SetStateChanged(_gameRulesProxy, "CCSGameRulesProxy", "m_pGameRules");
        }
    }
}
