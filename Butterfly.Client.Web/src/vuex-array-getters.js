export function VuexArrayGetters(arrayName) {
	let result = {};
	result[`${arrayName}Length`] = state => state[arrayName].length;
	result[`${arrayName}FindIndex`] = state => callback => state[arrayName].findIndex(callback);
	return result;
}
