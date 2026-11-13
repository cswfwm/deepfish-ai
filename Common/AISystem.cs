using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;

namespace DeepFishAI;

// AI 主循环：定时把游戏状态发给 DeepSeek，把回复拆成“思考”和“指令”。
public class AISystem : ModSystem
{
    private static AIConfig _config = new();
    private static readonly AIClient Client = new();
    private static readonly List<(string Role, string Content)> History = new();
    private static CancellationTokenSource? _pending;
    private static int _timer;
    private static bool _busy;

    public static AIConfig Config => _config;
    public static bool AutoEnabled { get; private set; } = true;

    public override void OnWorldLoad()
    {
        _config = AIConfig.Load();
        History.Clear();
        _timer = 0;
        ModLog.Info(_config.HasKey ? "已加载配置，开始工作" : "还没有 API Key，请在 config.json 里填写");
    }

    public override void PostUpdateEverything()
    {
        if (!_config.HasKey) return;
        if (!AutoEnabled) return;

        if (_timer > 0)
        {
            _timer--;
            return;
        }
        _timer = Math.Max(3, _config.IntervalSeconds) * 60;
        if (!_busy) _ = AskAsync(null);
    }

    // 玩家在聊天框输入时调用：立刻问一次。
    public static void AskFromChat(string text) => _ = AskAsync(text);

    private static async Task AskAsync(string? userText)
    {
        if (_busy) return;
        _busy = true;
        try
        {
            var snapshot = GameState.Capture();
            GameState.WriteToDisk(snapshot);

            var prompt = GameState.ToPromptText(snapshot);
            if (!string.IsNullOrWhiteSpace(userText))
                prompt += $"\n玩家对你说：{userText}\n";

            History.Add(("user", prompt));
            while (History.Count > 12) History.RemoveAt(0);

            _pending?.Cancel();
            _pending = new CancellationTokenSource();
            var reply = await Client.AskAsync(_config, History, _pending.Token).ConfigureAwait(false);

            if (!reply.Ok)
            {
                ModLog.Error("AI 请求失败：" + reply.Error);
                Speech.Think("想不出来……", reply.Error);
                return;
            }

            History.Add(("assistant", reply.Text));
            var split = SplitReply(reply.Text);
            Speech.Think(split.Thought, split.Script.Length > 0 ? "执行 " + CountLines(split.Script) + " 条指令" : "");

            if (_config.AutoAct && split.Script.Length > 0)
            {
                var captured = split.Script;
                Main.QueueMainThreadAction(() => ExecuteScript(captured));
            }
        }
        catch (Exception e)
        {
            ModLog.Error("AI 循环异常", e);
        }
        finally
        {
            _busy = false;
        }
    }

    private static void ExecuteScript(string script)
    {
        try
        {
            File.WriteAllText(ModPaths.ScriptFile, script);
            var result = ScriptEngine.Run(script);
            ModLog.Info($"脚本执行：成功 {result.Executed}，失败 {result.Failed}");
            if (result.Failed > 0 && result.Lines.Count > 0)
                Speech.Think(Speech.Current, result.Lines[0]);
        }
        catch (Exception e)
        {
            ModLog.Error("执行脚本异常", e);
        }
    }

    // 回复约定：三个反引号包起来的区块是指令，其余文字是思考。
    public static (string Thought, string Script) SplitReply(string text)
    {
        var start = text.IndexOf("```", StringComparison.Ordinal);
        if (start < 0) return (text.Trim(), "");

        var end = text.IndexOf("```", start + 3, StringComparison.Ordinal);
        if (end < 0) return (text.Substring(0, start).Trim(), text.Substring(start + 3).Trim());

        var thought = (text.Substring(0, start) + " " + text.Substring(end + 3)).Trim();
        return (thought, text.Substring(start + 3, end - start - 3).Trim());
    }

    private static int CountLines(string script)
    {
        var count = 0;
        foreach (var line in script.Split('\n'))
            if (line.Trim().Length > 0) count++;
        return count;
    }
}
