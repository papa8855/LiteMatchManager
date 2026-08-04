using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace LiteMatchManager;

public partial class LiteMatchManager
{
    private bool _bShowingRoundStartHud = false;

    private void HUD_OnMapStart()
    {
        _bShowingRoundStartHud = false;
    }

    private HookResult HUD_OnEventRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        if (!_isMatchLive) return HookResult.Continue;
        
        _bShowingRoundStartHud = true;
        
        // 2 秒
        AddTimer(2.0f, () =>
        {
            HUD_Clear(); 
        });

        return HookResult.Continue;
    }

    // 每一 Tick 渲染計分板 (單一字串、排除旁觀版)
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

        // 【高效能】全場只組裝一次這包 HTML 資訊
        string fullHudHtml = string.Format(Config.HudHtml_RoundStart_Title, "對戰進度", modeStr) + 
                             string.Format(Config.HudHtml_RoundStart_TScore, scoreT) + 
                             string.Format(Config.HudHtml_RoundStart_CTScore, scoreCT);

        foreach (var player in Utilities.GetPlayers())
        {
            if (!IsPlayerValidHUD(player)) continue;

            // 【排除旁觀】TeamNum 1 是旁觀，0 是未分配。直接跳過不發送。
            if (player.TeamNum == 1 || player.TeamNum == 0) continue;

            // T (2) 和 CT (3) 都接收同一包排版好的資訊
            player.PrintToCenterHtml(fullHudHtml);
        }
    }

    // 時間到，強制清除畫面
    private void HUD_Clear()
    {
        _bShowingRoundStartHud = false;

        foreach (var player in Utilities.GetPlayers())
        {
            // 一樣只針對場上的 T 和 CT 進行清除
            if (IsPlayerValidHUD(player) && (player.TeamNum == 2 || player.TeamNum == 3))
            {
                // 【頻道覆寫殺招】用原生文字頻道的空白，強行蓋掉 HTML 的 5 秒限制
                player.PrintToCenterHtml("", 0); 
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
