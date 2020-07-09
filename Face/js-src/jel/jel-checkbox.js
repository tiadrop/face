const dom = require("./index").dom;

module.exports = function progress(spec, defineProperties, triggerEvent){
	let classes = [];
	if(spec.classes){
		classes = Array.isArray(spec.classes) ? spec.classes : spec.classes.split(" ");
	}

	const layout = dom.label({
		classes: ["jel-checkbox", ...classes],
		content: [
			dom.input({
				attribs: {
					type: "checkbox",
				},
				id: "input",
				events: {
					change: ev => triggerEvent("change", layout.$input.checked)
				}
			}),
			dom.span({
				id: "caption",
				content: spec.caption
			})
		]
	});

	if(spec.checked) layout.$input.attribs.checked = true;

	defineProperties({
		caption: {
			get: () => dom.span.content,
			set: v => layout.$caption.content = v
		},
		checked: {
			get: () => layout.$input.checked,
			set: v => layout.$input.checked = v
		}
	});

	return layout;
};