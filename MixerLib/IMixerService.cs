using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Fritz.StreamLib.Core;

namespace MixerLib
{
	public interface IMixerService : IChatService, IStreamService
	{
		uint? ChannelID { get; }
		string ChannnelName { get; }
		uint? UserId { get; }
		string UserName { get; }

		event EventHandler<FollowedEventArgs> Followed;
		event EventHandler<HostedEventArgs> Hosted;
		event EventHandler<SubscribedEventArgs> Subscribed;
		event EventHandler<ResubscribedEventArgs> Resubscribed;

		Task<IEnumerable<API.GameTypeSimple>> LookupGameTypeAsync(string query);
		Task<API.GameTypeSimple> LookupGameTypeByIdAsync(uint gameTypeId);
		Task<(string title, uint? gameTypeId)> GetChannelInfoAsync();
		Task UpdateChannelInfoAsync(string title, uint? gameTypeId = null);
	}
}
