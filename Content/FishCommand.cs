using System;
using Terraria.ModLoader;

namespace DeepFishAI;

// 在聊天框输入 /鱼 <内容> 就立刻问一次 AI。
public class FishCommand : ModCommand
{
    public override string Command => "鱼";
    public override CommandType Type => CommandType.Chat;
    public override string Usage => "/鱼 你想说的话";
    public override string Description => "和大肥鱼说话，它会思考并执行指令";

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        var text = ExtractArgs(input);
        if (string.IsNullOrWhiteSpace(text))
        {
            Speech.Think("叫我干嘛？");
            caller.Reply("用法：/鱼 帮我在这边盖四面墙");
            return;
        }

        Speech.Think("嗯……让我想想", text);
        AISystem.AskFromChat(text);
        caller.Reply("大肥鱼收到了，正在思考……");
    }

    // input 是整行（含命令名），取出命令名之后的部分，保留原始大小写与空格。
    private static string ExtractArgs(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "";
        var trimmed = input.Trim();
        if (!trimmed.StartsWith("/")) return trimmed;
        trimmed = trimmed.Substring(1);
        var space = trimmed.IndexOf(' ');
        return space < 0 ? "" : trimmed.Substring(space + 1).Trim();
    }
}
