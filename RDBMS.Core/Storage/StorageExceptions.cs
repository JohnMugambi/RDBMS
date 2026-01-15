using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDBMS.Core.Storage;

public class TableNotFoundException : Exception
{
    public TableNotFoundException(string TableName) 
        : base($"Table {TableName} not found")
    {
    }
}

public class TableAlreadyExistsException : Exception
{
    public TableAlreadyExistsException(string tableName)
        : base($"Table '{tableName}' already exists")
    {
    }
}

public class StorageException : Exception
{
    public StorageException(string message) : base(message) { }

    public StorageException(string message, Exception innerException) : base(message, innerException) { }
}
