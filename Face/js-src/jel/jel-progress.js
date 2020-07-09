const dom = require("./index").dom;

module.exports = function progress(spec, defineProperties){
	let position = spec.position || 0;
	let classes = [];
	if(spec.classes){
		classes = Array.isArray(spec.classes) ? spec.classes : spec.classes.split(" ");
	}

	const layout = dom.div({
		classes: ["jel-progress", ...classes],
		content: [
			dom.div({
				classes: "jel-progress-inner",
				id: "inner",
				style: {
					width: position * 100 + "%"
				}
			}),
			...(spec.content || []),
		]

	});

	defineProperties({
		position: {
			get: () => position,
			set: v => {
				position = v;
				layout.$inner.style.width = position * 100 + "%"
			}
		},
		classes: { value: layout.classes },
	});

	return layout;
};

