
class StreamHub {
		constructor() {
				this.onFollowers = null;
				this.onViewers = null;
				this.debug = true;
				this._hub = null;
		}

		start() {
				this._hub = new signalR.HubConnection("/followerstream");

				this._hub.onclose(() => {
						if (this.debug) console.debug("hub connection closed");

						// Hub connection was closed for some reason
						let interval = setInterval(() => {
								// Try to reconnect hub every 5 secs
								this.start().then(() => {
										// Reconnect succeeded
										clearInterval(interval);
										if (this.debug) console.debug("hub reconnected");
								});
						}, 5000);
				});

				this._hub.on('OnFollowersCountUpdated', followerCount => {
						if (this.debug) console.debug("OnFollowersCountUpdated", { followerCount });
						if (this.onFollowers) this.onFollowers(followerCount);
				});
				this._hub.on('OnViewersCountUpdated', (serviceName, viewerCount) => {
						if (this.debug) console.debug("OnViewersCountUpdated", { serviceName, viewerCount });
						if (this.onViewers) this.onViewers(serviceName, viewerCount);
				});

				return this._hub.start();

		}
}

