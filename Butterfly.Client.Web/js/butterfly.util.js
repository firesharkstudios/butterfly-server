function FieldComparer(fieldName) {
    return function (a, b) {
        let valueA = a[fieldName];
        let valueB = b[fieldName];
        if (valueA < valueB) return -1;
        if (valueA > valueB) return 1;
        return 0;
    }
}

// From https://stackoverflow.com/questions/105034/create-guid-uuid-in-javascript
function uuidv4() {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
        var r = Math.random() * 16 | 0, v = c == 'x' ? r : (r & 0x3 | 0x8);
        return v.toString(16);
    });
}

function getOrCreateLocalStorageItem(key, createFunc) {
    let value = window.localStorage.getItem(key);
    if (!value) {
        value = createFunc();
        window.localStorage.setItem(key, value);
    }
    return value;
}

function authorizedAjax(method, uri, authorization, value) {
    return $.ajax(uri, {
        method: method,
        headers: {
            'Authorization': authorization,
        },
        data: JSON.stringify(value),
        processData: false,
    });
}