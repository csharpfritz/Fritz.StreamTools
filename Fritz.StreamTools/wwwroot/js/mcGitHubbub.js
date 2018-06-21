
class McGitHubbub {
		constructor() {
				this.onUpdated = null;
				this.debug = true;
				this._hub = null;                 
		}

		start(groups) {
				let url = (groups) ? "/github?groups=" + groups : "/github";
				this._hub = new signalR.HubConnectionBuilder()
					.withUrl(url)
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

				this._hub.on('OnGitHubUpdated', (repository, userName, commits) => {
						if (this.debug) console.debug("OnGitHubUpdated", { repository, userName, commits });
						if (this.onUpdated) this.onUpdated(repository, userName, commits);
				});

				return this._hub.start();

		}
}
