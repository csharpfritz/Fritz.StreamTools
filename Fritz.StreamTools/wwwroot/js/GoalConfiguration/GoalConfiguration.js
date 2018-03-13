var isLoadingFromStorage = false;

(function () {

	document.getElementById('fontsPanel').style.display = 'none';

	onload();

	loadPreview();

	InitPreview();

	function onload() {

		isLoadingFromStorage = true; 

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

		InitGoogleFonts();

		ConfigureDefaultFontColors();

		isLoadingFromStorage = false; 

	}

})();


function saveValues() {

	localStorage.clear();

	const elements = Array.from(document.getElementsByTagName("input"));
	for (let el of elements) {

		log(`Saving value: ${el.id}: ${el.value}`);

		localStorage.setItem(el.id, el.value);

	}

	localStorage.setItem('supportedFonts', JSON.stringify(supportedFonts));

}

// retrieve the supported font names from google api

function filterExactMatchOperation(currentItem, searchValue) {
	return !searchValue || currentItem === searchValue;
}

function filterPartialMatchOperation(currentItem, searchValue) {
	return !searchValue || currentItem.indexOf(searchValue) > -1;
}

function filterFontList(fonts, searchValue, searchOperation = filterPartialMatchOperation) {
	return fonts.filter(function (currentItem) {
		return searchOperation(currentItem, searchValue);
	});
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

document.getElementById(ConfigurationModel.FontName).onkeyup = function (d) {

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
		d.keyCode != 8 &&
		d.keyCode != 32 &&
		d.keyCode != 46 &&
		(
			d.keyCode < 65 ||
			d.keyCode > 90)
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
			bgBlendField = newNode.getElementsByTagName('input')[1];

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

function colourIsLight (r, g, b) {

	// Counting the perceptive luminance
	// human eye favors green color... 
	var a = 1 - (0.299 * r + 0.587 * g + 0.114 * b) / 255;
	console.log(a);
	return (a < 0.5);
}

function ConfigureDefaultFontColors() {

	var buttons = ["defaultEmptyFontColor", "defaultFillFontColor"];

	for (var button of buttons) {

		var thisButton = document.getElementById(button);
		thisButton.onclick = function (event) {

			event.preventDefault()

			var targetColorEl = document.getElementById(this.getAttribute("data-target"));
			var inspectColor = this.getAttribute("data-background");
			var inspectRgb = hexToRgb(document.getElementById(inspectColor).value.replace("#", ""));
			var newFontColor = colourIsLight(inspectRgb[0], inspectRgb[1], inspectRgb[2]) ? "#000000" : "#FFFFFF";
			
			targetColorEl.value = newFontColor;
			loadPreview();

		};

	}

}

function hexToRgb(hex) {
	var bigint = parseInt(hex, 16);
	var r = (bigint >> 16) & 255;
	var g = (bigint >> 8) & 255;
	var b = bigint & 255;

	return [r, g, b];
}
