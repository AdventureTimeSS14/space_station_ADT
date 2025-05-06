using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

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
                    return "Дата создания отсутствует или пустая";
                }
            }
            else
            {
                return "Дата создания не найдена в ответе API";
            }
        }
        catch (HttpRequestException httpEx)
        {
            return $"Ошибка при запросе API: {httpEx.Message}";
        }
        catch (JsonException)
        {
            return "Ошибка при разборе ответа API (неправильный формат JSON)";
        }
        catch (Exception ex)
        {
            return $"Произошла ошибка: {ex.Message}";
        }
    }
}
