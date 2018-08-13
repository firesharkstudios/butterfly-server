export default function(arrayName) {
	let result = {};
	result[`${arrayName}Splice`] = (state, options) => {
		if (options.item) state[arrayName].splice(options.start, options.deleteCount, options.item);
		else state[arrayName].splice(options.start, options.deleteCount);
	};
	return result;
}
