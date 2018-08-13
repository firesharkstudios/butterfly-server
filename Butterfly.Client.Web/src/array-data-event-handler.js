export default function(config) {
	let _private = this;

	let keyFieldNamesByName = {};

	_private.getKeyValue = function (name, record) {
		let result = '';
		let keyFieldNames = keyFieldNamesByName[name];
		for (let i = 0; i < keyFieldNames.length; i++) {
			let value = record[keyFieldNames[i]];
			if (!result && result.length > 0) result += ';';
			result += '' + value;
		}
		return result;
	}

	return function (messageType, data) {
		if (messageType == 'RESET') {
			for (let arrayKey in config.arrayMapping) {
				let array = config.arrayMapping[arrayKey];
				if (array) array.splice(0, array.length);
			}
		}
		else if (messageType == 'DATA-EVENT-TRANSACTION') {
			let dataEventTransaction = data;
			for (let i = 0; i < dataEventTransaction.dataEvents.length; i++) {
				let dataEvent = dataEventTransaction.dataEvents[i];
				//console.debug('ArrayDataEventHandler.handle():dataEvent.type=' + dataEvent.dataEventType + ',name=', dataEvent.name + ',keyValue=' + dataEvent.keyValue);
				if (dataEvent.dataEventType == 'InitialEnd') {
					if (config.onInitialEnd) config.onInitialEnd();
				}
				else {
					let array = config.arrayMapping[dataEvent.name];
					if (!array) {
						console.error('No mapping for data event \'' + dataEvent.name + '\'');
					}
					else if (dataEvent.dataEventType == 'InitialBegin') {
						array.splice(0, array.length);
						keyFieldNamesByName[dataEvent.name] = dataEvent.keyFieldNames;
					}
					else if (dataEvent.dataEventType == 'Insert' || dataEvent.dataEventType == 'Initial') {
						let keyValue = _private.getKeyValue(dataEvent.name, dataEvent.record);
						let index = array.findIndex(x => x._keyValue == keyValue);
						if (index >= 0) {
							console.error('Duplicate key \'' + keyValue + '\' in table \'' + dataEvent.name + '\'');
						}
						else {
							dataEvent.record['_keyValue'] = keyValue;
							array.splice(array.length, 0, dataEvent.record);
						}
					}
					else if (dataEvent.dataEventType == 'Update') {
						let keyValue = _private.getKeyValue(dataEvent.name, dataEvent.record);
						let index = array.findIndex(x => x._keyValue == keyValue);
						if (index == -1) {
							console.error('Could not find key \'' + keyValue + '\' in table \'' + dataEvent.name + '\'');
						}
						else {
							dataEvent.record['_keyValue'] = keyValue;
							array.splice(index, 1, dataEvent.record);
						}
					}
					else if (dataEvent.dataEventType == 'Delete') {
						let keyValue = _private.getKeyValue(dataEvent.name, dataEvent.record);
						let index = array.findIndex(x => x._keyValue == keyValue);
						array.splice(index, 1);
					}
				}
			}
		}
		else if (config.onChannelMessage) {
			config.onChannelMessage(messageType, data);
		}
	}
}
