## Intro
The goal of this PR was to lay the foundation for the moderator bot, and as Mixer don't have a C# client library I started with that. There is a lot of code (sorry!), but all of it was needed, and I have tried to split it up in sub services that MixerService can use (MixerFactory, MixerRestClient, MixerChat, MixerConstellation, ...).

**It has tests (42+) that simulates packets received from the websocket, so everything is tested from the bottom up. It also includes test for the MixerRestClient that uses a simulated HttpClient**

I have included a sample chatbot for trying out the service. It understands the following commands:
`!ping, !echo, !uptime, !quote !help`

## Configuration
````bash
{
  "StreamServices": {
    "Mixer": {
      "ReconnectDelay": "00:00:05",   # hh:mm:ss
      "Channel": "<channel_name>",
      "Token": "<oauth_user_token>"
    }
  }
}
````
**Token should be stored in secrets!**

## Authorization
This can run anonymously (no token in settings) but to be able to do anything useful, it requires OAuth implicit grant authorization.
Go to http://www.mixerdevtools.com/gettoken, set the scopes needed: 
````
channel:update:self
chat:bypass_links
chat:bypass_slowchat
chat:change_ban
chat:chat
chat:connect
chat:timeout
chat:whisper
````

Click 'Get OAuth Token' and save the token in secrets under `StreamServices:Mixer:Token` 
**DO NOT SHOW IT ON STREAM ;)**

You can use your own mixer account or create a dedicated bot user.
The token can always be revoked under Account -> OAUTH-APPS, remove 'Mixer Dev Tools'

## The IChatService interface
If a stream-service implements IChatService, it is automatically registered in DI. This interface allows you to receive chat messages, send/whisper messages and ban/timeout users.

````c#
public interface IChatService
{
  string Name { get; }
  bool IsAuthenticated { get; }

  event EventHandler<ChatMessageEventArgs> ChatMessage;
  event EventHandler<ChatUserInfoEventArgs> UserJoined;
  event EventHandler<ChatUserInfoEventArgs> UserLeft;

  Task<bool> SendMessageAsync(string message);
  Task<bool> SendWhisperAsync(string userName, string message);
  Task<bool> TimeoutUserAsync(string userName, TimeSpan time);
  Task<bool> BanUserAsync(string userName);
  Task<bool> UnbanUserAsync(string userName);
}

public class ChatMessageEventArgs : EventArgs
{
  public string ServiceName { get; set; }
  public Dictionary<string, object> Properties { get; }

  public int UserId { get; set; }
  public string UserName { get; set; }
  public bool IsWhisper { get; set; }
  public bool IsOwner { get; set; }
  public bool IsModerator { get; set; }
  public string Message { get; set; }
}

public class ChatUserInfoEventArgs : EventArgs
{
  public string ServiceName { get; set; }
  public Dictionary<string, object> Properties { get; }

  public int UserId { get; set; }
  public string UserName { get; set; }
}
````
Additionally it implements a mixer specific interface:
````c# 
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
````  
## Logging
You can set the LogLevel of MixerLive or MixerChat to "Trace" to make it show packets on the websocket. It WILL mask out the chatAuth key in the log
````json
{
  "Logging": {
    "LogLevel": {
      "MixerService": "Trace",
      "MixerRestClient": "Trace",
      "MixerConstellation": "Trace",
      "MixerChat": "Trace"
    }
  }
}
````

## References
https://dev.mixer.com/rest.html
https://dev.mixer.com/reference/oauth/index.html
https://dev.mixer.com/reference/chat/index.html
https://dev.mixer.com/reference/constellation/index.html


