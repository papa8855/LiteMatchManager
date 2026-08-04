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
        // 【優化】因為移除了黑魔法，這裡不再需要重置 _hudGameRulesProxy
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
                // 【關鍵修復】直接傳送絕對空字串 ""，不要放 <font></font> 或空白鍵
                p.PrintToCenterHtml("");
            }
        }

        // 【關鍵修復】徹底刪除原本的 GameRestart 與 SetStateChanged 黑魔法！
        // CS2 原生收到 "" 就會收起 UI，強制刷新反而會觸發 Panorama UI 的空黑框 Bug。
    }

    // 每一 Tick 渲染計分板
    private void HUD_OnTick()
    {
        // 【優化】原本這裡有尋找 _hudGameRulesProxy 的迴圈，現在直接拔除，實現 0 負擔 Tick！

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

        // 【優化】使用 .NET 的字串串接，減少多次 string.Format 產生的變數
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
        // 【優化】.NET 10 屬性模式匹配 (Pattern Matching)，取代落落長的 && 判斷，底層跳轉效能更佳
        return player is { 
            IsValid: true, 
            IsBot: false, 
            IsHLTV: false, 
            Connected: PlayerConnectedState.Connected, 
            Pawn.IsValid: true 
        };
    }
}
