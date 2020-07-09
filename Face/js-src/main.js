const libJel = require("./jel");

const scripts = {};

let pendingRequires = [];

const libs = {
	jel: libJel
};

const requireScript = (...scriptRefs) => {
	scriptRefs.forEach(scriptRef => {
		if (Array.isArray(scriptRef)) {
			scriptRef.forEach(requireScript);
			return;
		}
		const promise = fetch(scriptRef + ".js").then(r => {
			if (r.headers.get("Content-Type") != "text/javascript") {
				console.warn("Incorrect content type from " + screenLeft + ".js");
				return "";
			}
			return r.text();
		}).then(s => {
			let fn;
			try {
				fn = new Function("module", s);
			} catch (ex) {
				console.error(`Failed to parse ${scriptRef}`, ex);
				return;
			}

			// let the script chain this promise with module.init = promise
			const module = {};
			fn(module);
			return module.init;
		});
		pendingRequires.push(promise);
	});
};

const api = {
	libs,
	require: requireScript,
	register: (nameOrMap, fn) => {
		if (fn === undefined) {
			return void Object.keys(nameOrMap).forEach(k => api.register(k, nameOrMap[k]));			
		}
		if (!(fn instanceof Function)) throw new Error("Argument is not a function");
		if (!nameOrMap) throw new Error("Empty script name");
		if (scripts[nameOrMap] !== undefined) {
			if (fn === scripts[name]) return; // allow (ignore) repeat registration, at least of the same func instance
			throw new Error(`Conflicting script name '${nameOrMap}'`); // but error on possible conflict
		}
		scripts[nameOrMap] = fn;
	},
	await: promise => pendingRequires.push(promise),
	apply: el => Promise.all(pendingRequires).then(() => {
		// waiting until all requires are ready
		pendingRequires = []; // the resolved promises no longer need to be tracked
		if (!(el instanceof libJel.Jel)) el = libJel.wrapHtmlElement(el);
		el.qsa("[data-face-script").forEach(el => {
			const scriptRef = el.data['face-script'];
			const script = scripts[scriptRef];
			delete el.data['face-script'];
			if (!script) {
				console.warn(`Missing script '${scriptRef}'; you may need to enable a client feature.`);
			} else {
				script(el);
			}
		});
	})
};


Object.defineProperty(window, "face", { value: api });

setTimeout(() => api.apply(document.body), 0);