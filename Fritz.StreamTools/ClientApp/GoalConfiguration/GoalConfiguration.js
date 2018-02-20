	(function () {

		document.getElementById('fontsPanel').style.display = 'none';

		var supportedFonts = [];

		onload();

		LoadPreview();

		document.getElementById("preview").onclick = LoadPreview;

		function LoadPreview() {

			var iframeWidth = document.getElementById("widgetPreview").clientWidth - 40;
			let fontName = document.getElementById("fontname").value;

			if (supportedFonts.length > 0 && filterFontList(supportedFonts, fontName, filterExactMatchOperation).length !== 1) {
				alert('Font not supported by Google Fonts');
				return;
			}

			var urlTemplate = "/followers/goal/";
			urlTemplate += `${document.getElementById("goal").value}/`;
			urlTemplate += `${document.getElementById("caption").value}`;
			urlTemplate += `?width=${iframeWidth}`;
			urlTemplate += getBGColors();
			urlTemplate += getBGBlend();
			urlTemplate += `&emptyBgColor=${escape(document.getElementById("emptyBgColor").value)}`;
			urlTemplate += `&emptyFontColor=${escape(document.getElementById('emptyFontColor').value)}`;
			urlTemplate += `&fontName=${escape(fontName)}`;

			document.getElementById("widgetPreview").src = urlTemplate + `&current=${document.getElementById("current").value}`
			console.log(urlTemplate);

			document.getElementById("outputUrl").textContent = urlTemplate;
			document.getElementById("outputUrl").href = urlTemplate;
			saveValues();

		}

		function onload() {

			var bgArray = new Array();

			for (var i = 0; i < localStorage.length; i++) {
				var key = localStorage.key(i);
				var item = localStorage.getItem(key);

				if (key === 'supportedFonts') {

					var fonts = JSON.parse(item);
					setSupportedFonts(fonts);
					continue;

				}

				console.log(key, item);

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

			var sortedArray = bgArray.sort();

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

		function saveValues() {

			localStorage.clear();

			var elements = Array.from(document.getElementsByTagName("input"));
			for (var el of elements) {

				console.log(`Saving value: ${el.id}: ${el.value}`);

				localStorage.setItem(el.id, el.value);

			}

			localStorage.setItem('supportedFonts', JSON.stringify(supportedFonts));

		}

		// build the bgcolors parameter
		function getBGColors() {
			var spans = document.getElementsByName("spanBGColor");
			var result = "";
			if (spans) {
				for (var i = 0; i < spans.length; i++) {
					result += `,${escape(spans[i].children[0].value)}`;
				}
				result = `&bgcolors=${result.substr(1)}`;
			}
			return result;
		}

		// build the bgblend parameter
		function getBGBlend() {
			var spans = document.getElementsByName("spanBGColor");
			var result = "";
			if (spans) {
				for (var i = 0; i < spans.length; i++) {
					result += `,${spans[i].children[1].value}`;
				}
				result = `&bgblend=${result.substr(1)}`;
			}
			return result;
		}

		// retrieve the supported font names from google api
		function googleFontsAdapter(setter) {
			/* Reference
			for the HTTP in vanilla JS https: //www.sitepoint.com/guide-vanilla-ajax-without-jquery/*/
			let api = 'https://www.googleapis.com/webfonts/v1/webfonts?key=' + GoogleFontsApiKey;

			if (GoogleFontsApiKey) {
				setter([{
					'family': 'Aclonica'
				}, {
					'family': 'Mallanna'
				}]);
				return;
			}

			let xhr = new XMLHttpRequest();
			xhr.open('GET', api);
			xhr.send(null);

			console.log('Calling google fonts api');

			xhr.onreadystatechange = function () {
				let DONE = 4; // readyState 4 means the request is done.
				let OK = 200; // status 200 is a successful return.
				if (xhr.readyState === DONE) {
					if (xhr.status === OK)
						setter(JSON.parse(xhr.responseText).items); // 'This is the returned text.'
					else
						console.log('Error: ' + xhr.status); // An error occurred during the request.
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
			return fonts.filter(function (currentItem) {
				return searchOperation(currentItem, searchValue);
			});
		}

		function createCssLinkTag(encoded) {

			if (document.getElementById(`font-${encoded}`))
				return;

			let l = document.createElement('link');
			l.type = 'text/css';
			l.rel = 'stylesheet';
			l.id = `font-${encoded}`;
			l.href = `https://fonts.googleapis.com/css?family=${encoded}`;

			document.getElementsByTagName('head')[0].appendChild(l);

		}

		function createFontOption(fontFamily) {

			let option = document.createElement("option");
			option.style.fontFamily = fontFamily;
			option.text = fontFamily;
			return option;

		}

		function updateFontList(fonts) {

			let control = document.getElementById('fontNames');
			control.innerHTML = '';

			for (var current of fonts) {
				let encoded = encodeURI(current);

				createCssLinkTag(encoded);

				control.add(createFontOption(current));
			}

		}

		document.getElementById('fontname').onkeyup = function (d) {

			//console.log(d.keyCode);
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

		};

	})();

	function addColor() {
		addColor(null);
	}

	function addColor(key) {
		var spans = document.getElementsByName("spanBGColor");
		if (spans) {
			var addButton = spans[spans.length - 1].nextSibling;
			var newNode = spans[spans.length - 1].cloneNode(true);
			newNode.children[0].id = key ? `bgcolor${key}` : `bgcolor${spans.length + 1}`;
			newNode.children[1].id = key ? `bgblend${key}` : `bgblend${spans.length + 1}`;
			addButton.parentNode.insertBefore(newNode, addButton);
			newNode.getElementsByTagName("button")[0].onclick = removeColor;
		}
	}

	function removeColor() {

		if (!this.parentNode) return;

		this.parentNode.remove();
		renumberColors();

	}

	function renumberColors() {
		var spans = document.getElementsByName("spanBGColor");
		for (var i = 0; i < spans.length; i++) {
			var el = spans[i].firstChild;
			spans[i].children[0].id = `bgcolor${i + 1}`;
			spans[i].children[1].id = `bgblend${i + 1}`;
		}
	}