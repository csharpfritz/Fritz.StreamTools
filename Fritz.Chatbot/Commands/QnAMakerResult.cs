using Newtonsoft.Json;
using System;

namespace Fritz.Chatbot.Commands
{
    internal class QnAMakerResult
    {


        // public string Answer { get; set; }

        // /// <summary>
        // /// The score in range [0, 100] corresponding to the top answer found in the QnA    Service.
        // /// </summary>
        // [JsonProperty(PropertyName = "score")]
        // public double Score { get; set; }

        [JsonProperty("answers")]
        public QnAMakerAnswer[] Answers { get; set; }



    }

    public class QnAMakerAnswer
    {
        [JsonProperty("questions")]
        public string[] Questions { get; set; }

        [JsonProperty("answer")]
        public string Answer { get; set; }

        [JsonProperty("score")]
        public double Score { get; set; }

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("source")]
        public Uri Source { get; set; }

        [JsonProperty("metadata")]
        public object[] Metadata { get; set; }

    }

}
