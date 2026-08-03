using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using System.Collections.Generic;

namespace LiteMatchManager;

public partial class LiteMatchManager
{
    private void InitializeGameRules()
    {
        if (_gameRulesInitialized) return;
        
        foreach (var proxy in Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules"))
        {
            _gameRulesProxy = proxy; // 🔮 存下用來施展黑魔法的 Proxy 實體
            _gameRules = proxy.GameRules;
            break;
        }
        
        _gameRulesInitialized = _gameRules != null && _gameRulesProxy != null;
    }

    private void ShowHud(string html)
    {
        foreach (var p in Utilities.GetPlayers())
        {
            if (p != null && p.IsValid && !p.IsBot) p.PrintToCenterHtml(html);
        }
    }

    private void OnTick()
    {
        // ==========================================
        // 1. 回合開始 HUD 顯示邏輯
        // ==========================================
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
                if (!IsPlayerValid(player))
                    continue;

                player.PrintToCenterHtml(fullHudHtml);
            }
        }

        // ==========================================
        // 2. 處理未準備玩家的踢出倒數提示
        // ==========================================
        if (_pendingInitialReminders.Count > 0)
        {
            float currentTime = Server.CurrentTime;
            List<ulong>? toRemove = null;

            foreach (var kvp in _pendingInitialReminders)
            {
                if (currentTime >= kvp.Value)
                {
                    ulong steamId = kvp.Key;
                    toRemove ??= new List<ulong>();
                    toRemove.Add(steamId);

                    if (!_isMatchLive && !_readyPlayers.Contains(steamId))
                    {
                        foreach (var p in Utilities.GetPlayers())
                        {
                            if (p != null && p.IsValid && p.SteamID == steamId && (p.TeamNum == 2 || p.TeamNum == 3))
                            {
                                int elapsed = 0;
                                if (_playerUnreadyTime.TryGetValue(steamId, out int val)) elapsed = val;
                                int timeLeft = Config.KickUnreadyPlayerTime - elapsed;
                                
                                p.PrintToChat($" {_cachedPrefix} 請輸入 {ChatColors.Lime}!R{ChatColors.White} 準備 ，{ChatColors.Lime}{timeLeft}{ChatColors.White} 秒未準備將被踢出");
                                break;
                            }
                        }
                    }
                }
            }

            if (toRemove != null)
            {
                foreach (var id in toRemove)
                {
                    _pendingInitialReminders.Remove(id);
                }
            }
        }

        // ==========================================
        // 3. 【黑魔法實裝】強制刷新客戶端 UI 狀態
        // ==========================================
        // 降頻處理，避免每 Tick 執行底層判斷造成效能浪費
        _runThisTick = !_runThisTick;
        if (!_runThisTick) return;

        if (!_gameRulesInitialized) InitializeGameRules();

        if (_gameRules != null && _gameRulesProxy != null && _gameRulesProxy.IsValid)
        {
            float currentTime = Server.CurrentTime;
            float restartTime = _gameRules.RestartRoundTime;

            bool expectedState = restartTime < currentTime;

            if (_gameRules.GameRestart != expectedState)
            {
                _gameRules.GameRestart = expectedState;
                // 🔮 關鍵黑魔法：強制伺服器同步 GameRules 狀態給所有客戶端
                // 瞬間清除畫面上因 PrintToCenterHtml 卡住的 HUD 殘影
                Utilities.SetStateChanged(_gameRulesProxy, "CCSGameRulesProxy", "m_pGameRules");
            }
        }
    }
}