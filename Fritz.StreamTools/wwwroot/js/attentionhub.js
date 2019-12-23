
class AttentionHub {
	constructor() {
		this.onAlertFritz = null;
	  this.onSummonScott = null;
	  this.onPlaySoundEffect = null;
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

		this._hub.on("SummonScott", () => {
			if (this.debug) console.debug("Summoning Scott!");
			if (this.onSummonScott) this.onSummonScott();
		});

	  this._hub.on("PlaySoundEffect", (fileName) => {
			if (this.debug) console.debug(`Playing file: ${fileName}`);
			if (this.onPlaySoundEffect) this.onPlaySoundEffect(fileName);
		});

		this._hub.on("NotifyChannelPoints", redemption => {
			if (this.debug) console.debug(`Redeemed: ${redemption}`);
			if (this.onRedemption) this.onRedemption(redemption);
		});


		return this._hub.start();
	}

	sendTest() {
		if (this.debug) console.debug("sending AlertFritz");
		this._hub.send("AlertFritz");
	}
}
