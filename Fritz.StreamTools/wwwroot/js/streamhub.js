﻿
class StreamHub {
		constructor() {
				this.onFollowers = null;
			this.onViewers = null;
			this.onSentiment = null;
				this.debug = true;
				this._hub = null;
		}

		start(groups) {
				let url = (groups) ? "/followerstream?groups=" + groups : "/followerstream";
				this._hub = new signalR.HubConnectionBuilder()
					.withUrl(url)
					.withHubProtocol(new signalR.protocols.msgpack.MessagePackHubProtocol())
					.build();

				this._hub.onclose(() => {
						if (this.debug) console.debug("hub connection closed");

						// Hub connection was closed for some reason
						let interval = setInterval(() => {
								// Try to reconnect hub every 5 secs
								this.start(groups).then(() => {
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

			this._hub.on('OnNewCode', (newCode) => {
				if (this.debug) console.debug("OnNewCode");
				if (this.onNewCode) this.onNewCode(newCode);
			});

			this._hub.on('OnSentimentUpdated', (newSentiment, oneMinute, fiveMinute, all) => {
				if (this.debug) console.debug("OnSentimentUpdated", { newSentiment, oneMinute, fiveMinute, all });
				if (this.onSentiment) this.onSentiment(newSentiment, oneMinute, fiveMinute, all);
			});

				return this._hub.start();

		}
}
