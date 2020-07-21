using Microsoft.ML.Transforms.Image;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Fritz.Chatbot.ML.DataModels
{

	public class ImageInputData
	{

		//[ImageType(450, 900)]
		[ImageType(416, 416)]
		public Bitmap Image { get; set; }

	}

}
