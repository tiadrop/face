/*

Examples:

const jel = require("jel");
const $ = J.dom;

// wrap an existing element
const body = $(document.body);

// create and manipulate an element
const myHeader = $.h1({
	content: "greetings planet",
	// content: [$.span({ ... }), $.img({ ... })],
	// html: "string content is wrapped in a Text() thus html-safe; html is <b>not</b>",
	classes: "space separated classes",
	// classes: ["or", "array thereof"],
	attribs: {
		title: "howdy globe"
	},
	data: {}, // maps to attribs["data-*"],
	events: {
		click: ev => alert("hi earth")
	}
});
body.append(myHeader);

myHeader.on("mousemove", ev => console.log("good day terra"));
myHeader.attribs.title = "alright world";
myHeader.classes.add("xyz");

// use components
const jel = require("jel").createParser({
	progress: require("jel-progress"),
	tabSet: require("jel-tabSet")
});
const $ = jel.dom;
body.append(jel.progress({
	position: .4,
	classes: "hpbar"
}));

// write components
const labeledEdit = (spec, define, trigger) => {
	const id = computeSomeRandomString();
	const layout = $.div({
		content: [
			$.label({
				content: spec.caption,
				attribs: { for: id },
				id: "label", // distinct from attribs.id; more later
				events: {
					// custom events -- usable via myLabeledEdit.on(...) and labeledEdit({ events: {...} })
					click: ev => trigger("labelClick", ev)
				}
			}),
			$.input({
				attribs: { id },
				id: "input",
				events: {
					change: ev => trigger("change", ev)
				}
			});
		]
	});

	define.importDom(layout); // exposes interfaces to manipulate standard jelement props such as style, parent, qsa
	define({
		caption: {
			// reference nested elements via id (not attribs.id)
			get: () => layout.$label.text,
			set: v => layout.$label.text = v
		}
	});
}

const jel = require("jel").createParser({ labeledEdit })

body.append(jel.labeledEdit({
	caption: "Favourite pasty"
}));


*/

const elementWrapperSym = Symbol();
const Jel = function Jel(type, spec = {}){
	spec = {...spec};
	if(spec.classes){
		spec.classes = Array.isArray(spec.classes) ? spec.classes : spec.classes.split(/\s+/);
	} else spec.classes = [];

	if(type === undefined) throw new Error("Entity type is undefined");
	const eventHandlers = {};
	const triggerEvent = (name, data) => {
		if(eventHandlers[name]) eventHandlers[name].forEach(h => h(data));
	};

	// type() (the initialiser) will return this component's root HTMLElement (or 'jelement' that represents it)
	const define = props => Object.defineProperties(this, props);
	define.values = vals => {
		Object.keys(vals).forEach(k => Object.defineProperty(this, k, { value: vals[k] }));
	};
	define.readOnly = vals => {
		Object.keys(vals).forEach(k => Object.defineProperty(this, k, { get: vals[k] }));
	};
	define.writeOnly = vals => {
		Object.keys(vals).forEach(k => Object.defineProperty(this, k, { set: vals[k] }));
	};
	define.importDom = (ent) => {
		["absoluteTop", "absoluteLeft", "style", "attribs", "data", "parent", "qsa"].forEach(k => Object.defineProperty(this, k, {
			get: () => {
				return ent[k];
			}
		}))
	}

	// get el/ent from type
	const domRoot = type(spec, define, triggerEvent);

	this.entityType = type.name;

	let domElement;
	if(domRoot instanceof Jel){
		domElement = domRoot.domElement;
	} else if(domRoot instanceof HTMLElement){
		domElement = domRoot;
		domRoot[elementWrapperSym] = this;
	} else throw new Error("Invalid return type from entity definition");

	if(type !== initHtmlNode){
		const rootJel = (domRoot instanceof Jel) ? domRoot : wrapHtmlNode(domRoot);
		rootJel.entity = this;
	}

	if(!this.on) this.on = (eventName, handler) => {
		if(eventHandlers[eventName] === undefined) eventHandlers[eventName] = [];
		eventHandlers[eventName].push(handler);
	};
	if(!this.off) this.off = (eventName, handler) => {
		if(eventHandlers[eventName] === undefined) return;
		const idx = eventHandlers[eventName].indexOf(handler);
		if (idx !== -1) eventHandlers[eventName].splice(idx);
	};


	if(spec.events){
		Object.keys(spec.events).forEach(k => {
			if(spec.events[k]) this.on(k, spec.events[k]);
		})
	}

	const id = spec.id;
	Object.defineProperties(this, {
		id: { value: id },
		domElement: { value: domElement, enumerable: true, },
		hasAncestor: { value: elOrEnt => {
			let el = elOrEnt instanceof HTMLElement ? el : elOrEnt.domElement;
			let p = this.parent;
			while(p){
				if(p.domElement === el) return true;
				p = p.parent;
			}
			return false;
		}}
	});
};

const wrapHtmlNode = node => {
	if(typeof node == "string") node = document.querySelector(node);
	if(!(node instanceof HTMLElement)) throw new Error("Expecting HTMLElement");
	if(!node[elementWrapperSym]){
		// wrap(someNode) == wrap(someNode) should be true
		node[elementWrapperSym] = new Jel(initHtmlNode, {
			id: node.id,
			wrapping: node,
		});
	}
	return node[elementWrapperSym];
};

const parseHtml = html => {
	const result = [];
	const tempElement = document.createElement("div");
	tempElement.innerHTML = html;
	while(tempElement.childNodes.length){
		let node = tempElement.childNodes[0];
		tempElement.removeChild(node);
		if(!(node instanceof Text)) node = wrapHtmlNode(node);
		result.push(node);
	}
	return result;
};

const attributesAccessorHandler = {
	get: (o, k) => o.getAttribute(k),
	set: (o, k, v) => {
		o.setAttribute(k, v);
		return true;
	},
	has: (o, k) => o.hasAttribute(k),
	deleteProperty: (o, k) => {
		o.removeAttribute(k);
		return true;
	},
	ownKeys: (o) => Array.from(o.attributes).map(a => a.name),
	enumerable: true,	
};
const dataAttributesAccessorHandler = {
	get: (o, k) => {
		const v = o.getAttribute("data-" + k);
		return v === null ? undefined : v;
	},
	set: (o, k, v) => {
		o.setAttribute("data-" + k, v);
		return true;
	},
	has: (o, k) => o.hasAttribute("data-" + k),
	deleteProperty: (o, k) => o.removeAttribute("data-" + k),
	ownKeys: (o) => Reflect.ownKeys(o.dataset),
	enumerable: true,	
};

const combineSubStyles = source => {
	return Object.keys(source).map(k => `${k}(${source[k]})`).join(" ");
};

const styleAccessorHandler = {
	set: (o, k, v) => {
		let match;
		if(match = k.match(/^filter_(\w+)$/)){
			o.filters[match[1]] = v;
			o.element.style.filter = combineSubStyles(o.filters);
			return true;
		}
		if(match = k.match(/^transform_(\w+)$/)){
			o.transforms[match[1]] = v;
			o.element.style.transform = combineSubStyles(o.transforms);
			return true;
		}
		o.element.style[k] = v;
		return true;
	}, 
	get: (o, k) => {
		if(k === "animate" && Jel.twixt){
			return (duration, toStyles, ease) => {
				const properties = {};
				Object.keys(toStyles).forEach(k => {
					let currentValue;
					if(match = k.match(/^filter_(\w+)$/)){
						currentValue = o.filters[match[1]];
					} else if(match = k.match(/^transform_(\w+)$/)){
						currentValue = o.transforms[match[1]];
					} else currentValue = o.element.style[k];	
					properties.push({ from: currentValue, to: toStyles[k], ease })
				});
				return Jel.twixt.animate(o.element.style, duration, properties);
			};
		}
		if(match = k.match(/^filter_(\w+)$/)){
			return o.filters[match[1]];
		}
		if(match = k.match(/^transform_(\w+)$/)){
			return o.transforms[match[1]];
		}
		return o.element.style[k];
	}
};

const wrapStyles = element => {
	const filters = {};
	const transforms = {};
	const wrapper = new Proxy({element, filters, transforms}, styleAccessorHandler);
	return wrapper;
};

// ent constructors define entity props and return the domElement they created
const initHtmlNode = (spec, defineProperties) => {
	spec = { ...spec };
	const element = spec.wrapping ? spec.wrapping : document.createElement(spec.tag);

	if(spec.wrapping){
		spec.content = Array.from(element.childNodes);
	} else { // spec.classes is now always an array (converted earlier)
		let classes = (spec.classes && spec.classes.filter(c => c)) || [];
		if(classes.length) {
			element.className = classes.join(" ");
		}
	}

	if(spec.html){
		if(spec.content) throw new Error("HTML entity spec can include content OR html");
		spec.content = parseHtml(spec.html);
		// todo: these jel wrappers are unreferenced; shouldn't we just set element.innerHTML instead?
		// ^^ probably not; elements with id ARE referenced @ jelement["$id"]
	}

	const attributesAccessor = new Proxy(element, attributesAccessorHandler);
	const dataAttributesAccessor = new Proxy(element, dataAttributesAccessorHandler);
	const styleAccessor = wrapStyles(element);

	if(spec.attribs) Object.keys(spec.attribs).forEach(k => element.setAttribute(k, spec.attribs[k]));
	if(spec.data) Object.keys(spec.data).forEach(k => element.setAttribute("data-" + k, spec.data[k]));

	const getAbsoluteLeft = () => {
		return element.offsetLeft + (element.offsetParent ? wrapHtmlNode(element.offsetParent).absoluteLeft : 0);
	};
	const getAbsoluteTop = () => {
		return element.offsetTop + (element.offsetParent ? wrapHtmlNode(element.offsetParent).absoluteTop : 0);
	};

	const propertyDefs = {
		enabled: {
			get: () => !element.disabled,
			set: v => element.disabled = !v
		}
	};

	// pass-through properties
	["value", "checked"].forEach(prop => {
		propertyDefs[prop] = {
			get: () => element[prop],
			set: v => element[prop] = v,
			enumerable: true
		}
	});
	// bound props
	["click", "getContext", "focus", "blur"].forEach(prop => {
		propertyDefs[prop] = {
			get: () => element[prop].bind(element)
		}
	});

	const addContent = content => {
		if(!content) return;
		let item;
		if(Array.isArray(content)){
			content.forEach(addContent);
			return;
		}
		if(content instanceof HTMLElement){
			item = wrapHtmlNode(content);
		} else if(content instanceof Text){
			item = content;
		} else if(["string", "number"].includes(typeof content)){
			item = new Text(content);
		} else if(content instanceof Jel){
			item = content;
		} else if(content.type) {
			item = new Jel(content.type, content);
		} else throw new Error("Invalid entity type");

		if(item instanceof Text){
			element.appendChild(item);
			return;
		}

		if(item.id){
			propertyDefs["$" + item.id] = {
				get: () => item, enumerable: true,
				configurable: true,
			};	
		}

		defineProperties(propertyDefs);
		element.appendChild(item.domElement);
	};

	const eventHandlerWrappers = new WeakMap();
	const wrapEventHandler = fn => {
		if(!eventHandlerWrappers.has(fn)){
			eventHandlerWrappers.set(fn, ev => {
				if(ev.clientX !== undefined && ev.innerX === undefined) ev.innerX = ev.clientX - getAbsoluteLeft();
				if(ev.clientY !== undefined && ev.innerY === undefined) ev.innerY = ev.clientY - getAbsoluteTop();
				fn(ev);
			});
		}
		return eventHandlerWrappers.get(fn);
	};

	defineProperties({
		classes: { value: element.classList, enumerable: true },
		attribs: { value: attributesAccessor, enumerable: true },
		data: { value: dataAttributesAccessor, enumerable: true },
		on: { value: (eventName, handler) => element.addEventListener(eventName, wrapEventHandler(handler)) },
		off: { value: (eventName, handler) => element.removeEventListener(eventName, wrapEventHandler(handler)) },
		absoluteLeft: { get: getAbsoluteLeft },
		absoluteTop: { get: getAbsoluteTop },
		qsa: { value: query => Array.from(element.querySelectorAll(query)).map(wrapHtmlNode) },
		parent: { get: () => element.parentElement && wrapHtmlNode(element.parentElement), },
		style: { 
			get: () => styleAccessor,
			set: (k, v) => { element.style[k] = v; },
			enumerable: true 
		},
		tag: { get: () => element.tagName.toLowerCase() },
		content: {
			get: () => [...element.childNodes].map(node => node instanceof Text ? node : wrapHtmlNode(node)),
			set: v => {
				element.innerHTML = "";
				addContent(v);
			}, enumerable: true
		},
		html: {
			get: () => element.innerHTML,
			set: v => {
				element.innerHTML = v;
			}, enumerable: true
		},
		text: {
			get: () => element.textContent,
			set: v => {
				element.innerHTML = "";
				addContent(v);
			},
			enumerable: true
		},
		append: { value: addContent, enumerable: true },
		remove: { value: entityToRemove => {
			const elementToRemove = entityToRemove instanceof Jel ? entityToRemove.domElement : entityToRemove;
			if(!(entityToRemove instanceof Jel)) throw new Error("Invalid type");
			element.removeChild(elementToRemove);
			//children.splice(children.indexOf(entityToRemove), 1);
			if(entityToRemove instanceof Jel && entityToRemove.id){
				delete entity["$" + entityToRemove.id];
			}
		}},
		...propertyDefs
	});


	if(spec.content){
		addContent(spec.content);
	}

	if(spec.style){
		Object.keys(spec.style).forEach(k => styleAccessor[k] = spec.style[k]);
	}

	spec = null;
	return element;
};

const appendToParent = (child, parent) => {
	if(parent instanceof Jel){
		parent.append(child);
	} else if(parent instanceof HTMLElement){
		parent.appendChild(child.domElement);
	} else throw new Error("Incompatible type");
};

const createParser = (entityTypes = {}) => {
	let customTypes = {};
	Object.keys(entityTypes).forEach(k => {
		customTypes[k] = (spec, appendTo) => {
			const entity = new Jel(entityTypes[k], spec);
			if(appendTo) appendToParent(entity, appendTo);
			return entity;
		}
	});

	const domFunc = source => {
		if(source instanceof HTMLElement) return wrapHtmlNode(source);
		if(typeof source == "string"){
			if(source[0] === "<"){
				return parseHtml(source)[0];
			} else {
				return wrapHtmlNode(document.querySelector(source));
			}
		} else {
			throw new Error("Invalid argument");
		}
	};

	return {
		dom: new Proxy(() => {}, {
			apply: (o, _, args) => domFunc(...args),
			get: (o, k) => {
				return (spec = {}) => {
					const entity = new Jel(initHtmlNode, {...spec, tag: k});
					return entity;
				}
			}
		}),
		createType: configurator => (spec = {}) => new Jel(configurator, spec),
		...customTypes
	};
};

module.exports = {
	createParser,
	wrapHtmlElement: wrapHtmlNode,
	Jel,
	dom: createParser().dom
};
