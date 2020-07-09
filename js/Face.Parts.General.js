const round = n => Math.round(n);
const plural = (num, noun) => {
	if (num == 1) return `1 ${noun}`;
	return `${num} ${noun}s`;
};
face.register("Face.Parts.TimeAgo", el => {
	const startTime = el.data.timestamp;
	delete el.data.timestamp;
	const get = () => {
		const currentTime = Math.floor((new Date()).getTime() / 1000);
		const seconds = currentTime - startTime;
		if (seconds < 120) return plural(seconds, "second");
		const minutes = round(seconds / 60);
		if (minutes < 120) return plural(minutes, "minute");
		const hours = round(minutes / 60);
		if (hours < 48) return plural(hours, "hour");
		const days = round(hours / 24);
		if (days < 14) return plural(days, "day");
		const weeks = round(days / 7);
		if (weeks < 52) return plural(weeks, "week");
		const years = round(weeks / 52);
		return plural(years, "year");
	}
	const update = () => {
		el.content = get();
	};
	update();
	setInterval(update, 1000);
});

const delegateIds = {};
let delegatePingInterval = null;

face.register("Face.Parts.DelegateButton", el => {
	const delegateId = el.data.delegateId;
	delete el.data.delegateId;
	delegateIds[delegateId] = el;
	if (delegatePingInterval === null) {
		delegatePingInterval = setInterval(() => {
			fetch('.Face/delegate/ping', {
				method: "POST",
				body: Object.keys(delegateIds).join('/')
			});
		}, 60000);
	}

	const once = el.data.once;
	if (once) delete el.data.once;

	let expireTime = el.data.expires;
	let expireInterval = null;
	if (expireTime) {
		delete el.data.expires;
		expireInterval = setInterval(() => {
			if ((new Date()).getTime() / 1000 > expireTime) {
				clearInterval(expireInterval);
				expireInterval = null;
				el.classes.add("Face_DelegateButton_expired");
				el.attribs.title = "Action expired";
				// todo client-side indicator of expiry (pass expire time as data attr)
				el.enabled = false;
			}
		}, 1000); // checking every second to account for browsers' potential interval restrictions eg in background tabs
	}
	el.on("click", () => {
		fetch(".Face/delegate/" + delegateId, {
			method: "POST",
			// todo include field values if the button exists in a <form>
		}).then(r => r.text()).then(s => {
			if (once) {
				el.enabled = false;
				if (expireInterval !== null) clearInterval(expireInterval);
				expireInterval = null;
			}
			// todo: work out means to have server determine what happens here; hmm
			// likely a similar 'rpc' setup to C_B's and a configurable script pool reference
			// ^ could include automatic dependency loading similar to element scripts
		}).catch(e => {

		});
	});
});


