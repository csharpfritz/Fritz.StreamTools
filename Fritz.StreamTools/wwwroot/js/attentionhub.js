
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
			.withAutomaticReconnect([0, 2000, 10000, 15000, 20000, 30000, 30000, 30000, 30000, 30000, 45000, 60000 ])
			.build();

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

		this._hub.on("Teammate", teammate => {
			if (this.debug) console.debug(`Teammate arrived: ${teammate}`);
			if (this.onTeammate) this.onTeammate(teammate);
		});


		return this._hub.start();
	}

	sendTest() {
		if (this.debug) console.debug("sending AlertFritz");
		this._hub.send("AlertFritz");
	}
}
