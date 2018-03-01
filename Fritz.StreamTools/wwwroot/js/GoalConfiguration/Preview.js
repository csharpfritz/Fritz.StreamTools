function InitPreview() {
	const quickPreviewButton = "preview";
	const quickPreviewTextBoxes = ["preview", ConfigurationModel.Caption,
		ConfigurationModel.Goal,
		ConfigurationModel.CurrentValue,
		ConfigurationModel.EmptyBackgroundColor,
		ConfigurationModel.EmptyFontColor,
		"bgcolor1", "bgblend1"];

	for (var tb of quickPreviewTextBoxes) {
		document.getElementById(tb).onchange = loadPreview;
	}

	document.getElementById(quickPreviewButton).onclick = loadPreview;


}


function loadPreview() {

	if (isLoadingFromStorage) return;

	const iframeWidth = document.getElementById("widgetPreview").clientWidth - 40;
	const fontName = document.getElementById(ConfigurationModel.FontName).value;

	if (supportedFonts.length > 0 && filterFontList(supportedFonts, fontName, filterExactMatchOperation).length !== 1) {
		alert('Font not supported by Google Fonts');
		return;
	}

	var urlTemplate = "/followers/goal/";
	urlTemplate += `${document.getElementById(ConfigurationModel.Goal).value}/`;
	urlTemplate += `${document.getElementById(ConfigurationModel.Caption).value}`;
	urlTemplate += `?width=${iframeWidth}`;
	urlTemplate += getBgColors();
	urlTemplate += getBgBlend();
	urlTemplate += `&EmptyBackgroundColor=${escape(document.getElementById(ConfigurationModel.EmptyBackgroundColor).value)}`;
	urlTemplate += `&emptyFontColor=${escape(document.getElementById(ConfigurationModel.EmptyFontColor).value)}`;
	urlTemplate += `&fontName=${escape(fontName)}`;

	document.getElementById("widgetPreview").src = urlTemplate + `&currentValue=${document.getElementById(ConfigurationModel.CurrentValue).value}`
	log(urlTemplate);

	document.getElementById("outputUrl").textContent = urlTemplate;
	document.getElementById("outputUrl").href = urlTemplate;
	saveValues();

}

// build the bgcolors parameter
function getBgColors() {
	const spans = document.getElementsByName("spanBGColor");
	var result = "";
	if (spans) {
		for (let i = 0; i < spans.length; i++) {
			result += `,${escape(spans[i].getElementsByTagName('input')[0].value.trim())}`;
		}
		result = `&${ConfigurationModel.FillBackgroundColor}=${result.substr(1)}`;
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
		result = `&${ConfigurationModel.FillBackgroundColorBlend}=${result.substr(1)}`;
	}
	return result;
}
