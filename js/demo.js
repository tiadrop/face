face.register("demo.EventLog", el => {
	const $ = face.libs.jel.dom;
	setInterval(() => {
		fetch("_eventLog").then(r => r.text()).then(text => {
			if (text) {
				el.append($(text));
				face.apply(el);
			}
			while (el.content.length > 10) el.remove(el.content[0]);
		});
	}, 4000);
});