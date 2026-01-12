using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDBMS.Core.Models;
/// <summary>
/// Supported data types in the RDBMS
/// </summary>
public enum DataType
{
    INT,        // Integer: 1, 42, -100
    VARCHAR,    // String with max length
    BOOLEAN,    // true/false
    DATETIME,   // Date and time
    DECIMAL     // Decimal numbers: 3.14, 99.99
}
