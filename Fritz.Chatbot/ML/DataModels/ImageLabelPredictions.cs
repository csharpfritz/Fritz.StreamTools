using Microsoft.ML.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fritz.Chatbot.ML.DataModels
{
	public class ImageLabelPredictions
	{

		//TODO: Change to fixed output column name for TensorFlow model
		[ColumnName("layer8_leaky")]
		public float[] PredictedLabels;

	}
}
