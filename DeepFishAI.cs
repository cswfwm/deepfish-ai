using Terraria.ModLoader;

namespace DeepFishAI;

public class DeepFishAI : Mod
{
    public override void Load()
    {
        Logger.Info("[大肥鱼] 模组加载");
    }

    public override void PostSetupContent()
    {
        var cfg = AIConfig.Load();
        Logger.Info("[大肥鱼] 配置文件：" + ModPaths.ConfigFile);
        if (!cfg.HasKey) Logger.Warn("[大肥鱼] 还没填 API Key，AI 不会工作");
    }
}
