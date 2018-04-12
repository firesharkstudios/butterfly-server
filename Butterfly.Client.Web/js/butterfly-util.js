if (!butterfly) var butterfly = {};
if (!butterfly.util) butterfly.util = {};

butterfly.util.FieldComparer = function(fieldNames) {
    let fieldNameArray = fieldNames ? fieldNames.split(',') : null;
    return function (a, b) {
        if (fieldNameArray) {
            for (let i = 0; i < fieldNameArray.length; i++) {
                let reverse = fieldNameArray[i].startsWith('-');
                let fieldName = reverse ? fieldNameArray[i].substring(1) : fieldNameArray[i];

                let valueA = a[fieldName];
                if (typeof valueA == 'string') valueA = valueA.toLowerCase();

                let valueB = b[fieldName];
                if (typeof valueB == 'string') valueB = valueB.toLowerCase();

                if (valueA < valueB) return reverse ? 1 : -1;
                if (valueA > valueB) return reverse ? -1 : 1;
            }
        }
        return 0;
    }
}

// From https://stackoverflow.com/questions/105034/create-guid-uuid-in-javascript
butterfly.util.uuid = function () {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
        var r = Math.random() * 16 | 0, v = c == 'x' ? r : (r & 0x3 | 0x8);
        return v.toString(16);
    });
}

butterfly.util.getOrCreateLocalStorageItem = function (key, createFunc) {
    let value = window.localStorage.getItem(key);
    if (!value) {
        value = createFunc();
        window.localStorage.setItem(key, value);
    }
    return value;
} 