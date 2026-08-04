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

        // 【關鍵修復】
        // 1. 絕對不要呼叫 PrintToCenterHtml("")！只要呼叫，Panorama 就會畫出無字底框。
        // 2. 完全依賴您的「黑魔法」來強制關閉客戶端的 HUD 顯示。
        // （因為是 partial class，我們直接使用主程式已經緩存好的 _gameRules）

        if (_gameRulesInitialized && _gameRules is not null)
        {
            foreach (var proxy in Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules"))
            {
                // 瞬間反轉狀態，強制 CS2 客戶端刷新並收起所有 Center 畫面
                _gameRules.GameRestart = !_gameRules.GameRestart; 
                Utilities.SetStateChanged(proxy, "CCSGameRulesProxy", "m_pGameRules");
                break;
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

        // [優化] null 聚合運算子 ?? ，取代三元運算子，速度更快
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
        // [優化] .NET 10 屬性模式匹配，取代落落長的 null 與布林值判斷
        return player is { 
            IsValid: true, 
            IsBot: false, 
            IsHLTV: false, 
            Connected: PlayerConnectedState.Connected, 
            Pawn.IsValid: true 
        };
    }
}
