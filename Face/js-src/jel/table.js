const $ = require("./index").dom;

module.exports = (spec, defineProperties, triggerEvent) => {
	const layout = $.table({
		classes: spec.classes,
		content: $.tbody({
			id: "body"
		})
	});
	const tbody = layout.$body;
	if(spec.headers){
		tbody.append($.tr({
			content: spec.headers.map(s => $.th({
				content: s
			}))
		}));
		tbody.append(spec.data.map(dataRow => $.tr({
			content: dataRow.map(dataItem => $.td({
				content: dataItem
			}))
		})));
	}

	defineProperties({
		body: { value: body }
	});

	return layout;
};