var log = function (message, params) {
	// console.log(message, params);

};


// "fontname",

(function () {

	document.getElementById('fontsPanel').style.display = 'none';

	onload();

	loadPreview();

	const quickPreviewButton = "preview";
	const quickPreviewTextBoxes = ["caption", "goal", "current", "emptyBgColor", "emptyFontColor", "bgcolor1", "bgblend1"];

	for (var tb of quickPreviewTextBoxes) {
		document.getElementById(tb).onchange = loadPreview;
	}

	document.getElementById(quickPreviewButton).onclick = loadPreview;


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

		InitGoogleFonts();


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