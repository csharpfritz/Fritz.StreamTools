var supportedFonts = [];

function InitGoogleFonts() {

	if (GoogleFontsApiKey !== '' && supportedFonts.length === 0) {

		googleFontsAdapter(setSupportedFontsFromApi);
	} else if (GoogleFontsApiKey === '') {

		log('Setting base fonts');
		setSupportedFontsFromApi(baseFontsAdapter());

	}

}

function baseFontsAdapter() {

	return [
			{ 'family': 'Arial' },
			{ 'family': 'Helvetica' },
			{ 'family': 'Times New Roman' },
			{ 'family': 'Times' },
			{ 'family': 'Courier New' }
	];

}

function googleFontsAdapter(setter) {
	//Reference for the HTTP in vanilla JS https://www.sitepoint.com/guide-vanilla-ajax-without-jquery
	const api = `https://www.googleapis.com/webfonts/v1/webfonts?key=${GoogleFontsApiKey}`;

	if (!GoogleFontsApiKey) {
		setter([{
			'family': 'Abel'
		}, {
			'family': 'Abril Fatface'
		}, {
			'family': 'Acme'
		}, {
			'family': 'Alegreya'
		}, {
			'family': 'Alex Brush'
		}, {
			'family': 'Amaranth'
		}, {
			'family': 'Amatic SC'
		}, {
			'family': 'Anton'
		}, {
			'family': 'Arbutus Slab'
		}, {
			'family': 'Architects Daughter'
		}, {
			'family': 'Archivo'
		}, {
			'family': 'Archivo Black'
		}, {
			'family': 'Arima Madurai'
		}, {
			'family': 'Asap'
		}, {
			'family': 'Bad Script'
		}, {
			'family': 'Baloo Bhaina'
		}, {
			'family': 'Bangers'
		}, {
			'family': 'Berkshire Swash'
		}, {
			'family': 'Bitter'
		}, {
			'family': 'Boogaloo'
		}, {
			'family': 'Bree Serif'
		}, {
			'family': 'Bungee Shade'
		}, {
			'family': 'Cantata One'
		}, {
			'family': 'Catamaran'
		}, {
			'family': 'Caveat'
		}, {
			'family': 'Caveat Brush'
		}, {
			'family': 'Ceviche One'
		}, {
			'family': 'Chewy'
		}, {
			'family': 'Contrail One'
		}, {
			'family': 'Crete Round'
		}, {
			'family': 'Dancing Script'
		}, {
			'family': 'Exo 2'
		}, {
			'family': 'Fascinate'
		}, {
			'family': 'Francois One'
		}, {
			'family': 'Freckle Face'
		}, {
			'family': 'Fredoka One'
		}, {
			'family': 'Gloria Hallelujah'
		}, {
			'family': 'Gochi Hand'
		}, {
			'family': 'Great Vibes'
		}, {
			'family': 'Handlee'
		}, {
			'family': 'Inconsolata'
		}, {
			'family': 'Indie Flower'
		}, {
			'family': 'Kaushan Script'
		}, {
			'family': 'Lalezar'
		}, {
			'family': 'Lato'
		}, {
			'family': 'Libre Baskerville'
		}, {
			'family': 'Life Savers'
		}, {
			'family': 'Lobster'
		}, {
			'family': 'Lora'
		}, {
			'family': 'Luckiest Guy'
		}, {
			'family': 'Marcellus SC'
		}, {
			'family': 'Merriweather'
		}, {
			'family': 'Merriweather Sans'
		}, {
			'family': 'Monoton'
		}, {
			'family': 'Montserrat'
		}, {
			'family': 'News Cycle'
		}, {
			'family': 'Nothing You Could Do'
		}, {
			'family': 'Noto Serif'
		}, {
			'family': 'Oleo Script Swash Caps'
		}, {
			'family': 'Open Sans'
		}, {
			'family': 'Open Sans Condensed'
		}, {
			'family': 'Oranienbaum'
		}, {
			'family': 'Oswald'
		}, {
			'family': 'PT Sans'
		}, {
			'family': 'PT Sans Narrow'
		}, {
			'family': 'PT Serif'
		}, {
			'family': 'Pacifico'
		}, {
			'family': 'Patrick Hand'
		}, {
			'family': 'Peralta'
		}, {
			'family': 'Permanent Marker'
		}, {
			'family': 'Philosopher'
		}, {
			'family': 'Play'
		}, {
			'family': 'Playfair Display'
		}, {
			'family': 'Playfair Display SC'
		}, {
			'family': 'Poiret One'
		}, {
			'family': 'Press Start 2P'
		}, {
			'family': 'Prosto One'
		}, {
			'family': 'Quattrocento'
		}, {
			'family': 'Questrial'
		}, {
			'family': 'Quicksand'
		}, {
			'family': 'Raleway'
		}, {
			'family': 'Rancho'
		}, {
			'family': 'Righteous'
		}, {
			'family': 'Roboto'
		}, {
			'family': 'Roboto Condensed'
		}, {
			'family': 'Roboto Slab'
		}, {
			'family': 'Rubik'
		}, {
			'family': 'Rye'
		}, {
			'family': 'Satisfy'
		}, {
			'family': 'Shadows Into Light'
		}, {
			'family': 'Shojumaru'
		}, {
			'family': 'Sigmar One'
		}, {
			'family': 'Skranji'
		}, {
			'family': 'Slabo 27px'
		}, {
			'family': 'Special Elite'
		}, {
			'family': 'Tinos'
		}, {
			'family': 'Ultra'
		}, {
			'family': 'UnifrakturMaguntia'
		}, {
			'family': 'VT323'
		}, {
			'family': 'Yanone Kaffeesatz'
		}]);
		return;
	}

  const request = new Request(api);

  fetch(request)
	.then(response => response.json())
	.then(responseData => setter(responseData.items))
	.catch(error => log(`Error: ${error}`));
}

function setSupportedFonts(fonts) {
	localStorage.setItem('supportedFonts', JSON.stringify(supportedFonts));
	updateFontList(filterFontList(supportedFonts));
}

function setSupportedFontsFromApi(fonts) {
	supportedFonts = fonts.map((v) => v.family);
	setSupportedFonts(supportedFonts);
}

