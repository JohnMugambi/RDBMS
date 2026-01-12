using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDBMS.Core.Storage;

/// <summary>
/// Data Transfer Object for saving/loading table schema
/// </summary>
public class TableSchemaDto
{
    public string Name { get; set; } = string.Empty;
    public List<ColumnDto> Columns { get; set; } = new();
    public List<IndexDto> Indexes { get; set; } = new();
}

public class ColumnDto
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int? MaxLength { get; set; }
    public bool IsPrimaryKey { get; set; }
    public bool IsUnique { get; set; }
    public bool IsNotNull { get; set; }
}

public class IndexDto
{
    public string Name { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string ColumnName { get; set; } = string.Empty;
}


/// <summary>
/// DTO for saving/loading table data
/// </summary>
public class TableDataDto
{
    public List<Dictionary<string, object?>> Rows { get; set; } = new();
}