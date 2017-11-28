function ArrayDataEventHandler(config) {

    let keyFieldNamesByName = {};

    this.handle = function (dataEvent) {
        console.log('ArrayDataEventHandler.handle():dataEvent.type=' + dataEvent.dataEventType + ',name=', dataEvent.name + ',keyValue=' + dataEvent.keyValue);
        let array = config.arrayMapping[dataEvent.name];
        if (dataEvent.dataEventType == 'InitialBegin') {
            array.splice(0, array.length);
            keyFieldNamesByName[dataEvent.name] = dataEvent.keyFieldNames;
        }
        else if (dataEvent.dataEventType == 'Insert' || dataEvent.dataEventType == 'Initial') {
            let keyValue = this.getKeyValue(dataEvent.name, dataEvent.record);
            dataEvent.record['_keyValue'] = keyValue;
            array.push(dataEvent.record);
        }
        else if (dataEvent.dataEventType == 'Update') {
            let keyValue = this.getKeyValue(dataEvent.name, dataEvent.record);
            let index = this.findIndex(array, keyValue);
            array.splice(index, 1, dataEvent.record);
        }
        else if (dataEvent.dataEventType == 'Delete') {
            let keyValue = this.getKeyValue(dataEvent.name, dataEvent.record);
            let index = this.findIndex(array, keyValue);
            array.splice(index, 1);
        }
        else if (dataEvent.dataEventType == 'InitialEnd') {
            if (config.loaded) config.loaded();
        }
    }

    this.findIndex = function (array, keyValue) {
        return array.findIndex(x => x._keyValue == keyValue);
    }

    this.getKeyValue = function (name, record) {
        let result = '';
        let keyFieldNames = keyFieldNamesByName[name];
        for (let i = 0; i < keyFieldNames.length; i++) {
            let value = record[keyFieldNames[i]];
            if (!result && result.length>0) result += ';';
            result += '' + value;
        }
        return result;
    }
}