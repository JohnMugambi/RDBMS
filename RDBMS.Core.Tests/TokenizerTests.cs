using RDBMS.Core.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDBMS.Core.Tests
{
    public class TokenizerTests
    {
        #region Keyword Tests

        [Fact]
        public void Tokenize_SelectStatement_ReturnsCorrectTokens()
        {
            // Arrange
            string sql = "SELECT name FROM users";
            var tokenizer = new Tokenizer(sql);

            // Act
            var tokens = tokenizer.Tokenize();

            // Assert
            Assert.Equal(5, tokens.Count); // SELECT, name, FROM, users, EOF
            Assert.Equal(TokenType.SELECT, tokens[0].Type);
            Assert.Equal(TokenType.IDENTIFIER, tokens[1].Type);
            Assert.Equal("name", tokens[1].Value);
            Assert.Equal(TokenType.FROM, tokens[2].Type);
            Assert.Equal(TokenType.IDENTIFIER, tokens[3].Type);
            Assert.Equal("users", tokens[3].Value);
            Assert.Equal(TokenType.EOF, tokens[4].Type);
        }

        [Fact]
        public void Tokenize_AllKeywords_RecognizedCorrectly()
        {
            // Arrange
            var keywords = new[]
            {
                ("SELECT", TokenType.SELECT),
                ("FROM", TokenType.FROM),
                ("WHERE", TokenType.WHERE),
                ("INSERT", TokenType.INSERT),
                ("INTO", TokenType.INTO),
                ("VALUES", TokenType.VALUES),
                ("UPDATE", TokenType.UPDATE),
                ("SET", TokenType.SET),
                ("DELETE", TokenType.DELETE),
                ("CREATE", TokenType.CREATE),
                ("TABLE", TokenType.TABLE),
                ("DROP", TokenType.DROP),
                ("INDEX", TokenType.INDEX),
                ("ON", TokenType.ON),
                ("PRIMARY", TokenType.PRIMARY),
                ("KEY", TokenType.KEY),
                ("UNIQUE", TokenType.UNIQUE),
                ("NOT", TokenType.NOT),
                ("NULL", TokenType.NULL_LITERAL),
                ("AND", TokenType.AND),
                ("OR", TokenType.OR),
                ("JOIN", TokenType.JOIN),
                ("INNER", TokenType.INNER),
                ("LEFT", TokenType.LEFT),
                ("RIGHT", TokenType.RIGHT),
                ("OUTER", TokenType.OUTER)
            };

            foreach (var (keyword, expectedType) in keywords)
            {
                // Act
                var tokens = new Tokenizer(keyword).Tokenize();

                // Assert
                Assert.Equal(expectedType, tokens[0].Type);
            }
        }

        [Fact]
        public void Tokenize_Keywords_CaseInsensitive()
        {
            // Arrange
            var variations = new[] { "SELECT", "select", "Select", "SeLeCt" };

            foreach (var keyword in variations)
            {
                // Act
                var tokens = new Tokenizer(keyword).Tokenize();

                // Assert
                Assert.Equal(TokenType.SELECT, tokens[0].Type);
                Assert.Equal("SELECT", tokens[0].Value); // Should be normalized to uppercase
            }
        }

        #endregion

        #region Data Type Tests

        [Fact]
        public void Tokenize_DataTypes_RecognizedCorrectly()
        {
            // Arrange
            var dataTypes = new[]
            {
                ("INT", TokenType.INT),
                ("VARCHAR", TokenType.VARCHAR),
                ("BOOLEAN", TokenType.BOOLEAN),
                ("DATETIME", TokenType.DATETIME),
                ("DECIMAL", TokenType.DECIMAL)
            };

            foreach (var (dataType, expectedType) in dataTypes)
            {
                // Act
                var tokens = new Tokenizer(dataType).Tokenize();

                // Assert
                Assert.Equal(expectedType, tokens[0].Type);
            }
        }

        #endregion

        #region Identifier Tests

        [Fact]
        public void Tokenize_SimpleIdentifier_RecognizedCorrectly()
        {
            // Arrange
            string sql = "table_name";
            var tokenizer = new Tokenizer(sql);

            // Act
            var tokens = tokenizer.Tokenize();

            // Assert
            Assert.Equal(TokenType.IDENTIFIER, tokens[0].Type);
            Assert.Equal("table_name", tokens[0].Value);
        }

        [Fact]
        public void Tokenize_IdentifierWithNumbers_RecognizedCorrectly()
        {
            // Arrange
            string sql = "user123 id1 table2";
            var tokenizer = new Tokenizer(sql);

            // Act
            var tokens = tokenizer.Tokenize();

            // Assert
            Assert.Equal(TokenType.IDENTIFIER, tokens[0].Type);
            Assert.Equal("user123", tokens[0].Value);
            Assert.Equal(TokenType.IDENTIFIER, tokens[1].Type);
            Assert.Equal("id1", tokens[1].Value);
        }

        [Fact]
        public void Tokenize_IdentifierWithUnderscores_RecognizedCorrectly()
        {
            // Arrange
            string sql = "_private __internal user_id";
            var tokenizer = new Tokenizer(sql);

            // Act
            var tokens = tokenizer.Tokenize();

            // Assert
            Assert.Equal(3, tokens.Count - 1); // Excluding EOF
            Assert.All(tokens.Take(3), token => Assert.Equal(TokenType.IDENTIFIER, token.Type));
        }

        #endregion

        #region String Literal Tests

        [Fact]
        public void Tokenize_SingleQuotedString_RecognizedCorrectly()
        {
            // Arrange
            string sql = "'Hello World'";
            var tokenizer = new Tokenizer(sql);

            // Act
            var tokens = tokenizer.Tokenize();

            // Assert
            Assert.Equal(TokenType.STRING_LITERAL, tokens[0].Type);
            Assert.Equal("Hello World", tokens[0].Value);
        }

        [Fact]
        public void Tokenize_DoubleQuotedString_RecognizedCorrectly()
        {
            // Arrange
            string sql = "\"Hello World\"";
            var tokenizer = new Tokenizer(sql);

            // Act
            var tokens = tokenizer.Tokenize();

            // Assert
            Assert.Equal(TokenType.STRING_LITERAL, tokens[0].Type);
            Assert.Equal("Hello World", tokens[0].Value);
        }

        [Fact]
        public void Tokenize_StringWithEscapedQuotes_HandlesCorrectly()
        {
            // Arrange
            string sql = "'John''s Blog'";
            var tokenizer = new Tokenizer(sql);

            // Act
            var tokens = tokenizer.Tokenize();

            // Assert
            Assert.Equal(TokenType.STRING_LITERAL, tokens[0].Type);
            Assert.Equal("John's Blog", tokens[0].Value);
        }

        [Fact]
        public void Tokenize_StringWithEscapeSequences_HandlesCorrectly()
        {
            // Arrange
            string sql = "'Line1\\nLine2\\tTabbed'";
            var tokenizer = new Tokenizer(sql);

            // Act
            var tokens = tokenizer.Tokenize();

            // Assert
            Assert.Equal(TokenType.STRING_LITERAL, tokens[0].Type);
            Assert.Contains("\n", tokens[0].Value);
            Assert.Contains("\t", tokens[0].Value);
        }

        [Fact]
        public void Tokenize_EmptyString_RecognizedCorrectly()
        {
            // Arrange
            string sql = "''";
            var tokenizer = new Tokenizer(sql);

            // Act
            var tokens = tokenizer.Tokenize();

            // Assert
            Assert.Equal(TokenType.STRING_LITERAL, tokens[0].Type);
            Assert.Equal("", tokens[0].Value);
        }

        [Fact]
        public void Tokenize_UnterminatedString_ThrowsException()
        {
            // Arrange
            string sql = "'Unterminated string";
            var tokenizer = new Tokenizer(sql);

            // Act & Assert
            Assert.Throws<SqlSyntaxException>(() => tokenizer.Tokenize());
        }

        #endregion

        #region Number Literal Tests

        [Fact]
        public void Tokenize_Integer_RecognizedCorrectly()
        {
            // Arrange
            string sql = "42";
            var tokenizer = new Tokenizer(sql);

            // Act
            var tokens = tokenizer.Tokenize();

            // Assert
            Assert.Equal(TokenType.NUMBER_LITERAL, tokens[0].Type);
            Assert.Equal("42", tokens[0].Value);
        }

        [Fact]
        public void Tokenize_Decimal_RecognizedCorrectly()
        {
            // Arrange
            string sql = "3.14159";
            var tokenizer = new Tokenizer(sql);

            // Act
            var tokens = tokenizer.Tokenize();

            // Assert
            Assert.Equal(TokenType.NUMBER_LITERAL, tokens[0].Type);
            Assert.Equal("3.14159", tokens[0].Value);
        }

        [Fact]
        public void Tokenize_MultipleNumbers_RecognizedCorrectly()
        {
            // Arrange
            string sql = "1 2.5 100 99.99";
            var tokenizer = new Tokenizer(sql);

            // Act
            var tokens = tokenizer.Tokenize();

            // Assert
            Assert.Equal(5, tokens.Count); // 4 numbers + EOF
            Assert.All(tokens.Take(4), token => Assert.Equal(TokenType.NUMBER_LITERAL, token.Type));
        }

        [Fact]
        public void Tokenize_NumberWithMultipleDecimalPoints_ThrowsException()
        {
            // Arrange
            string sql = "3.14.159";
            var tokenizer = new Tokenizer(sql);

            // Act & Assert
            Assert.Throws<SqlSyntaxException>(() => tokenizer.Tokenize());
        }

        #endregion

        #region Boolean Literal Tests

        [Fact]
        public void Tokenize_TrueKeyword_RecognizedAsBoolean()
        {
            // Arrange
            string sql = "TRUE";
            var tokenizer = new Tokenizer(sql);

            // Act
            var tokens = tokenizer.Tokenize();

            // Assert
            Assert.Equal(TokenType.BOOLEAN_LITERAL, tokens[0].Type);
            Assert.Equal("TRUE", tokens[0].Value);
        }

        [Fact]
        public void Tokenize_FalseKeyword_RecognizedAsBoolean()
        {
            // Arrange
            string sql = "FALSE";
            var tokenizer = new Tokenizer(sql);

            // Act
            var tokens = tokenizer.Tokenize();

            // Assert
            Assert.Equal(TokenType.BOOLEAN_LITERAL, tokens[0].Type);
            Assert.Equal("FALSE", tokens[0].Value);
        }

        [Fact]
        public void Tokenize_BooleanCaseInsensitive_RecognizedCorrectly()
        {
            // Arrange
            var variations = new[] { "TRUE", "true", "True", "FALSE", "false", "False" };

            foreach (var boolean in variations)
            {
                // Act
                var tokens = new Tokenizer(boolean).Tokenize();

                // Assert
                Assert.Equal(TokenType.BOOLEAN_LITERAL, tokens[0].Type);
            }
        }

        #endregion

        #region Null Literal Tests

        [Fact]
        public void Tokenize_NullKeyword_RecognizedAsNull()
        {
            // Arrange
            string sql = "NULL";
            var tokenizer = new Tokenizer(sql);

            // Act
            var tokens = tokenizer.Tokenize();

            // Assert
            Assert.Equal(TokenType.NULL_LITERAL, tokens[0].Type);
            Assert.Equal("NULL", tokens[0].Value);
        }

        #endregion

        #region Operator Tests

        [Fact]
        public void Tokenize_ComparisonOperators_RecognizedCorrectly()
        {
            // Arrange
            var operators = new[]
            {
                ("=", TokenType.EQUALS),
                ("!=", TokenType.NOT_EQUALS),
                ("<>", TokenType.NOT_EQUALS),
                (">", TokenType.GREATER_THAN),
                ("<", TokenType.LESS_THAN),
                (">=", TokenType.GREATER_OR_EQUAL),
                ("<=", TokenType.LESS_OR_EQUAL)
            };

            foreach (var (op, expectedType) in operators)
            {
                // Act
                var tokens = new Tokenizer(op).Tokenize();

                // Assert
                Assert.Equal(expectedType, tokens[0].Type);
                Assert.Equal(op, tokens[0].Value);
            }
        }

        [Fact]
        public void Tokenize_ArithmeticOperators_RecognizedCorrectly()
        {
            // Arrange
            var operators = new[]
            {
                ("+", TokenType.PLUS),
                ("-", TokenType.MINUS),
                ("*", TokenType.ASTERISK),
                ("/", TokenType.SLASH)
            };

            foreach (var (op, expectedType) in operators)
            {
                // Act
                var tokens = new Tokenizer(op).Tokenize();

                // Assert
                Assert.Equal(expectedType, tokens[0].Type);
            }
        }

        #endregion

        #region Delimiter Tests

        [Fact]
        public void Tokenize_Delimiters_RecognizedCorrectly()
        {
            // Arrange
            var delimiters = new[]
            {
                ("(", TokenType.LEFT_PAREN),
                (")", TokenType.RIGHT_PAREN),
                (",", TokenType.COMMA),
                (";", TokenType.SEMICOLON),
                (".", TokenType.DOT)
            };

            foreach (var (delimiter, expectedType) in delimiters)
            {
                // Act
                var tokens = new Tokenizer(delimiter).Tokenize();

                // Assert
                Assert.Equal(expectedType, tokens[0].Type);
            }
        }

        #endregion

        #region Whitespace Tests

        [Fact]
        public void Tokenize_WhitespaceIgnored_TokensCorrect()
        {
            // Arrange
            string sql = "  SELECT   name   FROM   users  ";
            var tokenizer = new Tokenizer(sql);

            // Act
            var tokens = tokenizer.Tokenize();

            // Assert
            Assert.Equal(5, tokens.Count); // SELECT, name, FROM, users, EOF
            Assert.Equal(TokenType.SELECT, tokens[0].Type);
            Assert.Equal(TokenType.IDENTIFIER, tokens[1].Type);
        }

        [Fact]
        public void Tokenize_NewlinesIgnored_TokensCorrect()
        {
            // Arrange
            string sql = "SELECT\nname\nFROM\nusers";
            var tokenizer = new Tokenizer(sql);

            // Act
            var tokens = tokenizer.Tokenize();

            // Assert
            Assert.Equal(5, tokens.Count);
            Assert.Equal(TokenType.SELECT, tokens[0].Type);
        }

        [Fact]
        public void Tokenize_TabsIgnored_TokensCorrect()
        {
            // Arrange
            string sql = "SELECT\tname\tFROM\tusers";
            var tokenizer = new Tokenizer(sql);

            // Act
            var tokens = tokenizer.Tokenize();

            // Assert
            Assert.Equal(5, tokens.Count);
        }

        #endregion

        #region Comment Tests

        [Fact]
        public void Tokenize_LineComment_Ignored()
        {
            // Arrange
            string sql = "SELECT name FROM users -- this is a comment";
            var tokenizer = new Tokenizer(sql);

            // Act
            var tokens = tokenizer.Tokenize();

            // Assert
            Assert.Equal(5, tokens.Count); // Comment should be ignored
            Assert.Equal(TokenType.SELECT, tokens[0].Type);
            Assert.Equal(TokenType.EOF, tokens[4].Type);
        }

        [Fact]
        public void Tokenize_LineCommentAtStart_Ignored()
        {
            // Arrange
            string sql = "-- comment\nSELECT name FROM users";
            var tokenizer = new Tokenizer(sql);

            // Act
            var tokens = tokenizer.Tokenize();

            // Assert
            Assert.Equal(5, tokens.Count);
            Assert.Equal(TokenType.SELECT, tokens[0].Type);
        }

        [Fact]
        public void Tokenize_BlockComment_Ignored()
        {
            // Arrange
            string sql = "SELECT name /* this is a block comment */ FROM users";
            var tokenizer = new Tokenizer(sql);

            // Act
            var tokens = tokenizer.Tokenize();

            // Assert
            Assert.Equal(5, tokens.Count);
            Assert.Equal(TokenType.SELECT, tokens[0].Type);
            Assert.Equal(TokenType.IDENTIFIER, tokens[1].Type);
            Assert.Equal(TokenType.FROM, tokens[2].Type);
        }

        [Fact]
        public void Tokenize_MultilineBlockComment_Ignored()
        {
            // Arrange
            string sql = @"SELECT name /* this is 
                          a multiline
                          block comment */ FROM users";
            var tokenizer = new Tokenizer(sql);

            // Act
            var tokens = tokenizer.Tokenize();

            // Assert
            Assert.Equal(5, tokens.Count);
        }

        #endregion

        #region Complex Query Tests

        [Fact]
        public void Tokenize_CompleteInsertStatement_TokenizesCorrectly()
        {
            // Arrange
            string sql = "INSERT INTO users (name, age) VALUES ('John', 25)";
            var tokenizer = new Tokenizer(sql);

            // Act
            var tokens = tokenizer.Tokenize();

            // Assert
            Assert.Equal(14, tokens.Count); // Including EOF
            Assert.Equal(TokenType.INSERT, tokens[0].Type);
            Assert.Equal(TokenType.INTO, tokens[1].Type);
            Assert.Equal(TokenType.IDENTIFIER, tokens[2].Type);
            Assert.Equal(TokenType.LEFT_PAREN, tokens[3].Type);
            Assert.Equal(TokenType.STRING_LITERAL, tokens[9].Type);
            Assert.Equal(TokenType.NUMBER_LITERAL, tokens[11].Type);
        }

        [Fact]
        public void Tokenize_CompleteSelectWithWhere_TokenizesCorrectly()
        {
            // Arrange
            string sql = "SELECT id, name FROM users WHERE id = 1 AND age > 18";
            var tokenizer = new Tokenizer(sql);

            // Act
            var tokens = tokenizer.Tokenize();

            // Assert
            Assert.Equal(15, tokens.Count); // Including EOF
            Assert.Equal(TokenType.SELECT, tokens[0].Type);
            Assert.Equal(TokenType.WHERE, tokens[6].Type);
            Assert.Equal(TokenType.AND, tokens[10].Type);
        }

        [Fact]
        public void Tokenize_CreateTableStatement_TokenizesCorrectly()
        {
            // Arrange
            string sql = "CREATE TABLE users (id INT PRIMARY KEY, name VARCHAR(100))";
            var tokenizer = new Tokenizer(sql);

            // Act
            var tokens = tokenizer.Tokenize();

            // Assert
            Assert.Equal(16, tokens.Count); // Including EOF
            Assert.Equal(TokenType.CREATE, tokens[0].Type);
            Assert.Equal(TokenType.TABLE, tokens[1].Type);
            Assert.Equal(TokenType.PRIMARY, tokens[6].Type);
            Assert.Equal(TokenType.KEY, tokens[7].Type);
            Assert.Equal(TokenType.VARCHAR, tokens[10].Type);
        }

        [Fact]
        public void Tokenize_SelectWithJoin_TokenizesCorrectly()
        {
            // Arrange
            string sql = "SELECT users.name, orders.product FROM users INNER JOIN orders ON users.id = orders.user_id";
            var tokenizer = new Tokenizer(sql);

            // Act
            var tokens = tokenizer.Tokenize();

            // Assert
            var tokenTypes = tokens.Select(t => t.Type).ToList();
            Assert.Contains(TokenType.INNER, tokenTypes);
            Assert.Contains(TokenType.JOIN, tokenTypes);
            Assert.Contains(TokenType.ON, tokenTypes);
            Assert.Contains(TokenType.DOT, tokenTypes);
        }

        [Fact]
        public void Tokenize_UpdateStatement_TokenizesCorrectly()
        {
            // Arrange
            string sql = "UPDATE users SET name = 'Jane', age = 26 WHERE id = 1";
            var tokenizer = new Tokenizer(sql);

            // Act
            var tokens = tokenizer.Tokenize();

            // Assert
            Assert.Equal(TokenType.UPDATE, tokens[0].Type);
            Assert.Equal(TokenType.SET, tokens[2].Type);
            Assert.Equal(TokenType.WHERE, tokens[10].Type);
        }

        [Fact]
        public void Tokenize_DeleteStatement_TokenizesCorrectly()
        {
            // Arrange
            string sql = "DELETE FROM users WHERE id = 1";
            var tokenizer = new Tokenizer(sql);

            // Act
            var tokens = tokenizer.Tokenize();

            // Assert
            Assert.Equal(8, tokens.Count); // Including EOF
            Assert.Equal(TokenType.DELETE, tokens[0].Type);
            Assert.Equal(TokenType.FROM, tokens[1].Type);
        }

        #endregion

        #region Position Tracking Tests

        [Fact]
        public void Tokenize_TracksTokenPositions_Correctly()
        {
            // Arrange
            string sql = "SELECT name FROM users";
            var tokenizer = new Tokenizer(sql);

            // Act
            var tokens = tokenizer.Tokenize();

            // Assert
            Assert.Equal(0, tokens[0].Position);  // SELECT at position 0
            Assert.Equal(7, tokens[1].Position);  // name at position 7
            Assert.Equal(12, tokens[2].Position); // FROM at position 12
            Assert.Equal(17, tokens[3].Position); // users at position 17
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void Tokenize_EmptyString_ReturnsOnlyEOF()
        {
            // Arrange
            string sql = "";
            var tokenizer = new Tokenizer(sql);

            // Act
            var tokens = tokenizer.Tokenize();

            // Assert
            Assert.Single(tokens);
            Assert.Equal(TokenType.EOF, tokens[0].Type);
        }

        [Fact]
        public void Tokenize_OnlyWhitespace_ReturnsOnlyEOF()
        {
            // Arrange
            string sql = "   \n\t   ";
            var tokenizer = new Tokenizer(sql);

            // Act
            var tokens = tokenizer.Tokenize();

            // Assert
            Assert.Single(tokens);
            Assert.Equal(TokenType.EOF, tokens[0].Type);
        }

        [Fact]
        public void Tokenize_OnlyComments_ReturnsOnlyEOF()
        {
            // Arrange
            string sql = "-- just a comment\n/* and a block comment */";
            var tokenizer = new Tokenizer(sql);

            // Act
            var tokens = tokenizer.Tokenize();

            // Assert
            Assert.Single(tokens);
            Assert.Equal(TokenType.EOF, tokens[0].Type);
        }

        [Fact]
        public void Tokenize_SpecialCharactersInString_HandledCorrectly()
        {
            // Arrange
            string sql = "'Special chars: !@#$%^&*()'";
            var tokenizer = new Tokenizer(sql);

            // Act
            var tokens = tokenizer.Tokenize();

            // Assert
            Assert.Equal(TokenType.STRING_LITERAL, tokens[0].Type);
            Assert.Contains("!@#$%^&*()", tokens[0].Value);
        }

        [Fact]
        public void Tokenize_ConsecutiveOperators_TokenizedSeparately()
        {
            // Arrange
            string sql = ">=<=!=";
            var tokenizer = new Tokenizer(sql);

            // Act
            var tokens = tokenizer.Tokenize();

            // Assert
            Assert.Equal(4, tokens.Count); // >=, <=, !=, EOF
            Assert.Equal(TokenType.GREATER_OR_EQUAL, tokens[0].Type);
            Assert.Equal(TokenType.LESS_OR_EQUAL, tokens[1].Type);
            Assert.Equal(TokenType.NOT_EQUALS, tokens[2].Type);
        }

        #endregion

        #region Integration Tests

        [Theory]
        [InlineData("SELECT * FROM users")]
        [InlineData("INSERT INTO users VALUES (1, 'John')")]
        [InlineData("UPDATE users SET name = 'Jane'")]
        [InlineData("DELETE FROM users WHERE id = 1")]
        [InlineData("CREATE TABLE users (id INT)")]
        [InlineData("DROP TABLE users")]
        public void Tokenize_CommonQueries_DoesNotThrow(string sql)
        {
            // Arrange
            var tokenizer = new Tokenizer(sql);

            // Act & Assert - should not throw
            var tokens = tokenizer.Tokenize();
            Assert.NotEmpty(tokens);
            Assert.Equal(TokenType.EOF, tokens.Last().Type);
        }

        [Fact]
        public void Tokenize_RealWorldQuery_TokenizesSuccessfully()
        {
            // Arrange
            string sql = @"
                SELECT u.id, u.name, o.total
                FROM users u
                INNER JOIN orders o ON u.id = o.user_id
                WHERE u.age >= 18 AND o.total > 100.00
                -- Get active users only
            ";
            var tokenizer = new Tokenizer(sql);

            // Act
            var tokens = tokenizer.Tokenize();

            // Assert
            Assert.NotEmpty(tokens);
            Assert.Equal(TokenType.EOF, tokens.Last().Type);
            Assert.Contains(tokens, t => t.Type == TokenType.SELECT);
            Assert.Contains(tokens, t => t.Type == TokenType.INNER);
            Assert.Contains(tokens, t => t.Type == TokenType.JOIN);
            Assert.Contains(tokens, t => t.Type == TokenType.WHERE);
            Assert.Contains(tokens, t => t.Type == TokenType.AND);
        }

        #endregion
    }
}