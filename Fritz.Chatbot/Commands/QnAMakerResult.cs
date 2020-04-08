using Newtonsoft.Json;
using System;

namespace Fritz.Chatbot.Commands
{
    public class QnAMakerResult
    {

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
