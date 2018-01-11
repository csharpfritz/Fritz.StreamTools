using System;
using Microsoft.Extensions.Configuration;

namespace Fritz.StreamTools.Services {
    public interface IStreamService {
        int CurrentFollowerCount { get; }
        int CurrentViewerCount { get; }

        event EventHandler<ServiceUpdatedEventArgs> Updated;
    }
}