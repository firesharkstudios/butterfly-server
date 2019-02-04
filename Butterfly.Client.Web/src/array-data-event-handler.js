export default function (config) {
  let _private = this;

  let keyFieldNamesByName = {};
  let batchSize = config.batchSize || 250;

  let queue = [];
  let queueCurrentOffset = 0;
  let handleQueueTimeout = null;

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

  _private.handleDataEvent = function (dataEvent) {
    //console.debug('ArrayDataEventHandler.handle():dataEvent.type=' + dataEvent.dataEventType + ',name=', dataEvent.name + ',keyValue=' + dataEvent.keyValue);
    if (dataEvent.dataEventType === 'InitialEnd') {
      if (config.onInitialEnd) config.onInitialEnd();
    }
    else {
      let array = config.arrayMapping[dataEvent.name];
      if (!array) {
        console.error('No mapping for data event \'' + dataEvent.name + '\'');
      }
      else if (dataEvent.dataEventType === 'InitialBegin') {
        array.splice(0, array.length);
        keyFieldNamesByName[dataEvent.name] = dataEvent.keyFieldNames;
      }
      else if (dataEvent.dataEventType === 'Insert' || dataEvent.dataEventType === 'Initial') {
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
      else if (dataEvent.dataEventType === 'Update') {
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
      else if (dataEvent.dataEventType === 'Delete') {
        let keyValue = _private.getKeyValue(dataEvent.name, dataEvent.record);
        let index = array.findIndex(x => x._keyValue == keyValue);
        array.splice(index, 1);
      }
    }
  };

  _private.handleQueue = function () {
    if (handleQueueTimeout) clearTimeout(handleQueueTimeout);

    if (queue.length > 0) {
      let begin = queueCurrentOffset;
      let end = Math.min(begin + batchSize, queue[0].length);
      for (let i = begin; i < end; i++) {
        _private.handleDataEvent(queue[0][i]);
      }
      if (end === queue[0].length) {
        queue.splice(0, 1);
        queueCurrentOffset = 0;
      }
      else {
        queueCurrentOffset += batchSize;
        handleQueueTimeout = setTimeout(_private.handleQueue, 0);
      }
    }
  }

  return function (messageType, data) {
    if (messageType === 'RESET') {
      for (let arrayKey in config.arrayMapping) {
        let array = config.arrayMapping[arrayKey];
        if (array) array.splice(0, array.length);
      }
    }
    else if (messageType === 'DATA-EVENT-TRANSACTION') {
      queue.push(data.dataEvents);
      _private.handleQueue();
    }
    else if (config.onChannelMessage) {
      config.onChannelMessage(messageType, data);
    }
  }
}
