const dom = require("./index").dom;
const Vis = require("../vis");

module.exports = function vis(spec, defineProperties){

	const layout = dom.canvas({
		classes: spec.classes,
		style: spec.style,
		events: spec.events,
		attribs: {
			width: spec.width,
			height: spec.height
		},
	});

	const vis = Vis.create(spec.width, spec.height, layout.domElement, spec.debug);

	defineProperties({
		scene: {
			get: () => vis,
		},
		style: { value: layout.style },
		classes: { value: layout.classes }
	});
	defineProperties.values({
		defs: Vis.defs
	});

	return layout;
};