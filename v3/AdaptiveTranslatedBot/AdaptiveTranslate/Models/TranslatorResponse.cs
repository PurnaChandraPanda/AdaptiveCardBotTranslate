using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AdaptiveTranslate.Models
{
    public class TranslatorResponse
    {
        [JsonProperty("translations")]
        public IEnumerable<TranslatorResult> Translations { get; set; }
    }
}