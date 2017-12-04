# IDatabase.SetInsertDefaultValue method

Allows specifying a lambda that creates a default value for a field when executing an INSERT. If the tableName parameter is left null, the getDefaultValue lambda will be applied to all tables.

```csharp
public void SetInsertDefaultValue(string fieldName, Func<object> getDefaultValue, string tableName = null)
```

| parameter | description |
| --- | --- |
| fieldName | Name of the field |
| getDefaultValue | The lambda that returns the default value |
| tableName | An optional table name. If not null, the getDefaultValue lambda is only applied to the specified table. If null, the getDefaultValue lambda is applied to all tables. |

## See Also

* interface [IDatabase](../IDatabase.md)
* namespace [Butterfly.Database](../../Butterfly.Database.md)

<!-- DO NOT EDIT: generated by xmldocmd for Butterfly.Database.dll -->