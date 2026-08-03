using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using System.Text.Json.Serialization;

namespace LiteMatchManager;

// 設定檔類別
public class MatchConfig : BasePluginConfig
{
    [JsonPropertyName("RoundStartHudDuration")]
    public float RoundStartHudDuration { get; set; } = 2.0f; // 您設定的 2 秒
    // 可以在此繼續新增其他設定 (如三十局的配置等)
}

public partial class LiteMatchManager : BasePlugin, IPluginConfig<MatchConfig>
{
    public override string ModuleName => "LiteMatchManager";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "YourName";

    public MatchConfig Config { get; set; } = new();

    // === 共用狀態變數 ===
    private bool _isMatchLive = false; 
    private bool _bShowingRoundStartHud = false; 
    
    // 黑魔法需要的底層變數 (將在 HUD.cs 中使用)
    private CCSGameRules? _gameRules;
    private CCSGameRulesProxy? _gameRulesProxy;
    private bool _gameRulesInitialized = false;
    private bool _runThisTick = false;

    public override void Load(bool hotReload)
    {
        RegisterListener<Listeners.OnMapStart>(OnMapStartHandler);
        RegisterListener<Listeners.OnTick>(OnTickHandler); // OnTickHandler 實作於 HUD.cs
    }

    public void OnConfigParsed(MatchConfig config)
    {
        Config = config;
    }

    private void OnMapStartHandler(string mapName)
    {
        // 換圖時重置所有狀態
        _isMatchLive = false;
        _bShowingRoundStartHud = false;
        _gameRulesInitialized = false;
        _gameRules = null;
        _gameRulesProxy = null;
    }

    // 回合開始事件
    [GameEventHandler]
    public HookResult OnEventRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        // 假設這是在正式比賽中才顯示 30 局 HUD
        // 如果需要，可以把 _isMatchLive 改成 true 測試
        _isMatchLive = true; 

        if (!_isMatchLive) return HookResult.Continue;

        // 呼叫 HUD.cs 中的顯示函數
        ShowRoundStartHud();

        return HookResult.Continue;
    }

    // 準備指令範例 (對應第一種 HUD)
    [ConsoleCommand("css_r", "Ready up")]
    public void OnCommandReady(CCSPlayerController? player, CommandInfo command)
    {
        if (!IsPlayerValid(player)) return;

        // ... 處理玩家準備邏輯 ...

        // 呼叫 HUD.cs 中的顯示函數
        ShowPrepPhaseHud("玩家已準備...");
    }

    // 共用輔助函數：嚴格的實體驗證
    public static bool IsPlayerValid(CCSPlayerController? player)
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
