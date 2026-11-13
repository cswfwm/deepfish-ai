using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.WorldBuilding;

namespace DeepFishAI;

// 内置简易指令集：AI 每行一条，游戏内逐条执行并汇报结果。
//   say 文本 / goto x y / place x y tileId / dig x y / fill x1 y1 x2 y2 tileId / wait
public static class ScriptEngine
{
    public sealed class Result
    {
        public int Executed;
        public int Failed;
        public readonly List<string> Lines = new();
    }

    public static Result Run(string script)
    {
        var result = new Result();
        try
        {
            var lines = script.Replace("\r", "").Split('\n');
            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (line.Length == 0) continue;
                try
                {
                    if (Execute(line, result)) result.Executed++;
                    else result.Failed++;
                }
                catch (Exception e)
                {
                    result.Failed++;
                    result.Lines.Add($"失败 {line}: {e.Message}");
                }
            }
        }
        catch (Exception e)
        {
            ModLog.Error("执行脚本失败", e);
        }
        return result;
    }

    private static bool Execute(string line, Result result)
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return false;
        var cmd = parts[0].ToLowerInvariant();

        switch (cmd)
        {
            case "wait":
                return true;

            case "say":
                if (parts.Length < 2) return false;
                var text = line.Substring(3).Trim();
                Speech.Say(text);
                result.Lines.Add("说：" + text);
                return true;

            case "goto":
                if (parts.Length < 3) return false;
                return MoveCursor(Int(parts[1]), Int(parts[2]), result);

            case "place":
                if (parts.Length < 4) return false;
                return Place(Int(parts[1]), Int(parts[2]), Int(parts[3]), result);

            case "dig":
                if (parts.Length < 3) return false;
                return Dig(Int(parts[1]), Int(parts[2]), result);

            case "fill":
                if (parts.Length < 6) return false;
                return Fill(Int(parts[1]), Int(parts[2]), Int(parts[3]), Int(parts[4]), Int(parts[5]), result);

            default:
                return false;
        }
    }

    private static int Int(string text) => int.TryParse(text, out var v) ? v : 0;

    private static bool MoveCursor(int x, int y, Result result)
    {
        if (!WorldGen.InWorld(x, y, 0)) return false;
        Main.LocalPlayer.position.X = x * 16 - Main.LocalPlayer.width / 2f;
        result.Lines.Add($"移动光标到 {x},{y}");
        return true;
    }

    private static bool Place(int x, int y, int tileType, Result result)
    {
        if (!WorldGen.InWorld(x, y, 0)) return false;
        if (tileType <= 0) return false;
        var placed = WorldGen.PlaceTile(x, y, tileType, mute: true);
        if (placed)
        {
            result.Lines.Add($"放置 {tileType} 于 {x},{y}");
            return true;
        }
        return false;
    }

    private static bool Dig(int x, int y, Result result)
    {
        if (!WorldGen.InWorld(x, y, 0)) return false;
        var tile = GameState.TileSafe(x, y);
        if (tile == null || !tile.HasTile) return false;
        WorldGen.KillTile(x, y, noItem: false);
        result.Lines.Add($"挖掉 {x},{y}");
        return true;
    }

    private static bool Fill(int x1, int y1, int x2, int y2, int tileType, Result result)
    {
        var placed = 0;
        var fromX = Math.Min(x1, x2);
        var toX = Math.Max(x1, x2);
        var fromY = Math.Min(y1, y2);
        var toY = Math.Max(y1, y2);
        // 限制单次规模，避免 AI 一笔画掉整张地图。
        if ((toX - fromX + 1) * (toY - fromY + 1) > 2000) return false;

        for (var x = fromX; x <= toX; x++)
        {
            for (var y = fromY; y <= toY; y++)
            {
                if (!WorldGen.InWorld(x, y, 0)) continue;
                if (WorldGen.PlaceTile(x, y, tileType, mute: true)) placed++;
            }
        }
        result.Lines.Add($"填充 {placed} 格");
        return placed > 0;
    }
}
