export default function(store, arrayName) {
	return {
		get length() { return store.getters[`${arrayName}Length`] },
		findIndex(callback) { return store.getters[`${arrayName}FindIndex`](callback) },
		splice(start, deleteCount, item) {
			return store.commit(`${arrayName}Splice`, { start, deleteCount, item });
		},
	};
}
