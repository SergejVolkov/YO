using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using YO.Internals.Shikimori.Parameters;

namespace YO.Internals.Shikimori.Categories
{
	public abstract class BaseCategory
	{
		private const int RequestsPerSecond = 5;
		private readonly HttpClient _httpClient;
		private readonly string _baseUrl;

		protected BaseCategory(HttpClient httpClient, string baseUrl)
		{
			_httpClient = httpClient;
			_baseUrl = baseUrl;
		}

		protected async Task<TResult?> GetAsync<TResult>(string requestPath, ParametersBase parameters)
		{
			await Task.Delay(1000 / RequestsPerSecond);
			
			var fullPath = parameters.BuildQuery(_baseUrl + requestPath);
			var httpRequest = new HttpRequestMessage(HttpMethod.Get, fullPath);
			var httpResponse = await _httpClient.SendAsync(httpRequest);

			if (!httpResponse.IsSuccessStatusCode)
			{
				throw new Exception($"ShikimoriRequest error: {httpResponse.StatusCode}");
			}

			var response = await httpResponse.Content.ReadAsStringAsync();
			return HandleResponse<TResult>(response);
		}

		private static TResult? HandleResponse<TResult>(string response)
		{
			try
			{
				return JsonConvert.DeserializeObject<TResult>(response);
			} catch (JsonSerializationException)
			{
				return default;
			}
		}
	}
}