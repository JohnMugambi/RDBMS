using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDBMS.Core.Parsing;

/// <summary>
/// Represents a single token in SQL text
/// </summary>
/// 
public class Token
{
    public TokenType Type {  get; set; }
    public string Value { get; set; }
    public int Position { get; set; } // Position in original SQL string

    public Token(TokenType type, string value, int position = 0)
    {
        Type = type;
        Value = value;
        Position = position;
    }

    public override string ToString()
    {
        return $"[{Type}] '{Value}' @{Position}";
    }
}

/// <summary>
/// All possible token types in our SQL subset
/// </summary>
/// 
public enum TokenType
{
    //Key words
    SELECT,
    FROM,
    WHERE,
    INSERT,
    INTO,
    VALUES,
    UPDATE,
    SET,
    DELETE,
    CREATE,
    TABLE,
    DROP,
    INDEX,
    ON,
    PRIMARY,
    KEY,
    UNIQUE,
    NOT,
    NULL,
    AND,
    OR,
    JOIN,
    INNER,
    LEFT,
    RIGHT,
    OUTER,

    // Data Types
    INT,
    VARCHAR,
    BOOLEAN,
    DATETIME,
    DECIMAL,

    // Operators
    EQUALS,              // =
    NOT_EQUALS,          // != or <>
    GREATER_THAN,        // >
    LESS_THAN,           // 
    GREATER_OR_EQUAL,    // >=
    LESS_OR_EQUAL,       // <=
    PLUS,                // +
    MINUS,               // -
    ASTERISK,            // *
    SLASH,               // /

    // Delimiters
    LEFT_PAREN,          // (
    RIGHT_PAREN,         // )
    COMMA,               // ,
    SEMICOLON,           // ;
    DOT,                 // .

    // Literals
    STRING_LITERAL,      // 'hello' or "hello"
    NUMBER_LITERAL,      // 123, 45.67
    BOOLEAN_LITERAL,     // TRUE, FALSE
    NULL_LITERAL,        // NULL

    // Identifiers
    IDENTIFIER,          // table_name, column_name

    // Special
    EOF,                 // End of file
    UNKNOWN              // Error token
}
