using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace LiteMatchManager;

public partial class LiteMatchManager
{
    private bool _bShowingRoundStartHud = false;
    private bool _runThisTick = false;

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

    // 時間到，關閉總開關
    private void HUD_Clear()
    {
        _bShowingRoundStartHud = false;
    }

    // 每一 Tick 渲染計分板與狂刷黑魔法
    private void HUD_OnTick()
    {
        // 【最關鍵的防護罩】
        // 只要 2 秒時間一到，_bShowingRoundStartHud 變成 false，這裡就會直接 return！
        // 保證「黑魔法只在顯示對戰資訊時發動」，平時伺服器零負擔。
        if (!_bShowingRoundStartHud) return;

        // --- 1. 正常發送對戰資訊 ---
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

        string fullHudHtml = string.Format(Config.HudHtml_RoundStart_Title, "對戰", modeStr) + 
                             string.Format(Config.HudHtml_RoundStart_TScore, scoreT) + 
                             string.Format(Config.HudHtml_RoundStart_CTScore, scoreCT);

        foreach (var player in Utilities.GetPlayers())
        {
            if (IsPlayerValidHUD(player))
            {
                player.PrintToCenterHtml(fullHudHtml);
            }
        }

        // --- 2. LOGO 專案的暴走黑魔法 (被封印在這 2 秒內) ---
        _runThisTick = !_runThisTick; //[cite: 3]
        if (!_runThisTick) return; //[cite: 3]

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
                break; 
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
