using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System;
using Robust.Shared.Utility;

namespace Content.Server.ADT.Administration;

public sealed partial class GetCreationDateClass
{
    private static readonly HttpClient _httpClient = new HttpClient();

    public static async Task<string> GetCreationDate(string uuid)
    {
        string url = $"https://auth.spacestation14.com/api/query/userid?userid={uuid}";

        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                Logger.Error($"API request failed for UUID {uuid}: {response.StatusCode}");
                return "Дата не найдена";
            }

            string jsonResponse = await response.Content.ReadAsStringAsync();
            using JsonDocument jsonDoc = JsonDocument.Parse(jsonResponse);

            if (jsonDoc.RootElement.TryGetProperty("createdTime", out JsonElement createdTimeElement) &&
                createdTimeElement.ValueKind != JsonValueKind.Null &&
                createdTimeElement.ValueKind != JsonValueKind.Undefined)
            {
                string? createdTimeStr = createdTimeElement.GetString();
                if (!string.IsNullOrEmpty(createdTimeStr))
                {
                    DateTimeOffset dateObj = DateTimeOffset.Parse(createdTimeStr);
                    return dateObj.ToString("dd.MM.yyyy");
                }
            }

            Logger.Error($"CreatedTime property missing or invalid for UUID: {uuid}");
            return "Дата не найдена";
        }
        catch (HttpRequestException httpEx)
        {
            Logger.Error($"HTTP error for UUID {uuid}: {httpEx.Message}");
            return "Ошибка соединения";
        }
        catch (JsonException jsonEx)
        {
            Logger.Error($"JSON parsing error for UUID {uuid}: {jsonEx.Message}");
            return "Ошибка данных";
        }
        catch (FormatException)
        {
            Logger.Error($"Invalid date format for UUID: {uuid}");
            return "Неверный формат даты";
        }
        catch (Exception ex)
        {
            Logger.Error($"Unexpected error for UUID {uuid}: {ex.Message}");
            return "Ошибка системы";
        }
    }
}
