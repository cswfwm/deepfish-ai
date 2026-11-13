using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace DeepFishAI;

// 兼容 OpenAI Chat Completions 格式：换 BaseUrl 就能接别的服务。
public sealed class AIClient
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(60) };

    public sealed class Reply
    {
        public bool Ok;
        public string Text = "";
        public string Error = "";
    }

    public async Task<Reply> AskAsync(AIConfig cfg, IReadOnlyList<(string Role, string Content)> history, CancellationToken token)
    {
        if (!cfg.HasKey) return new Reply { Ok = false, Error = "未配置 API Key" };

        try
        {
            var messages = new JsonArray();
            messages.Add(new JsonObject { ["role"] = "system", ["content"] = cfg.SystemPrompt });
            foreach (var (role, content) in history)
                messages.Add(new JsonObject { ["role"] = role, ["content"] = content });

            var body = new JsonObject
            {
                ["model"] = cfg.Model,
                ["messages"] = messages,
                ["temperature"] = cfg.Temperature,
                ["max_tokens"] = cfg.MaxTokens,
                ["stream"] = false,
            };

            var url = cfg.BaseUrl.TrimEnd('/') + "/chat/completions";
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json"),
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", cfg.ApiKey);

            using var response = await _http.SendAsync(request, token).ConfigureAwait(false);
            var text = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                return new Reply { Ok = false, Error = $"HTTP {(int)response.StatusCode}: {Trim(text)}" };

            var json = JsonNode.Parse(text);
            var content = json?["choices"]?[0]?["message"]?["content"]?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(content))
                return new Reply { Ok = false, Error = "返回为空：" + Trim(text) };

            return new Reply { Ok = true, Text = content.Trim() };
        }
        catch (OperationCanceledException)
        {
            return new Reply { Ok = false, Error = "请求超时" };
        }
        catch (Exception e)
        {
            return new Reply { Ok = false, Error = e.Message };
        }
    }

    private static string Trim(string text)
        => text.Length <= 200 ? text : text.Substring(0, 200) + "...";
}
