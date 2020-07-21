using System;
using System.Collections.Generic;
using System.Text;

namespace Fritz.Chatbot.ML.DataModels
{
	public class ImagePredictedLabelWithProbability
	{
		public string ImageId;

		public string PredictedLabel;
		public float Probability { get; set; }

		public long PredictionExecutionTime;
	}

}
