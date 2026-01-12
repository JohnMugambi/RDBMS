using RDBMS.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRDBMS.Core.Models;

/// <summary>
/// This is a column in a table
/// </summary>
public class Column
{
    public string Name { get; set; } = string.Empty;
    public DataType Type { get; set; }
    public int? MaxLength { get; set; }
    public bool IsPrimaryKey { get; set; }
    public bool IsUnique { get; set; }
    public bool IsNotNull { get; set; }

    public Column() { }

    public Column(string name, DataType type)
    {
        Name = name;
        Type = type;
    }

    /// <summary>
    /// Validate if value is compatible with column
    /// Respects constraints like NOT NULL and VARCHAR length
    ///  </summary> 
    public bool IsValidValue(object? value)
    {
        if (value == null)
        {
            return !IsNotNull;
        }
        return Type switch
        {
            DataType.INT => value is int,
            DataType.VARCHAR => value is string str && (MaxLength == null || str.Length <= MaxLength),
            DataType.BOOLEAN => value is bool,
            DataType.DATETIME => value is DateTime,
            DataType.DECIMAL => value is decimal,
            _ => false,
        };
    }

    /// <summary>
    /// convert the column definition to SQL-like string.
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        var constraints = new List<string>();
        if (IsPrimaryKey) constraints.Add("PRIMARY KEY");
        if (IsUnique) constraints.Add("UNIQUE");
        if (IsNotNull) constraints.Add("NOT NULL");

        var typeStr = Type == DataType.VARCHAR && MaxLength.HasValue
            ? $"{Type}({MaxLength})"
            : Type.ToString();

        var constraintStr = constraints.Any() ? $" {string.Join(" ", constraints)}" : "";
        return $"{Name} {typeStr}{constraintStr}";
    }
}