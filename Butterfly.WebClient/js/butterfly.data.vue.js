/*
 * Copyright 2017 Fireshark Studios, LLC
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

function VueDataEventHandler(config) {

    let keyFieldNamesByName = {};

    this.handle = function (dataEvent) {
        console.log('VueDataEventHandler.handle():dataEvent.type=' + dataEvent.dataEventType + ',name=', dataEvent.name + ',keyValue=' + dataEvent.keyValue);
        let vueArray = config.vueArrayMapping[dataEvent.name];
        if (dataEvent.dataEventType == 'InitialBegin') {
            vueArray.splice(0, vueArray.length);
            keyFieldNamesByName[dataEvent.name] = dataEvent.keyFieldNames;
        }
        else if (dataEvent.dataEventType == 'Insert' || dataEvent.dataEventType == 'Initial') {
            let keyValue = this.getKeyValue(dataEvent.name, dataEvent.record);
            dataEvent.record['_keyValue'] = keyValue;
            vueArray.push(dataEvent.record);
        }
        else if (dataEvent.dataEventType == 'Update') {
            let keyValue = this.getKeyValue(dataEvent.name, dataEvent.record);
            let index = this.findIndex(vueArray, keyValue);
            vueArray.splice(index, 1, dataEvent.record);
        }
        else if (dataEvent.dataEventType == 'Delete') {
            let keyValue = this.getKeyValue(dataEvent.name, dataEvent.record);
            let index = this.findIndex(vueArray, keyValue);
            vueArray.splice(index, 1);
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
        for (let keyFieldName in keyFieldNames) {
            let value = record[keyFieldName];
            if (!result) result += ';';
            result += '' + value;
        }
        return result;
    }
}