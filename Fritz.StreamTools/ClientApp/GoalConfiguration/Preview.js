
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
