using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace LiteMatchManager;

public partial class LiteMatchManager
{
    private bool _bShowingRoundStartHud = false;

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
        
        // 【堅持 2 秒！】
        AddTimer(2.0f, () =>
        {
            HUD_Clear(); 
        });

        return HookResult.Continue;
    }

    // 時間到，暴力清除畫面
    private void HUD_Clear()
    {
        // 1. 停止發送任何新的 HUD (絕對不要發送空字串)
        _bShowingRoundStartHud = false;

        // 2. 因為伺服器只有這一個插件，我們直接發動黑魔法！
        if (_gameRulesInitialized && _gameRules is not null)
        {
            foreach (var proxy in Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules"))
            {
                // 第一步：瞬間反轉狀態，強迫客戶端 Panorama UI 崩解並收起 HTML 框
                _gameRules.GameRestart = !_gameRules.GameRestart;
                Utilities.SetStateChanged(proxy, "CCSGameRulesProxy", "m_pGameRules");

                // 第二步：利用你提到的 ServerGraphic 邏輯進行「狀態強制校正」
                // 為了確保客戶端確實收到了上一步的「反轉」訊號，我們延遲 0.1 秒後再把它校正回來
                AddTimer(0.1f, () =>
                {
                    if (_gameRules == null || !proxy.IsValid) return;

                    float currentTime = Server.CurrentTime; //
                    float restartTime = _gameRules.RestartRoundTime; //[cite: 3]
                    bool expectedState = restartTime < currentTime; //[cite: 3]

                    // 檢查並強制回歸正確狀態，確保遊戲進程不會壞掉[cite: 3]
                    if (_gameRules.GameRestart != expectedState) //[cite: 3]
                    {
                        _gameRules.GameRestart = expectedState; //[cite: 3]
                        Utilities.SetStateChanged(proxy, "CCSGameRulesProxy", "m_pGameRules"); //[cite: 3]
                    }
                });
                
                break; // 處理完第一個代理就退出
            }
        }
    }

    // 每一 Tick 渲染計分板
    private void HUD_OnTick()
    {
        if (!_bShowingRoundStartHud) return;

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
