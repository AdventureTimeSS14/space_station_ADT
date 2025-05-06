using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Robust.Shared.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.Server.GameStates;

namespace Content.Server.ADT.Administration;
public sealed partial class GetCreationData
{
    private static readonly HttpClient _httpClient = new HttpClient();

    public static async Task<string> GetCreationDate(string uuid)
    {
        string url = $"https://auth.spacestation14.com/api/query/userid?userid={uuid}";

        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            string jsonResponse = await response.Content.ReadAsStringAsync();
            JsonDocument jsonDoc = JsonDocument.Parse(jsonResponse);

            if (jsonDoc.RootElement.TryGetProperty("createdTime", out JsonElement createdTimeElement))
            {
                string? createdTimeStr = createdTimeElement.GetString();
                if (!string.IsNullOrEmpty(createdTimeStr))
                {
                    DateTimeOffset dateObj = DateTimeOffset.Parse(createdTimeStr);
                    return dateObj.ToString("dd.MM.yyyy");
                }
                else
                {
                    Logger.Error($"Пустая дата создания для UUID: {uuid}");
                    return "Произошла ошибка";
                }
            }
            else
            {
                Logger.Error($"Свойство createdTime не найдено в ответе API для UUID: {uuid}");
                return "Произошла ошибка";
            }
        }
        catch (HttpRequestException httpEx)
        {
            Logger.Error($"Ошибка HTTP при запросе API для UUID {uuid}: {httpEx.Message}");
            return "Произошла ошибка";
        }
        catch (JsonException jsonEx)
        {
            Logger.Error($"Ошибка парсинга JSON для UUID {uuid}: {jsonEx.Message}");
            return "Произошла ошибка";
        }
        catch (Exception ex)
        {
            Logger.Error($"Неожиданная ошибка для UUID {uuid}: {ex}");
            return "Произошла ошибка";
        }
    }
}
