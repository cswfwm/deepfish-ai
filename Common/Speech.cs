using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace DeepFishAI;

// 大肥鱼头顶的思考气泡：分两行显示，过一会儿自动消失。
public static class Speech
{
    public static string Current = "";
    public static string Detail = "";
    private static int _timer;
    private static int _detailTimer;

    public static bool Speaking => _timer > 0;

    // 第一行显示“思考”，第二行显示更细的内容。
    public static void Think(string text, string? detail = null)
    {
        Current = Clip(text, 42);
        Detail = detail == null ? "" : Clip(detail, 52);
        _timer = 240;
        _detailTimer = detail == null ? 0 : 240;
    }

    public static void Say(string text) => Think(text);

    public static void Tick()
    {
        if (_timer > 0) _timer--;
        if (_detailTimer > 0) _detailTimer--;
        if (_detailTimer == 0) Detail = "";
    }

    public static string Line1 => _timer > 0 ? Current : "";
    public static string Line2 => _detailTimer > 0 ? Detail : "";

    private static string Clip(string text, int max)
    {
        var flat = text.Replace("\r", " ").Replace("\n", " ");
        return flat.Length <= max ? flat : flat.Substring(0, max) + "…";
    }

    // 在世界里画气泡：纯文本，跟随宠物头顶。
    public static void DrawBubble(SpriteBatch spriteBatch, Vector2 worldTop)
    {
        if (!Speaking) return;

        var lines = new[] { Line1, Line2 };
        var font = FontAssets.MouseText.Value;
        var screen = worldTop - Main.screenPosition;
        var y = screen.Y;

        foreach (var line in lines)
        {
            if (string.IsNullOrEmpty(line)) continue;
            var size = font.MeasureString(line);
            var pos = new Vector2(screen.X - size.X / 2f, y - size.Y);
            // 底框 + 文字
            spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                new Rectangle((int)(pos.X - 4), (int)(pos.Y - 2), (int)size.X + 8, (int)size.Y + 4),
                new Color(20, 30, 50, 200));
            spriteBatch.DrawString(font, line, pos, new Color(230, 245, 255));
            y -= size.Y + 6;
        }
    }
}
