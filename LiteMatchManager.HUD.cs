using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace LiteMatchManager;

public partial class LiteMatchManager
{
    // ==========================================
    // 渲染第二種 HUD (30勝計分板)
    // ==========================================
    private void UpdateRoundStartHud()
    {
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

    // ==========================================
    // 精準清除 30 勝 HUD 殘影
    // ==========================================
    private void ClearRoundStartHud()
    {
        _bShowingRoundStartHud = false;

        foreach (var p in Utilities.GetPlayers())
        {
            if (IsPlayerValidHUD(p))
            {
                p.PrintToCenterHtml("<font></font>");
            }
        }

        if (_gameRulesProxy != null && _gameRulesProxy.IsValid)
        {
            Utilities.SetStateChanged(_gameRulesProxy, "CCSGameRulesProxy", "m_pGameRules");
        }
    }

    // ==========================================
    // 黑魔法狀態刷新機制
    // ==========================================
    private void ProcessBlackMagic()
    {
        _runThisTick = !_runThisTick;
        if (!_runThisTick) return;

        if (_gameRules != null && _gameRulesProxy != null && _gameRulesProxy.IsValid)
        {
            float currentTime = Server.CurrentTime;
            float restartTime = _gameRules.RestartRoundTime;

            bool expectedState = restartTime < currentTime;

            if (_gameRules.GameRestart != expectedState)
            {
                _gameRules.GameRestart = expectedState;
                Utilities.SetStateChanged(_gameRulesProxy, "CCSGameRulesProxy", "m_pGameRules");
            }
        }
    }

    // 專屬於 HUD 的防呆判斷 (絕不影響主程式)
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
