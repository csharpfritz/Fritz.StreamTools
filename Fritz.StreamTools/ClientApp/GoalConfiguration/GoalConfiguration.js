var log = function (message, params) {
	// console.log(message, params);

};

var supportedFonts = [];

// "fontname",
const quickPreviewButton = "preview";
const quickPreviewTextBoxes = ["preview", "caption", "goal", "current", "emptyBgColor", "emptyFontColor", "bgcolor1", "bgblend1"];

(function () {

	document.getElementById('fontsPanel').style.display = 'none';

	onload();

	loadPreview();

	for (var tb of quickPreviewTextBoxes) {
		document.getElementById(tb).onchange = loadPreview;
	}

	document.getElementById(quickPreviewButton).onchange = loadPreview;


	function onload() {

		const bgArray = new Array();

		for (var i = 0; i < localStorage.length; i++) {
			var key = localStorage.key(i);
			const item = localStorage.getItem(key);

			if (key === 'supportedFonts') {

				log("setting supported fonts from local storage");
				const fonts = JSON.parse(item);
				setSupportedFonts(fonts);
				continue;

			}

			log(key, item);

			// store an array of all the required color pickers
			if (key.substr(0, 2) == "bg") {
				bgArray.push(key);
			} else {

				var el = document.getElementById(key);
				if (el) {
					el.value = item;
				}
			}

		}

		const sortedArray = bgArray.sort();

		for (var i of sortedArray) {

			if (i.substr(0, 7) == "bgcolor") {

				if (!document.getElementById(i)) {
					var key = i.substr(7);
					addColor(key);
				}

			}

		}

		for (var i of sortedArray) {

			var el = document.getElementById(i);
			if (el) {
				el.value = localStorage.getItem(i);
			}

		}

		if (supportedFonts.length == 0)
			googleFontsAdapter(setSupportedFontsFromApi);

		}
})();

	function loadPreview() {

		const iframeWidth = document.getElementById("widgetPreview").clientWidth - 40;
		const fontName = document.getElementById("fontname").value;

		if (supportedFonts.length > 0 && filterFontList(supportedFonts, fontName, filterExactMatchOperation).length !== 1) {
			alert('Font not supported by Google Fonts');
			return;
		}

		var urlTemplate = "/followers/goal/";
		urlTemplate += `${document.getElementById("goal").value}/`;
		urlTemplate += `${document.getElementById("caption").value}`;
		urlTemplate += `?width=${iframeWidth}`;
		urlTemplate += getBgColors();
		urlTemplate += getBgBlend();
		urlTemplate += `&emptyBgColor=${escape(document.getElementById("emptyBgColor").value)}`;
		urlTemplate += `&emptyFontColor=${escape(document.getElementById('emptyFontColor').value)}`;
		urlTemplate += `&fontName=${escape(fontName)}`;

		document.getElementById("widgetPreview").src = urlTemplate + `&current=${document.getElementById("current").value}`
		log(urlTemplate);

		document.getElementById("outputUrl").textContent = urlTemplate;
		document.getElementById("outputUrl").href = urlTemplate;
		saveValues();

	};

	function saveValues() {

		localStorage.clear();

		const elements = Array.from(document.getElementsByTagName("input"));
		for (let el of elements) {

			log(`Saving value: ${el.id}: ${el.value}`);

			localStorage.setItem(el.id, el.value);

		}

		localStorage.setItem('supportedFonts', JSON.stringify(supportedFonts));

	}

	// build the bgcolors parameter
	function getBgColors() {
		const spans = document.getElementsByName("spanBGColor");
		var result = "";
		if (spans) {
			for (let i = 0; i < spans.length; i++) {
					result += `,${escape(spans[i].getElementsByTagName('input')[0].value)}`;
			}
			result = `&bgcolors=${result.substr(1)}`;
		}
		return result;
	}

	// build the bgblend parameter
	function getBgBlend() {
			const spans = document.getElementsByName("spanBGColor");
		var result = "";
		if (spans) {
			for (let i = 0; i < spans.length; i++) {
					result += `,${spans[i].getElementsByTagName('input')[1].value}`;
			}
			result = `&bgblend=${result.substr(1)}`;
		}
		return result;
	}

	// retrieve the supported font names from google api
	function googleFontsAdapter(setter) {
	//Reference for the HTTP in vanilla JS https://www.sitepoint.com/guide-vanilla-ajax-without-jquery
		let api = 'https://www.googleapis.com/webfonts/v1/webfonts?key=' + GoogleFontsApiKey;

		if (!GoogleFontsApiKey)
		{
			setter([{ 'family': 'Abel' }, { 'family': 'Abril Fatface' }, { 'family': 'Acme' }, { 'family': 'Alegreya' }, { 'family': 'Alex Brush' }, { 'family': 'Amaranth' }, { 'family': 'Amatic SC' }, { 'family': 'Anton' }, { 'family': 'Arbutus Slab' }, { 'family': 'Architects Daughter' }, { 'family': 'Archivo' }, { 'family': 'Archivo Black' }, { 'family': 'Arima Madurai' }, { 'family': 'Asap' }, { 'family': 'Bad Script' }, { 'family': 'Baloo Bhaina' }, { 'family': 'Bangers' }, { 'family': 'Berkshire Swash' }, { 'family': 'Bitter' }, { 'family': 'Boogaloo' }, { 'family': 'Bree Serif' }, { 'family': 'Bungee Shade' }, { 'family': 'Cantata One' }, { 'family': 'Catamaran' }, { 'family': 'Caveat' }, { 'family': 'Caveat Brush' }, { 'family': 'Ceviche One' }, { 'family': 'Chewy' }, { 'family': 'Contrail One' }, { 'family': 'Crete Round' }, { 'family': 'Dancing Script' }, { 'family': 'Exo 2' }, { 'family': 'Fascinate' }, { 'family': 'Francois One' }, { 'family': 'Freckle Face' }, { 'family': 'Fredoka One' }, { 'family': 'Gloria Hallelujah' }, { 'family': 'Gochi Hand' }, { 'family': 'Great Vibes' }, { 'family': 'Handlee' }, { 'family': 'Inconsolata' }, { 'family': 'Indie Flower' }, { 'family': 'Kaushan Script' }, { 'family': 'Lalezar' }, { 'family': 'Lato' }, { 'family': 'Libre Baskerville' }, { 'family': 'Life Savers' }, { 'family': 'Lobster' }, { 'family': 'Lora' }, { 'family': 'Luckiest Guy' }, { 'family': 'Marcellus SC' }, { 'family': 'Merriweather' }, { 'family': 'Merriweather Sans' }, { 'family': 'Monoton' }, { 'family': 'Montserrat' }, { 'family': 'News Cycle' }, { 'family': 'Nothing You Could Do' }, { 'family': 'Noto Serif' }, { 'family': 'Oleo Script Swash Caps' }, { 'family': 'Open Sans' }, { 'family': 'Open Sans Condensed' }, { 'family': 'Oranienbaum' }, { 'family': 'Oswald' }, { 'family': 'PT Sans' }, { 'family': 'PT Sans Narrow' }, { 'family': 'PT Serif' }, { 'family': 'Pacifico' }, { 'family': 'Patrick Hand' }, { 'family': 'Peralta' }, { 'family': 'Permanent Marker' }, { 'family': 'Philosopher' }, { 'family': 'Play' }, { 'family': 'Playfair Display' }, { 'family': 'Playfair Display SC' }, { 'family': 'Poiret One' }, { 'family': 'Press Start 2P' }, { 'family': 'Prosto One' }, { 'family': 'Quattrocento' }, { 'family': 'Questrial' }, { 'family': 'Quicksand' }, { 'family': 'Raleway' }, { 'family': 'Rancho' }, { 'family': 'Righteous' }, { 'family': 'Roboto' }, { 'family': 'Roboto Condensed' }, { 'family': 'Roboto Slab' }, { 'family': 'Rubik' }, { 'family': 'Rye' }, { 'family': 'Satisfy' }, { 'family': 'Shadows Into Light' }, { 'family': 'Shojumaru' }, { 'family': 'Sigmar One' }, { 'family': 'Skranji' }, { 'family': 'Slabo 27px' }, { 'family': 'Special Elite' }, { 'family': 'Tinos' }, { 'family': 'Ultra' }, { 'family': 'UnifrakturMaguntia' }, { 'family': 'VT323' }, { 'family': 'Yanone Kaffeesatz' }]);
			return;
		}

		let xhr = new XMLHttpRequest();
		xhr.open('GET', api);
		xhr.send(null);

		log('Calling google fonts api');

		xhr.onreadystatechange = function() {
			let DONE = 4; // readyState 4 means the request is done.
			let OK = 200; // status 200 is a successful return.
			if (xhr.readyState === DONE) {
				if (xhr.status === OK)
					setter(JSON.parse(xhr.responseText).items); // 'This is the returned text.'
				else
					log('Error: ' + xhr.status); // An error occurred during the request.
			}
		}
	}

	function setSupportedFonts(fonts) {
		localStorage.setItem('supportedFonts', JSON.stringify(supportedFonts));
		updateFontList(filterFontList(supportedFonts));
	}

	function setSupportedFontsFromApi(fonts) {
		supportedFonts = fonts.map((v) => v.family);
		setSupportedFonts(supportedFonts);
	}

	function filterExactMatchOperation(currentItem, searchValue) {
		return !searchValue || currentItem === searchValue;
	}

	function filterPartialMatchOperation(currentItem, searchValue) {
		return !searchValue || currentItem.indexOf(searchValue) > -1;
	}

	function filterFontList(fonts, searchValue, searchOperation = filterPartialMatchOperation) {
		return fonts.filter(function (currentItem) { return searchOperation(currentItem, searchValue); });
	}

	function createCssLinkTag(encoded) {

		if (document.getElementById(`font-${encoded}`))
			return;

		const l = document.createElement('link');
		l.type = 'text/css';
		l.rel = 'stylesheet';
		l.id = `font-${encoded}`;
		l.href = `https://fonts.googleapis.com/css?family=${encoded}`;

		document.getElementsByTagName('head')[0].appendChild(l);

	}

	function createFontOption(fontFamily) {

		const option = document.createElement("option");
		option.style.fontFamily = fontFamily;
		option.text = fontFamily;
		return option;

	}

	function updateFontList(fonts) {

		const control = document.getElementById('fontNames');
		control.innerHTML = '';

		for (let current of fonts) {
			const encoded = encodeURI(current);

			createCssLinkTag(encoded);

			control.add(createFontOption(current));
		}

	}

	document.getElementById('fontname').onkeyup = function (d) {

		// UP: 38,  DOWN: 40
		const keyCodeTab = 9;
		const keyCodeEnter = 13;

		if (d.keyCode == keyCodeEnter || d.keyCode == keyCodeTab) {
			log("Selecting the current font..");
			document.getElementById('fontsPanel').style.display = 'none';
			loadPreview();
			return;

		}

		// log(d.keyCode);
		if (
			d.keyCode != 8
			&& d.keyCode != 32
			&& d.keyCode != 46
			&& (
				d.keyCode < 65
				|| d.keyCode > 90)
		)
			return;
		document.getElementById('fontsPanel').style.display = '';
		updateFontList(filterFontList(supportedFonts, this.value));

	};

	document.getElementById('fontNames').onchange = function () {

		document.getElementById('fontsPanel').style.display = 'none';
		document.getElementById('fontname').value = this.value;

		loadPreview();

		};

function addColor(key) {

	key = key || null;
	const spans = document.getElementsByName("spanBGColor");
	if (spans) {
		const addButton = spans[spans.length - 1].nextSibling,
			newNode = spans[spans.length - 1].cloneNode(true),
			bgColorField = newNode.getElementsByTagName('input')[0],
			bgBlendField = newNode.getElementsByTagName('input')[0];

		bgColorField.id = key ? `bgcolor${key}` : `bgcolor${spans.length + 1}`;
		bgColorField.onchange = loadPreview;
		bgBlendField.id = key ? `bgblend${key}` : `bgblend${spans.length + 1}`;
		bgBlendField.onchange = loadPreview;

		addButton.parentNode.insertBefore(newNode, addButton);
		newNode.getElementsByClassName("input-group-text btn btn-danger")[0].onclick = removeColor;
		loadPreview();
	}
}

function removeColor() {
		if (this === window) return;

		this.closest('[name=spanBGColor]').remove();
		renumberColors();
		loadPreview();
}

function renumberColors() {
	const spans = document.getElementsByName("spanBGColor");
	for (let i = 0; i < spans.length; i++) {
		spans[i].getElementsByTagName('input')[0].id = `bgcolor${i + 1}`;
		spans[i].getElementsByTagName('input')[1].id = `bgblend${i + 1}`;
	}
}



