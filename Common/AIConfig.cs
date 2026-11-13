using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace DeepFishAI;

// 配置存在 mod 的 dataDirectory 里，玩家改文件即可，不需要 UI。
public sealed class AIConfig
{
    public string BaseUrl { get; set; } = "https://api.deepseek.com";
    public string Model { get; set; } = "deepseek-chat";
    public string ApiKey { get; set; } = "";
    public string SystemPrompt { get; set; } =
        "你是泰拉瑞亚手机版里的一只会说话的大肥鱼宠物。你只能通过给出的指令影响游戏。"
        + "每次回答先用一句话说出你的想法，然后给出 0~3 条指令，每条一行。"
        + "指令格式：place x y tileId / dig x y / fill x1 y1 x2 y2 tileId / goto x y / wait / say 文本。"
        + "不要编造指令之外的东西。";
    public int IntervalSeconds { get; set; } = 20;
    public float Temperature { get; set; } = 0.4f;
    public int MaxTokens { get; set; } = 300;
    public bool AutoAct { get; set; } = true;
    public bool Speak { get; set; } = true;

    public static string FilePath => ModPaths.ConfigFile;

    public static AIConfig Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var text = File.ReadAllText(FilePath);
                var cfg = JsonSerializer.Deserialize<AIConfig>(text, JsonOptions);
                if (cfg != null) return cfg;
            }
        }
        catch (Exception e)
        {
            ModLog.Error("读取配置失败", e);
        }

        var fresh = new AIConfig();
        fresh.Save();
        return fresh;
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(ModPaths.DataDir);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, JsonOptions));
        }
        catch (Exception e)
        {
            ModLog.Error("写入配置失败", e);
        }
    }

    public bool HasKey => !string.IsNullOrWhiteSpace(ApiKey);

    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };
}
