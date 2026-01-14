using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDBMS.Core.Parsing;

/// <summary>
/// Tokenizer (Lexer) - Breaks SQL text into tokens
/// Handles keywords, identifiers, operators, literals, and delimiters
/// </summary>
/// 
public class Tokenizer
{
    private string _input;
    private int _position;
    private char _currentChar;

    // SQL keywords (case-insensitive)
    private static readonly HashSet<string> Keywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
         "SELECT", "FROM", "WHERE", "INSERT", "INTO", "VALUES",
            "UPDATE", "SET", "DELETE", "CREATE", "TABLE", "DROP",
            "INDEX", "ON", "PRIMARY", "KEY", "UNIQUE", "NOT", "NULL",
            "AND", "OR", "JOIN", "INNER", "LEFT", "RIGHT", "OUTER",
            "INT", "VARCHAR", "BOOLEAN", "DATETIME", "DECIMAL",
            "TRUE", "FALSE"
    };


    public Tokenizer(string input)
    {
        _input = input ?? throw new ArgumentException(nameof(input));
        _position = 0;
        _currentChar = _position < _input.Length ? _input[_position] : '\0';
    }

    /// <summary>
    /// Main tokenization method - converts SQL string to list of tokens
    /// </summary>
    /// 
    public List<Token> Tokenize()
    {
        var tokens = new List<Token>();

        while (_currentChar != '\0')
        {
            //Skip white spaces
            if (char.IsWhiteSpace(_currentChar))
            {
                SkipWhitespace();
                continue;
            }

            // Skip comments (-- style and /* */ style)
            if (_currentChar == '-' && Peek() == '-')
            {
                SkipLineComment();
                continue;
            }

            if (_currentChar == '/' && Peek() == '*')
            {
                SkipBlockComment();
                continue;
            }

            // String literals: 'hello' or "hello"
            if (_currentChar == '\'' || _currentChar == '"')
            {
                tokens.Add(ReadStringLiteral());
                continue;
            }

            // Number literals: 123 or 45.67
            if (char.IsDigit(_currentChar))
            {
                tokens.Add(ReadNumberLiteral());
                continue;
            }

            if (char.IsLetter(_currentChar) || _currentChar == '_')
            {
                tokens.Add(ReadIdentifierOrKeyword());
                continue;
            }

            // Operators and Delimiters
            Token token = ReadOperatorOrDelimiter();
            if (token != null)
            {
                tokens.Add(token);
                continue;
            }

            // Unknown character - create error token
            tokens.Add(new Token(TokenType.UNKNOWN, _currentChar.ToString(), _position));
            Advance();
        }

        // Add EOF token
        tokens.Add(new Token(TokenType.EOF, "", _position));

        return tokens;
    }

    #region Character Navigation

    /// <summary>
    /// Advance to next character
    /// </summary>
    /// 
    private void Advance()
    {
        _position++;
        _currentChar = _position < _input.Length ? _input[_position] : '\0';
    }

    /// <summary>
    /// Peek at next character without advancing
    /// </summary>
    private char Peek(int offset = 1)
    {
        int peekPos = _position + offset;
        return peekPos < _input.Length ? _input[peekPos] : '\0';
    }

    /// <summary>
    /// Skip whitespace characters
    /// </summary>
    private void SkipWhitespace()
    {
        while (_currentChar != '\0' && char.IsWhiteSpace(_currentChar))
        {
            Advance();
        }
    }

    /// <summary>
    /// Skip line comment: -- comment text
    /// </summary>
    private void SkipLineComment()
    {
        // Skip '--'
        Advance();
        Advance();

        // Skip until end of line or end of input
        while (_currentChar != '\0' && _currentChar != '\n')
        {
            Advance();
        }

        if (_currentChar == '\n')
        {
            Advance();
        }
    }

    /// <summary>
    /// Skip block comment: /* comment text */
    /// </summary>
    private void SkipBlockComment()
    {
        // Skip '/*'
        Advance();
        Advance();

        // Skip until '*/' or end of input
        while (_currentChar != '\0')
        {
            if (_currentChar == '*' && Peek() == '/')
            {
                Advance(); // Skip '*'
                Advance(); // Skip '/'
                break;
            }
            Advance();
        }
    }

    #endregion

    #region Token Reading Methods

    /// <summary>
    /// Read string literal: 'hello' or "world"
    /// Handles escaped quotes: 'don''t' or "say ""hello"""
    /// </summary>
    /// 
    private Token ReadStringLiteral()
    {
        int startPos = _position;
        char quoteChar = _currentChar;
        var sb = new StringBuilder();

        Advance();

        while (_currentChar != '\0' && _currentChar != quoteChar)
        {
            if (_currentChar == '\\') // Handle escape sequences
            {
                Advance();
                if (_currentChar != '\0')
                {
                    // Simple escape handling: \n, \t, \\, \'
                    switch (_currentChar)
                    {
                        case 'n': sb.Append('\n'); break;
                        case 't': sb.Append('\t'); break;
                        case 'r': sb.Append('\r'); break;
                        case '\\': sb.Append('\\'); break;
                        case '\'': sb.Append('\''); break;
                        case '"': sb.Append('"'); break;
                        default: sb.Append(_currentChar); break;
                    }
                    Advance();
                }
            }
            else if (_currentChar == quoteChar && Peek() == quoteChar)
            {
                // Handle doubled quotes: '' or ""
                sb.Append(quoteChar);
                Advance();
                Advance();
            }
            else
            {
                sb.Append(_currentChar);
                Advance();
            }
        }

        if (_currentChar == quoteChar)
        {
            Advance(); // Skip closing quote
        }
        else
        {
            throw new SqlSyntaxException($"Unterminated string literal at position {startPos}");
        }

        return new Token(TokenType.STRING_LITERAL, sb.ToString(), startPos);
    }

    /// <summary>
    /// Read number literal: 123 or 45.67
    /// </summary>
    private Token ReadNumberLiteral()
    {
        int startPos = _position;
        var sb = new StringBuilder();
        bool hasDecimalPoint = false;

        while (_currentChar != '\0' && (char.IsDigit(_currentChar) || _currentChar == '.'))
        {
            if (_currentChar == '.')
            {
                if (hasDecimalPoint)
                {
                    throw new SqlSyntaxException($"Invalid number format at position {_position}: multiple decimal points");
                }
                hasDecimalPoint = true;
            }

            sb.Append(_currentChar);
            Advance();
        }

        return new Token(TokenType.NUMBER_LITERAL, sb.ToString(), startPos);
    }

    /// <summary>
    /// Read identifier or keyword: table_name, SELECT, id123
    /// Identifiers can contain letters, digits, and underscores
    /// Must start with letter or underscore
    /// </summary>
    private Token ReadIdentifierOrKeyword()
    {
        int startPos = _position;
        var sb = new StringBuilder();

        while (_currentChar != '\0' && (char.IsLetterOrDigit(_currentChar) || _currentChar == '_'))
        {
            sb.Append(_currentChar);
            Advance();
        }

        string value = sb.ToString();

        // Check if it's a keyword
        if (Keywords.Contains(value))
        {
            // Convert to uppercase for token type matching
            string upperValue = value.ToUpper();

            // Handle special cases
            if (upperValue == "TRUE" || upperValue == "FALSE")
            {
                return new Token(TokenType.BOOLEAN_LITERAL, upperValue, startPos);
            }

            if (upperValue == "NULL")
            {
                return new Token(TokenType.NULL_LITERAL, upperValue, startPos);
            }

            // Map keyword to token type
            TokenType tokenType = Enum.Parse<TokenType>(upperValue, ignoreCase: true);
            return new Token(tokenType, upperValue, startPos);
        }

        // It's an identifier
        return new Token(TokenType.IDENTIFIER, value, startPos);
    }

    /// <summary>
    /// Read operator or delimiter: =, !=, >=, (, ), etc.
    /// </summary>
    private Token ReadOperatorOrDelimiter()
    {
        int startPos = _position;
        char current = _currentChar;
        char next = Peek();

        // Two-character operators
        if (current == '!' && next == '=')
        {
            Advance();
            Advance();
            return new Token(TokenType.NOT_EQUALS, "!=", startPos);
        }

        if (current == '<' && next == '>')
        {
            Advance();
            Advance();
            return new Token(TokenType.NOT_EQUALS, "<>", startPos);
        }

        if (current == '>' && next == '=')
        {
            Advance();
            Advance();
            return new Token(TokenType.GREATER_OR_EQUAL, ">=", startPos);
        }

        if (current == '<' && next == '=')
        {
            Advance();
            Advance();
            return new Token(TokenType.LESS_OR_EQUAL, "<=", startPos);
        }

        // Single-character operators and delimiters
        Token token = current switch
        {
            '=' => new Token(TokenType.EQUALS, "=", startPos),
            '>' => new Token(TokenType.GREATER_THAN, ">", startPos),
            '<' => new Token(TokenType.LESS_THAN, "<", startPos),
            '+' => new Token(TokenType.PLUS, "+", startPos),
            '-' => new Token(TokenType.MINUS, "-", startPos),
            '*' => new Token(TokenType.ASTERISK, "*", startPos),
            '/' => new Token(TokenType.SLASH, "/", startPos),
            '(' => new Token(TokenType.LEFT_PAREN, "(", startPos),
            ')' => new Token(TokenType.RIGHT_PAREN, ")", startPos),
            ',' => new Token(TokenType.COMMA, ",", startPos),
            ';' => new Token(TokenType.SEMICOLON, ";", startPos),
            '.' => new Token(TokenType.DOT, ".", startPos),
            _ => null
        };

        if (token != null)
        {
            Advance();
        }

        return token;
    }
    
    #endregion

    /// <summary>
    /// Exception thrown when SQL syntax is invalid during tokenization
    /// </summary>
    public class SqlSyntaxException : Exception
    {
        public SqlSyntaxException(string message) : base(message) { }
    }

};
