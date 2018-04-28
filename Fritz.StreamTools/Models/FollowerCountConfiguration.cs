using System;
using System.ComponentModel.DataAnnotations;

namespace Fritz.StreamTools.Models
{
	public class FollowerCountConfiguration
	{
		[Display(Name = "Font Color")]
		public string FontColor { get; set; }

		[Display(Name = "Current Value")]		
		public int CurrentValue { get; set; }

		[Display(Name = "Background Color")]
		public string BackgroundColor { get; set; }

		internal void LoadDefaultSettings(FollowerCountConfiguration config)
		{
			this.BackgroundColor = string.IsNullOrWhiteSpace(this.BackgroundColor) ? config.BackgroundColor : "#000";
			this.FontColor = string.IsNullOrWhiteSpace(this.FontColor) ? config.FontColor : "#32cd32";

		}

	}

}
