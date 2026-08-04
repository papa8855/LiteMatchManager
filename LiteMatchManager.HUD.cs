using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace LiteMatchManager;

public partial class LiteMatchManager
{
    private bool _bShowingRoundStartHud = false;
    private bool _runThisTick = false; // 新增這行，用來模仿 LOGO 專案的 2 Ticks 節奏

    // 給檔案一換地圖時呼叫的重置
    private void HUD_OnMapStart()
    {
        _bShowingRoundStartHud = false;
    }

    // 給檔案一回合開始時呼叫的計時器
    private HookResult HUD_OnEventRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        if (!_isMatchLive) return HookResult.Continue;
        
        _bShowingRoundStartHud = true;
        
        // 堅持你的 2 秒硬需求！
        AddTimer(2.0f, () =>
        {
            HUD_Clear(); 
        });

        return HookResult.Continue;
    }

    // 時間到，單純停止發送
    private void HUD_Clear()
    {
        // 真正的黑魔法在下面，這裡我們只要切斷發送訊號就好
        _bShowingRoundStartHud = false;
    }

    // 每一 Tick 渲染計分板與狂刷黑魔法
    private void HUD_OnTick()
    {
        // 1. 如果在 2 秒內，發送對戰資訊
        if (_bShowingRoundStartHud)
        {
            if (_cachedTeamT is not { IsValid: true } || _cachedTeamCT is not { IsValid: true })
            {
                foreach (var team in Utilities.FindAllEntitiesByDesignerName<CCSTeam>("cs_team_manager"))
                {
                    if (team.TeamNum == 2) _cachedTeamT = team;
                    else if (team.TeamNum == 3) _cachedTeamCT = team;
                }
            }

            int scoreT = _cachedTeamT?.Score ?? 0;
            int scoreCT = _cachedTeamCT?.Score ?? 0;
            string modeStr = _liveMatchTargetPlayers <= 2 ? "單 挑" : "團 戰";

            string fullHudHtml = string.Format(Config.HudHtml_RoundStart_Title, "對戰進度", modeStr) + 
                                 string.Format(Config.HudHtml_RoundStart_TScore, scoreT) + 
                                 string.Format(Config.HudHtml_RoundStart_CTScore, scoreCT);

            foreach (var player in Utilities.GetPlayers())
            {
                if (IsPlayerValidHUD(player))
                {
                    player.PrintToCenterHtml(fullHudHtml);
                }
            }
        }

        // 2. 【完全移植 LOGO 專案的暴走黑魔法】
        // 根據 ServerGraphic 的寫法，每兩次 Tick 執行一次狀態校正。
        // 這會跟 CS2 引擎瘋狂打架（導致稍微閃爍），但也徹底摧毀了 5 秒不消失的 Bug！
        _runThisTick = !_runThisTick;
        if (!_runThisTick) return;

        if (_gameRulesInitialized && _gameRules is not null)
        {
            foreach (var proxy in Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules"))
            {
                float currentTime = Server.CurrentTime; //[cite: 3]
                float restartTime = _gameRules.RestartRoundTime; //[cite: 3]
                bool expectedState = restartTime < currentTime; //[cite: 3]

                if (_gameRules.GameRestart != expectedState) //[cite: 3]
                {
                    _gameRules.GameRestart = expectedState; //[cite: 3]
                    Utilities.SetStateChanged(proxy, "CCSGameRulesProxy", "m_pGameRules"); //[cite: 3]
                }
                break; // 找到一次 Proxy 就可以跳出了，節省效能
            }
        }
    }

    // 專屬於 HUD 的防呆判斷
    private static bool IsPlayerValidHUD(CCSPlayerController? player)
    {
        return player is { 
            IsValid: true, 
            IsBot: false, 
            IsHLTV: false, 
            Connected: PlayerConnectedState.Connected, 
            Pawn.IsValid: true 
        };
    }
}
