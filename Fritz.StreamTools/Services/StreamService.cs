using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fritz.StreamTools.Services {
  public class StreamService : IStreamService {
    private List<IStreamService> _services;
    public StreamService(
      TwitchService twitch,
      MixerService mixer
    ) {
      _services = new List<IStreamService>();
      _services.Add(twitch);
      _services.Add(mixer);
    }

    public int CurrentFollowerCount { get { return _services.Sum(s => s.CurrentFollowerCount); } }

    public int CurrentViewerCount { get { return _services.Sum(s => s.CurrentViewerCount); } }

    public event EventHandler<ServiceUpdatedEventArgs> Updated {
      add {
        foreach (var s in _services)
          s.Updated += value;
      }
      remove {
        foreach (var s in _services)
          s.Updated -= value;
      }
    }
  }
}
