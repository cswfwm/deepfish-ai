using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DeepFishAI.Content.Pets;

// 大肥鱼：飘在玩家身边，头顶显示思考气泡。
public class BigFatFishPet : ModProjectile
{
    public override string Texture => "DeepFishAI/Assets/BigFatFish";

    public override void SetStaticDefaults()
    {
        Main.projFrames[Type] = 4;
        Main.projPet[Type] = true;
    }

    public override void SetDefaults()
    {
        Projectile.CloneDefaults(ProjectileID.ZephyrFish);
        AIType = ProjectileID.ZephyrFish;
        Projectile.width = 34;
        Projectile.height = 26;
    }

    public override bool PreAI()
    {
        var player = Main.player[Projectile.owner];
        // 交给原版 ZephyrFish 的 AI 做跟随，只额外推进气泡计时。
        Speech.Tick();
        return true;
    }

    public override void AI()
    {
        var player = Main.player[Projectile.owner];
        if (!player.dead && player.HasBuff(ModContent.BuffType<DeepFishPetBuff>()))
            Projectile.timeLeft = 2;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        // 让气泡跟着宠物画出来。
        Speech.DrawBubble(Main.spriteBatch, Projectile.Center + new Vector2(0, -Projectile.height));
        return true;
    }
}
