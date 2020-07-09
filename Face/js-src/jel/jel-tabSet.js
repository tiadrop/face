// eg j.tabset({ tabs: [{ caption: "tab1", content: dom.div({...}) }] })

const dom = require("./index").dom;

module.exports = function tabSet(spec, defineProperties, triggerEvent){
	const layout = dom.div({
		classes: ["jel-tabset", ...spec.classes],
		content: [
			dom.div({
				id: "tabs",
				classes: "jel-tabset-tabs",
			}),
			dom.div({
				id: "content",
				classes: "jel-tabset-content",
			})
		]
	});

	const addTab = spec => {
		const client = dom.div({
			content: spec.content,
			classes: "jel-tabset-client",
			style: {
				display: "none"
			}
		});
		const tab = dom.button({
			classes: "jel-tabset-tab",
			content: spec.caption,
			events: {
				click: ev => {
					triggerEvent("change", {
						tab,
						client,
						data: spec.data,
						tabIndex: layout.$tabs.content.indexOf(tab)
					}),
					layout.$content.content.forEach(ent => {
						ent.style.display = ent === client ? "block" : "none";
					});
					layout.$tabs.content.forEach(ent => {
						ent.classes.toggle("active", ent === tab);
					});
				}
			}
		})
		layout.$tabs.append(tab);
		layout.$content.append(client);
	};

	if(spec.tabs) spec.tabs.forEach(addTab);
	layout.$tabs.content[spec.activeIndex || 0].click();

	defineProperties({
		addTab: { value: addTab }
	});

	return layout;
};