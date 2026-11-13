using System;
using System.IO;
using Terraria;
using Terraria.ModLoader;

namespace DeepFishAI;

// 运行时探测可写目录，避免依赖某个可能不存在的属性。
public static class ModPaths
{
    private static string? _dataDir;

    public static string DataDir
    {
        get
        {
            _dataDir ??= Resolve();
            return _dataDir;
        }
    }

    public static string StateFile => Path.Combine(DataDir, "state", "world.json");
    public static string ScriptFile => Path.Combine(DataDir, "state", "script.txt");
    public static string ConfigFile => Path.Combine(DataDir, "config.json");

    private static string Resolve()
    {
        var candidates = new[]
        {
            Safe(() => Path.Combine(Main.SavePath, "ModData", "DeepFishAI")),
            Safe(() => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "DeepFishAI")),
            Safe(() => Path.Combine(Path.GetTempPath(), "DeepFishAI")),
        };

        foreach (var candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate)) continue;
            try
            {
                Directory.CreateDirectory(candidate);
                var probe = Path.Combine(candidate, ".probe");
                File.WriteAllText(probe, "ok");
                File.Delete(probe);
                ModLog.Info("数据目录：" + candidate);
                return candidate;
            }
            catch
            {
                // 换下一个候选
            }
        }

        var fallback = Path.Combine(Path.GetTempPath(), "DeepFishAI");
        try { Directory.CreateDirectory(fallback); } catch { }
        return fallback;
    }

    private static string? Safe(Func<string?> getter)
    {
        try { return getter(); } catch { return null; }
    }
}

public static class ModLog
{
    public static void Info(string message)
    {
        try { ModContent.GetInstance<DeepFishAI>().Logger.Info("[大肥鱼] " + message); } catch { }
    }

    public static void Error(string message, Exception? e = null)
    {
        try
        {
            var logger = ModContent.GetInstance<DeepFishAI>().Logger;
            if (e == null) logger.Error("[大肥鱼] " + message);
            else logger.Error("[大肥鱼] " + message, e);
        }
        catch { }
    }
}
