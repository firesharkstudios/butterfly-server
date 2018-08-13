export default function(fieldNames) {
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
