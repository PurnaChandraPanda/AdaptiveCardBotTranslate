// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.BotBuilderSamples.Translation.Model;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.BotBuilderSamples.Translation
{
    public class MicrosoftTranslator
    {
        private const string Host = "https://api.cognitive.microsofttranslator.com";
        private const string Path = "/translate?api-version=3.0";
        private const string UriParams = "&to=";

        private static HttpClient _client = new HttpClient();

        private readonly string _key;


        public MicrosoftTranslator(IConfiguration configuration)
        {
            var key = configuration["TranslatorKey"];
            _key = key ?? throw new ArgumentNullException(nameof(key));
        }

        public async Task<string> TranslateAsync(string text, string targetLocale, CancellationToken cancellationToken = default(CancellationToken))
        {
            // From Cognitive Services translation documentation:
            // https://docs.microsoft.com/en-us/azure/cognitive-services/translator/quickstart-csharp-translate
            var body = new object[] { new { Text = text } };
            var requestBody = JsonConvert.SerializeObject(body);

            using (var request = new HttpRequestMessage())
            {
                var uri = Host + Path + UriParams + targetLocale;
                request.Method = HttpMethod.Post;
                request.RequestUri = new Uri(uri);
                request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                request.Headers.Add("Ocp-Apim-Subscription-Key", _key);

                var response = await _client.SendAsync(request, cancellationToken);

                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<TranslatorResponse[]>(responseBody);

                return result?.FirstOrDefault()?.Translations?.FirstOrDefault()?.Text;
            }
        }

		public async Task<object> TranslateAdaptiveCardAsync(object card, string targetLocale, CancellationToken cancellationToken = default)
		{
			var propertiesToTranslate = new[] { "text", "altText", "fallbackText", "title", "placeholder", "data" };

			var cardJObject = JObject.FromObject(card);
			var list = new List<(JContainer Container, object Key, string Text)>();
			
			void recurseThroughJObject(JObject jObject)
			{
				var type = jObject["type"];
				var parent = jObject.Parent;
				var grandParent = parent?.Parent;
				// value should be translated in facts and Input.Text, and ignored in Input.Date and Input.Time and Input.Toggle and Input.ChoiceSet and Input.Choice
				var valueIsTranslatable = type?.Type == JTokenType.String && (string)type == "Input.Text"
					|| type == null && parent?.Type == JTokenType.Array && grandParent?.Type == JTokenType.Property && ((JProperty)grandParent)?.Name == "facts";

				foreach (var key in ((IDictionary<string, JToken>)jObject).Keys)
				{
					switchOnJToken(jObject, key, propertiesToTranslate.Contains(key) || (key == "value" && valueIsTranslatable));
				}
			}

			void switchOnJToken(JContainer jContainer, object key, bool shouldTranslate)
			{
				var jToken = jContainer[key];

				switch (jToken.Type)
				{
					case JTokenType.Object:

						recurseThroughJObject((JObject)jToken);
						break;

					case JTokenType.Array:

						var jArray = (JArray)jToken;
						var shouldTranslateChild = key as string == "inlines";

						for (int i = 0; i < jArray.Count; i++)
						{
							switchOnJToken(jArray, i, shouldTranslateChild);
						}

						break;

					case JTokenType.String:

						if (shouldTranslate)
						{
							// Store the text to translate as well as the JToken information to apply the translated text to
							list.Add((jContainer, key, (string)jToken));
						}

						break;
				}
			}

			recurseThroughJObject(cardJObject);

			// From Cognitive Services translation documentation:
			// https://docs.microsoft.com/en-us/azure/cognitive-services/translator/quickstart-csharp-translate
			var requestBody = JsonConvert.SerializeObject(list.Select(item => new { item.Text }));

			using (var request = new HttpRequestMessage())
			{
				var uri = $"https://api.cognitive.microsofttranslator.com/translate?api-version=3.0&to={targetLocale}";
				request.Method = HttpMethod.Post;
				request.RequestUri = new Uri(uri);
				request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
				request.Headers.Add("Ocp-Apim-Subscription-Key", _key);

				var response = await _client.SendAsync(request, cancellationToken);

				response.EnsureSuccessStatusCode();

				var responseBody = await response.Content.ReadAsStringAsync();
				var result = JsonConvert.DeserializeObject<TranslatorResponse[]>(responseBody);

				if (result == null)
				{
					return null;
				}

				for (int i = 0; i < result.Length && i < list.Count; i++)
				{
					var item = list[i];
					var translatedText = result[i]?.Translations?.FirstOrDefault()?.Text;

					if (!string.IsNullOrWhiteSpace(translatedText))
					{
						// Modify each stored JToken with the translated text
						item.Container[item.Key] = translatedText;  
					}
				}

				// Return the modified JObject representing the Adaptive Card
				return cardJObject;
			}
		}
	}
}
