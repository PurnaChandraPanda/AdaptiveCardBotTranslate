using AdaptiveCards;
using AdaptiveTranslate.Models;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace AdaptiveTranslate.Dialogs
{
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        private readonly HttpClient _client;

        public RootDialog()
        {
            _client = new HttpClient();
        }

        public async Task StartAsync(IDialogContext context)
        {            
            context.Wait(MessageReceivedAsync);
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
        {
            var activity = await result as Activity;

            var message = activity.Text.ToLowerInvariant();

            // if typed 1 or 2, post a simple message response
            if (message == "1" || message == "2")
            {
                await context.PostAsync("you typed 1 or 2");
            }
            else
            {
                var reply = context.MakeMessage();
                var attachment = await TranslateAttachment(CreateAttachment());
                reply.Attachments.Add(attachment);
                await context.PostAsync(reply, CancellationToken.None);                
            }

            context.Wait(MessageReceivedAsync);
        }

        /// <summary>
        /// Create adaptive card attachment
        /// </summary>
        /// <returns></returns>
        private Attachment CreateAttachment()
        {
            AdaptiveCard card = new AdaptiveCard()
            {
                Body = new List<CardElement>()
                {
                    new Container()
                    {
                        Speak = "<s>Hello!</s><s>Are you looking for a flight or a hotel?</s>",
                        Items = new List<CardElement>()
                        {
                            new ColumnSet()
                            {
                                Columns = new List<Column>()
                                {
                                    new Column()
                                    {
                                        Size = ColumnSize.Auto,
                                        Items = new List<CardElement>()
                                        {
                                            new Image()
                                            {
                                                Url = "https://placeholdit.imgix.net/~text?txtsize=65&txt=Adaptive+Cards&w=300&h=300",
                                                Size = ImageSize.Medium,
                                                Style = ImageStyle.Person
                                            }
                                        }
                                    },
                                    new Column()
                                    {
                                        Size = ColumnSize.Stretch,
                                        Items = new List<CardElement>()
                                        {
                                            new TextBlock()
                                            {
                                                Text =  "Hello!",
                                                Weight = TextWeight.Bolder,
                                                IsSubtle = true
                                            },
                                            new TextBlock()
                                            {
                                                Text = "Are you looking for a flight or a hotel?",
                                                Wrap = true
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
                // Buttons
                Actions = new List<ActionBase>() {
                    new ShowCardAction()
                    {
                        Title = "Hotels",
                        Speak = "<s>Hotels</s>",
                        Card = GetHotelSearchCard()
                    },
                    new ShowCardAction()
                    {
                        Title = "Flights",
                        Speak = "<s>Flights</s>",
                        Card = new AdaptiveCard()
                        {
                            Body = new List<CardElement>()
                            {
                                new TextBlock()
                                {
                                    Text = "Flights is not implemented =(",
                                    Speak = "<s>Flights is not implemented</s>",
                                    Weight = TextWeight.Bolder
                                }
                            }
                        }
                    }
                }
            };

            Attachment attachment = new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = card
            };

            return attachment;
        }

        /// <summary>
        /// Get adaptive card generated with Body and Actions
        /// </summary>
        /// <returns></returns>
        private AdaptiveCard GetHotelSearchCard()
        {
            return new AdaptiveCard()
            {
                Body = new List<CardElement>()
                        {
                                // Hotels Search form
                                new TextBlock()
                                {
                                    Text = "Welcome to the Hotels finder!",
                                    Speak = "<s>Welcome to the Hotels finder!</s>",
                                    Weight = TextWeight.Bolder,
                                    Size = TextSize.Large
                                },
                                new TextBlock() { Text = "Please enter your destination:" },
                                new TextInput()
                                {
                                    Id = "Destination",
                                    Speak = "<s>Please enter your destination</s>",
                                    Placeholder = "Miami, Florida",
                                    Style = TextInputStyle.Text
                                },
                                new TextBlock() { Text = "When do you want to check in?" },
                                new DateInput()
                                {
                                    Id = "Checkin",
                                    Speak = "<s>When do you want to check in?</s>"
                                },
                                new TextBlock() { Text = "How many nights do you want to stay?" },
                                new NumberInput()
                                {
                                    Id = "Nights",
                                    Min = 1,
                                    Max = 60,
                                    Speak = "<s>How many nights do you want to stay?</s>"
                                }
                        },
                Actions = new List<ActionBase>()
                        {
                            new SubmitAction()
                            {
                                Title = "Search",
                                Speak = "<s>Search</s>",
                                DataJson = "{ \"Type\": \"HotelSearch\" }"
                            }
                        }
            };
        }

        /// <summary>
        /// Translate the adaptive card attachment from english to french
        /// </summary>
        /// <param name="attachment"></param>
        /// <returns></returns>
        private async Task<Attachment> TranslateAttachment(Attachment attachment)
        {
            var translatedContent = await TranslateAdaptiveCardAsync(attachment, "fr");
            return (translatedContent as JObject).ToObject<Attachment>();
        }

        private async Task<object> TranslateAdaptiveCardAsync(object card, string targetLocale, CancellationToken cancellationToken = default)
        {
            var propertiesToTranslate = new[] { "text", "altText", "fallbackText", "title", "placeholder", "data" };

            var cardJObject = JObject.FromObject(card);
            var list = new List<(JContainer Container, object Key, string Text)>();
            
            recurseThroughJObject(cardJObject, ref propertiesToTranslate, ref list);

            var requestBody = JsonConvert.SerializeObject(list.Select(item => new { item.Text }));

            using (var request = new HttpRequestMessage())
            {
                var uri = $"https://api.cognitive.microsofttranslator.com/translate?api-version=3.0&to={targetLocale}";
                request.Method = HttpMethod.Post;
                request.RequestUri = new Uri(uri);
                request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                var _key = ConfigurationManager.AppSettings["trns:APIKey"];
                request.Headers.Add("Ocp-Apim-Subscription-Key", _key);

                var response = await _client.SendAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

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
            }

            // Return the modified JObject representing the Adaptive Card
            return cardJObject;

        }

        private void recurseThroughJObject(JObject jObject, ref string[] propertiesToTranslate, ref List<(JContainer Container, object Key, string Text)> list)
        {
            var type = jObject["type"];
            var parent = jObject.Parent;
            var grandParent = parent?.Parent;
            // value should be translated in facts and Input.Text, and ignored in Input.Date and Input.Time and Input.Toggle and Input.ChoiceSet and Input.Choice
            var valueIsTranslatable = type?.Type == JTokenType.String && (string)type == "Input.Text"
                || type == null && parent?.Type == JTokenType.Array && grandParent?.Type == JTokenType.Property && ((JProperty)grandParent)?.Name == "facts";

            foreach (var key in ((IDictionary<string, JToken>)jObject).Keys)
            {
                switchOnJToken(jObject, key, propertiesToTranslate.Contains(key) || (key == "value" && valueIsTranslatable), ref propertiesToTranslate, ref list);
            }
        }

        private void switchOnJToken(JContainer jContainer, object key, bool shouldTranslate, ref string[] propertiesToTranslate, ref List<(JContainer Container, object Key, string Text)> list)
        {
            var jToken = jContainer[key];

            switch (jToken.Type)
            {
                case JTokenType.Object:

                    recurseThroughJObject((JObject)jToken, ref propertiesToTranslate, ref list);
                    break;

                case JTokenType.Array:

                    var jArray = (JArray)jToken;
                    var shouldTranslateChild = key as string == "inlines";

                    for (int i = 0; i < jArray.Count; i++)
                    {
                        switchOnJToken(jArray, i, shouldTranslateChild, ref propertiesToTranslate, ref list);
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
    }
}