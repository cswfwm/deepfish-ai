using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using Terraria;
using Terraria.ID;

namespace DeepFishAI;

// 把当前游戏状态整理成一份 JSON，既写给 AI，也落在磁盘上供外部查看。
public static class GameState
{
    public sealed class Snapshot
    {
        public string Time { get; set; } = "";
        public int Life { get; set; }
        public int LifeMax { get; set; }
        public int Mana { get; set; }
        public int ManaMax { get; set; }
        public int PosX { get; set; }
        public int PosY { get; set; }
        public int TileX { get; set; }
        public int TileY { get; set; }
        public int CursorTileX { get; set; }
        public int CursorTileY { get; set; }
        public bool Day { get; set; }
        public bool BloodMoon { get; set; }
        public bool Hardmode { get; set; }
        public int HeldItemType { get; set; }
        public string HeldItemName { get; set; } = "";
        public int HeldItemStack { get; set; }
        public int NearChestCount { get; set; }
        public List<EntityLine> NPCs { get; set; } = new();
        public List<InventoryLine> Hotbar { get; set; } = new();
        public List<string> NearbyTiles { get; set; } = new();
    }

    public sealed class EntityLine
    {
        public int Type { get; set; }
        public string Name { get; set; } = "";
        public int X { get; set; }
        public int Y { get; set; }
        public int Distance { get; set; }
        public bool Hostile { get; set; }
    }

    public sealed class InventoryLine
    {
        public int Slot { get; set; }
        public int Type { get; set; }
        public string Name { get; set; } = "";
        public int Stack { get; set; }
    }

    public static Snapshot Capture()
    {
        var s = new Snapshot();
        try
        {
            var player = Main.LocalPlayer;
            if (player == null) return s;

            s.Time = DateTime.Now.ToString("HH:mm:ss");
            s.Life = player.statLife;
            s.LifeMax = player.statLifeMax2;
            s.Mana = player.statMana;
            s.ManaMax = player.statManaMax2;
            s.PosX = (int)(player.Center.X / 16f);
            s.PosY = (int)(player.Center.Y / 16f);
            s.TileX = s.PosX;
            s.TileY = s.PosY;
            s.CursorTileX = (int)(Main.MouseWorld.X / 16f);
            s.CursorTileY = (int)(Main.MouseWorld.Y / 16f);
            s.Day = Main.dayTime;
            s.BloodMoon = Main.bloodMoon;
            s.Hardmode = Main.hardMode;

            var held = player.inventory[player.selectedItem];
            if (held != null && held.type > 0)
            {
                s.HeldItemType = held.type;
                s.HeldItemName = SafeName(held.type);
                s.HeldItemStack = held.stack;
            }

            // 周围 30 格的 NPC，按距离排序取 8 个。
            var found = new List<EntityLine>();
            for (var i = 0; i < Main.maxNPCs; i++)
            {
                var npc = Main.npc[i];
                if (npc == null || !npc.active) continue;
                var dx = npc.Center.X - player.Center.X;
                var dy = npc.Center.Y - player.Center.Y;
                var dist = (int)(Math.Sqrt(dx * dx + dy * dy) / 16);
                if (dist > 30) continue;
                found.Add(new EntityLine
                {
                    Type = npc.type,
                    Name = SafeNpcName(npc),
                    X = (int)(npc.Center.X / 16),
                    Y = (int)(npc.Center.Y / 16),
                    Distance = dist,
                    Hostile = !npc.friendly,
                });
            }
            found.Sort((a, b) => a.Distance.CompareTo(b.Distance));
            for (var i = 0; i < found.Count && i < 8; i++) s.NPCs.Add(found[i]);

            // 前 10 格物品栏。
            for (var i = 0; i < 10 && i < player.inventory.Length; i++)
            {
                var item = player.inventory[i];
                if (item == null || item.type <= 0) continue;
                s.Hotbar.Add(new InventoryLine
                {
                    Slot = i,
                    Type = item.type,
                    Name = SafeName(item.type),
                    Stack = item.stack,
                });
            }

            // 光标附近 5x5 的方块类型，用于让 AI 判断能不能放。
            for (var dx = -2; dx <= 2; dx++)
            {
                for (var dy = -2; dy <= 2; dy++)
                {
                    var x = s.CursorTileX + dx;
                    var y = s.CursorTileY + dy;
                    if (!WorldGen.InWorld(x, y, 0)) continue;
                    s.NearbyTiles.Add($"{x},{y}={TileTypeAt(x, y)}");
                }
            }

            // 附近的宝箱数量。
            var chests = 0;
            for (var i = 0; i < Main.maxChests; i++)
            {
                var chest = Main.chest[i];
                if (chest == null || chest.x < 0) continue;
                if (Math.Abs(chest.x - s.TileX) > 20 || Math.Abs(chest.y - s.TileY) > 20) continue;
                chests++;
            }
            s.NearChestCount = chests;
        }
        catch (Exception e)
        {
            ModLog.Error("采集状态失败", e);
        }
        return s;
    }

    private static string SafeName(int type)
    {
        try { return Lang.GetItemNameValue(type) ?? ""; } catch { return ""; }
    }

    private static string SafeNpcName(NPC npc)
    {
        try { return npc.GivenName ?? Lang.GetNPCNameValue(npc.type) ?? ""; } catch { return ""; }
    }

    internal static Tile? TileSafe(int x, int y)
    {
        try { return Main.tile[x, y]; } catch { return null; }
    }

    private static int TileTypeAt(int x, int y)
    {
        try
        {
            var tile = TileSafe(x, y);
            return tile.HasTile ? tile.TileType : -1;
        }
        catch { return -1; }
    }

    // 写给磁盘，供外部程序读取。
    public static void WriteToDisk(Snapshot snapshot)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ModPaths.StateFile)!);
            var json = JsonSerializer.Serialize(snapshot, AIConfig.JsonOptions);
            File.WriteAllText(ModPaths.StateFile, json);
        }
        catch (Exception e)
        {
            ModLog.Error("写状态文件失败", e);
        }
    }

    // 给 AI 的紧凑文本，省 token。
    public static string ToPromptText(Snapshot s)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"时间 {s.Time}，{(s.Day ? "白天" : "夜晚")}，血量 {s.Life}/{s.LifeMax}，魔力 {s.Mana}/{s.ManaMax}");
        sb.AppendLine($"玩家格坐标 ({s.TileX},{s.TileY})，光标格 ({s.CursorTileX},{s.CursorTileY})");
        sb.AppendLine($"手持：{s.HeldItemName} x{s.HeldItemStack}（type {s.HeldItemType}）");
        if (s.NPCs.Count > 0)
        {
            sb.Append("附近敌人：");
            var parts = new List<string>();
            foreach (var e in s.NPCs)
                parts.Add($"{e.Name}({e.X},{e.Y}) 距离{e.Distance}{(e.Hostile ? "" : " 友好")}");
            sb.AppendLine(string.Join("；", parts));
        }
        else sb.AppendLine("附近没有敌人");

        if (s.Hotbar.Count > 0)
        {
            sb.Append("物品栏：");
            var parts = new List<string>();
            foreach (var i in s.Hotbar) parts.Add($"[{i.Slot}]{i.Name}x{i.Stack}");
            sb.AppendLine(string.Join(" ", parts));
        }
        if (s.NearChestCount > 0) sb.AppendLine($"附近宝箱 {s.NearChestCount} 个");
        return sb.ToString();
    }
}
