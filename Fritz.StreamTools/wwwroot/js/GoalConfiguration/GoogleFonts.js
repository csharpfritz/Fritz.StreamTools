(function (window) {
  'use strict';

  window.fontsModule = {
		initModule: initModulePublic,
		getSupportedFonts: getSupportedFontsPublic,
		setSupportedFonts: setSupportedFontsPublic
  };

  let googleFontsApiKey = '';
  let supportedFonts = [];

  const defaultFonts = [ 'Arial', 'Helvetica', 'Times New Roman', 'Times', 'Courier New' ];

  function initModulePublic(moduleConfig) {
		if (!moduleConfig.googleFontsApiKey) {
			log('No Google Fonts Api key provided');
		} else {
			googleFontsApiKey = moduleConfig.googleFontsApiKey;
		}

		const cachedFonts = JSON.parse(localStorage.getItem('supportedFonts'));

		if (cachedFonts && Array.isArray(cachedFonts)) {
			log(`Setting [${cachedFonts.length}] supported fonts from local storage`);

			supportedFonts = cachedFonts;
		  updateFontList(filterFontList(supportedFonts));
		} else if (googleFontsApiKey) {
			requestGoogleFonts();

		} else {
			useDefaultFonts();
		}
  }

  function getSupportedFontsPublic() {
		return supportedFonts;
  }

  function setSupportedFontsPublic(fonts) {
		supportedFonts = fonts;

		localStorage.setItem('supportedFonts', JSON.stringify(supportedFonts));
		updateFontList(filterFontList(supportedFonts));
  }
	
  function requestGoogleFonts() {
		log('Requesting fonts list from google fonts api');

	  const requestUrl = `https://www.googleapis.com/webfonts/v1/webfonts?key=${googleFontsApiKey}`;

	  const request = new Request(requestUrl);

	  fetch(request)
		.then(response => response.json())
		.then(responseData => responseData.items.map(v => v.family))
		.then(fonts => setSupportedFontsPublic(fonts))
		.catch(error => {
		  log('Error requesting google fonts', error);

		  useDefaultFonts();
		});
  }

  function useDefaultFonts() {
		log('Falling back to default fonts', defaultFonts);

		setSupportedFontsPublic(defaultFonts);
  }
}(window));
