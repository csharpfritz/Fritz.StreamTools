
class AttentionHub {
	constructor() {
		this.onAlertFritz = null;
		this.debug = true;
		this._hub = null;
	}

	start(groups) {
		let url = groups ? "/attentionhub?groups=" + groups : "/attentionhub";
		this._hub = new signalR.HubConnectionBuilder()
			.withUrl(url)
			.withHubProtocol(new signalR.protocols.msgpack.MessagePackHubProtocol())
			.build();

		this._hub.onclose(() => {
			if (this.debug) console.debug("hub connection closed");

			// Hub connection was closed for some reason
			let interval = setInterval(() => {
				this.start(groups).then(() => {
					clearInterval(interval);
					if (this.debug) console.debug("hub reconnected");
				});
			}, 5000);
		});

		this._hub.on("ClientConnected", connectionId => {
			if (this.debug) console.debug(`Client connected: ${connectionId}`);
		});

		this._hub.on("AlertFritz", () => {
			if (this.debug) console.debug("AlertFritz");
			if (this.onAlertFritz) this.onAlertFritz();
		});

		return this._hub.start();
	}

	sendTest() {
		if (this.debug) console.debug("sending AlertFritz");
		this._hub.send("AlertFritz");
	}
}
