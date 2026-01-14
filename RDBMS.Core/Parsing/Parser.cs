using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static RDBMS.Core.Parsing.Tokenizer;

namespace RDBMS.Core.Parsing
{
    /// <summary>
    /// Recursive Descent Parser - Converts tokens into query objects
    /// </summary>
    public class Parser
    {
        private List<Token> _tokens;
        private int _position;
        private Token _currentToken;

        public Parser(List<Token> tokens)
        {
            _tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
            _position = 0;
            _currentToken = _tokens.Count > 0 ? _tokens[0] : new Token(TokenType.EOF, "", 0);
        }

        /// <summary>
        /// Main parse method - determines query type and delegates to specific parser
        /// </summary>
        public IQuery Parse()
        {
            // Determine query type from first token
            return _currentToken.Type switch
            {
                TokenType.SELECT => ParseSelect(),
                TokenType.INSERT => ParseInsert(),
                TokenType.UPDATE => ParseUpdate(),
                TokenType.DELETE => ParseDelete(),
                TokenType.CREATE => ParseCreate(),
                TokenType.DROP => ParseDrop(),
                _ => throw new SqlSyntaxException($"Unexpected token: {_currentToken.Type} at position {_currentToken.Position}")
            };
        }

        #region Token Navigation

        /// <summary>
        /// Move to next token
        /// </summary>
        private void Advance()
        {
            _position++;
            _currentToken = _position < _tokens.Count ? _tokens[_position] : new Token(TokenType.EOF, "", 0);
        }

        /// <summary>
        /// Peek at next token without advancing
        /// </summary>
        private Token Peek(int offset = 1)
        {
            int peekPos = _position + offset;
            return peekPos < _tokens.Count ? _tokens[peekPos] : new Token(TokenType.EOF, "", 0);
        }

        /// <summary>
        /// Consume expected token type, throw error if mismatch
        /// </summary>
        private Token Consume(TokenType expected, string errorMessage = null)
        {
            if (_currentToken.Type != expected)
            {
                throw new SqlSyntaxException(
                    errorMessage ?? $"Expected {expected}, got {_currentToken.Type} at position {_currentToken.Position}"
                );
            }

            var token = _currentToken;
            Advance();
            return token;
        }

        /// <summary>
        /// Check if current token matches expected type without consuming
        /// </summary>
        private bool Check(TokenType type)
        {
            return _currentToken.Type == type;
        }

        /// <summary>
        /// Check and consume if match, return true if matched
        /// </summary>
        private bool Match(params TokenType[] types)
        {
            foreach (var type in types)
            {
                if (Check(type))
                {
                    Advance();
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region SELECT Parser

        /// <summary>
        /// Parse SELECT statement
        /// Grammar: SELECT columns FROM table [WHERE condition] [JOIN ...] [ORDER BY ...] [LIMIT n]
        /// </summary>
        private SelectQuery ParseSelect()
        {
            var query = new SelectQuery();

            Consume(TokenType.SELECT);

            // Parse columns
            query.Columns = ParseSelectColumns();

            // FROM clause
            Consume(TokenType.FROM, "Expected FROM after column list");
            query.TableName = Consume(TokenType.IDENTIFIER, "Expected table name after FROM").Value;

            // Optional JOIN clauses
            while (Check(TokenType.INNER) || Check(TokenType.LEFT) || Check(TokenType.RIGHT) || Check(TokenType.JOIN))
            {
                query.Joins.Add(ParseJoin());
            }

            // Optional WHERE clause
            if (Match(TokenType.WHERE))
            {
                query.Where = ParseWhereClause();
            }

            // Optional ORDER BY
            if (Match(TokenType.IDENTIFIER))
            {
                // Check if it's ORDER keyword (we don't have it as enum)
                if (_tokens[_position - 1].Value.Equals("ORDER", StringComparison.OrdinalIgnoreCase))
                {
                    Consume(TokenType.IDENTIFIER); // Consume BY
                    query.OrderBy = ParseOrderBy();
                }
                else
                {
                    // Backtrack
                    _position--;
                    _currentToken = _tokens[_position];
                }
            }

            // Optional LIMIT (if we implement it)
            // For now, skipping LIMIT parsing

            return query;
        }

        /// <summary>
        /// Parse column list: *, col1, col2, table.col, col AS alias
        /// </summary>
        private List<SelectColumn> ParseSelectColumns()
        {
            var columns = new List<SelectColumn>();

            do
            {
                // Check for wildcard *
                if (Match(TokenType.ASTERISK))
                {
                    columns.Add(new SelectColumn { ColumnName = "*" });
                    continue;
                }

                // Parse column (potentially qualified: table.column)
                var col = new SelectColumn();

                string firstPart = Consume(TokenType.IDENTIFIER, "Expected column name").Value;

                // Check for table.column notation
                if (Match(TokenType.DOT))
                {
                    col.TableName = firstPart;
                    col.ColumnName = Consume(TokenType.IDENTIFIER, "Expected column name after dot").Value;
                }
                else
                {
                    col.ColumnName = firstPart;
                }

                // Check for AS alias
                if (_currentToken.Type == TokenType.IDENTIFIER &&
                    _currentToken.Value.Equals("AS", StringComparison.OrdinalIgnoreCase))
                {
                    Advance(); // Consume AS
                    col.Alias = Consume(TokenType.IDENTIFIER, "Expected alias after AS").Value;
                }

                columns.Add(col);

            } while (Match(TokenType.COMMA));

            return columns;
        }

        #endregion

        #region INSERT Parser

        /// <summary>
        /// Parse INSERT statement
        /// Grammar: INSERT INTO table [(col1, col2, ...)] VALUES (val1, val2, ...), (...)
        /// </summary>
        private InsertQuery ParseInsert()
        {
            var query = new InsertQuery();

            Consume(TokenType.INSERT);
            Consume(TokenType.INTO);
            query.TableName = Consume(TokenType.IDENTIFIER, "Expected table name").Value;

            // Optional column list
            if (Match(TokenType.LEFT_PAREN))
            {
                do
                {
                    query.Columns.Add(Consume(TokenType.IDENTIFIER, "Expected column name").Value);
                } while (Match(TokenType.COMMA));

                Consume(TokenType.RIGHT_PAREN, "Expected closing parenthesis");
            }

            // VALUES clause
            Consume(TokenType.VALUES, "Expected VALUES keyword");

            // Parse value lists
            do
            {
                Consume(TokenType.LEFT_PAREN, "Expected opening parenthesis");

                var valueList = new List<object>();
                do
                {
                    valueList.Add(ParseValue());
                } while (Match(TokenType.COMMA));

                Consume(TokenType.RIGHT_PAREN, "Expected closing parenthesis");
                query.Values.Add(valueList);

            } while (Match(TokenType.COMMA));

            return query;
        }

        #endregion

        #region UPDATE Parser

        /// <summary>
        /// Parse UPDATE statement
        /// Grammar: UPDATE table SET col1=val1, col2=val2 [WHERE condition]
        /// </summary>
        private UpdateQuery ParseUpdate()
        {
            var query = new UpdateQuery();

            Consume(TokenType.UPDATE);
            query.TableName = Consume(TokenType.IDENTIFIER, "Expected table name").Value;

            Consume(TokenType.SET, "Expected SET keyword");

            // Parse assignments
            do
            {
                var assignment = new Assignment
                {
                    ColumnName = Consume(TokenType.IDENTIFIER, "Expected column name").Value
                };

                Consume(TokenType.EQUALS, "Expected = operator");
                assignment.Value = ParseValue();

                query.Assignments.Add(assignment);

            } while (Match(TokenType.COMMA));

            // Optional WHERE
            if (Match(TokenType.WHERE))
            {
                query.Where = ParseWhereClause();
            }

            return query;
        }

        #endregion

        #region DELETE Parser

        /// <summary>
        /// Parse DELETE statement
        /// Grammar: DELETE FROM table [WHERE condition]
        /// </summary>
        private DeleteQuery ParseDelete()
        {
            var query = new DeleteQuery();

            Consume(TokenType.DELETE);
            Consume(TokenType.FROM);
            query.TableName = Consume(TokenType.IDENTIFIER, "Expected table name").Value;

            // Optional WHERE
            if (Match(TokenType.WHERE))
            {
                query.Where = ParseWhereClause();
            }

            return query;
        }

        #endregion

        #region CREATE Parser

        /// <summary>
        /// Parse CREATE statement (TABLE or INDEX)
        /// </summary>
        private IQuery ParseCreate()
        {
            Consume(TokenType.CREATE);

            if (Check(TokenType.TABLE))
            {
                return ParseCreateTable();
            }
            else if (Check(TokenType.INDEX))
            {
                return ParseCreateIndex();
            }

            throw new SqlSyntaxException($"Expected TABLE or INDEX after CREATE at position {_currentToken.Position}");
        }

        /// <summary>
        /// Parse CREATE TABLE
        /// Grammar: CREATE TABLE name (col1 TYPE constraints, col2 TYPE constraints, ...)
        /// </summary>
        private CreateTableQuery ParseCreateTable()
        {
            var query = new CreateTableQuery();

            Consume(TokenType.TABLE);
            query.TableName = Consume(TokenType.IDENTIFIER, "Expected table name").Value;

            Consume(TokenType.LEFT_PAREN, "Expected opening parenthesis");

            // Parse column definitions
            do
            {
                query.Columns.Add(ParseColumnDefinition());
            } while (Match(TokenType.COMMA));

            Consume(TokenType.RIGHT_PAREN, "Expected closing parenthesis");

            return query;
        }

        /// <summary>
        /// Parse column definition
        /// Grammar: column_name data_type [(length)] [PRIMARY KEY] [UNIQUE] [NOT NULL]
        /// </summary>
        private ColumnDefinition ParseColumnDefinition()
        {
            var column = new ColumnDefinition
            {
                Name = Consume(TokenType.IDENTIFIER, "Expected column name").Value
            };

            // Data type
            if (Check(TokenType.INT) || Check(TokenType.VARCHAR) || Check(TokenType.BOOLEAN) ||
                Check(TokenType.DATETIME) || Check(TokenType.DECIMAL))
            {
                column.DataType = _currentToken.Value;
                Advance();
            }
            else
            {
                throw new SqlSyntaxException($"Expected data type at position {_currentToken.Position}");
            }

            // VARCHAR length
            if (column.DataType.Equals("VARCHAR", StringComparison.OrdinalIgnoreCase) &&
                Match(TokenType.LEFT_PAREN))
            {
                var lengthToken = Consume(TokenType.NUMBER_LITERAL, "Expected length for VARCHAR");
                column.MaxLength = int.Parse(lengthToken.Value);
                Consume(TokenType.RIGHT_PAREN, "Expected closing parenthesis");
            }

            // Constraints
            while (true)
            {
                if (Match(TokenType.PRIMARY))
                {
                    Consume(TokenType.KEY, "Expected KEY after PRIMARY");
                    column.IsPrimaryKey = true;
                }
                else if (Match(TokenType.UNIQUE))
                {
                    column.IsUnique = true;
                }
                else if (Match(TokenType.NOT))
                {
                    Consume(TokenType.NULL, "Expected NULL after NOT");
                    column.IsNotNull = true;
                }
                else
                {
                    break; // No more constraints
                }
            }

            return column;
        }

        /// <summary>
        /// Parse CREATE INDEX
        /// Grammar: CREATE INDEX index_name ON table_name(column_name)
        /// </summary>
        private CreateIndexQuery ParseCreateIndex()
        {
            var query = new CreateIndexQuery();

            Consume(TokenType.INDEX);
            query.IndexName = Consume(TokenType.IDENTIFIER, "Expected index name").Value;

            Consume(TokenType.ON, "Expected ON keyword");
            query.TableName = Consume(TokenType.IDENTIFIER, "Expected table name").Value;

            Consume(TokenType.LEFT_PAREN, "Expected opening parenthesis");
            query.ColumnName = Consume(TokenType.IDENTIFIER, "Expected column name").Value;
            Consume(TokenType.RIGHT_PAREN, "Expected closing parenthesis");

            return query;
        }

        #endregion

        #region DROP Parser

        /// <summary>
        /// Parse DROP TABLE
        /// Grammar: DROP TABLE table_name
        /// </summary>
        private DropTableQuery ParseDrop()
        {
            var query = new DropTableQuery();

            Consume(TokenType.DROP);
            Consume(TokenType.TABLE, "Expected TABLE after DROP");
            query.TableName = Consume(TokenType.IDENTIFIER, "Expected table name").Value;

            return query;
        }

        #endregion

        #region WHERE Clause Parser

        /// <summary>
        /// Parse WHERE clause with AND/OR support
        /// Grammar: condition [AND condition] [OR condition]
        /// </summary>
        private WhereClause ParseWhereClause()
        {
            return ParseOrCondition();
        }

        /// <summary>
        /// Parse OR conditions (lower precedence)
        /// </summary>
        private WhereClause ParseOrCondition()
        {
            var left = ParseAndCondition();

            while (Match(TokenType.OR))
            {
                var right = ParseAndCondition();
                left = new WhereClause
                {
                    Type = ConditionType.Compound,
                    LogicalOp = LogicalOperator.OR,
                    Left = left,
                    Right = right
                };
            }

            return left;
        }

        /// <summary>
        /// Parse AND conditions (higher precedence)
        /// </summary>
        private WhereClause ParseAndCondition()
        {
            var left = ParseSimpleCondition();

            while (Match(TokenType.AND))
            {
                var right = ParseSimpleCondition();
                left = new WhereClause
                {
                    Type = ConditionType.Compound,
                    LogicalOp = LogicalOperator.AND,
                    Left = left,
                    Right = right
                };
            }

            return left;
        }

        /// <summary>
        /// Parse simple condition: column operator value
        /// Example: id = 1, name = 'John', age > 18
        /// </summary>
        private WhereClause ParseSimpleCondition()
        {
            var condition = new WhereClause { Type = ConditionType.Simple };

            // Left operand (column name, possibly qualified)
            condition.LeftOperand = Consume(TokenType.IDENTIFIER, "Expected column name").Value;

            // Check for table.column notation
            if (Match(TokenType.DOT))
            {
                condition.LeftOperand += "." + Consume(TokenType.IDENTIFIER, "Expected column name").Value;
            }

            // Operator
            if (Match(TokenType.EQUALS))
                condition.Operator = "=";
            else if (Match(TokenType.NOT_EQUALS))
                condition.Operator = "!=";
            else if (Match(TokenType.GREATER_THAN))
                condition.Operator = ">";
            else if (Match(TokenType.LESS_THAN))
                condition.Operator = "<";
            else if (Match(TokenType.GREATER_OR_EQUAL))
                condition.Operator = ">=";
            else if (Match(TokenType.LESS_OR_EQUAL))
                condition.Operator = "<=";
            else
                throw new SqlSyntaxException($"Expected comparison operator at position {_currentToken.Position}");

            // Right operand (value or column)
            condition.RightOperand = ParseValue();

            return condition;
        }

        #endregion

        #region JOIN Parser

        /// <summary>
        /// Parse JOIN clause
        /// Grammar: [INNER|LEFT|RIGHT|OUTER] JOIN table ON condition
        /// </summary>
        private JoinClause ParseJoin()
        {
            var join = new JoinClause();

            // Join type
            if (Match(TokenType.INNER))
                join.Type = JoinType.INNER;
            else if (Match(TokenType.LEFT))
                join.Type = JoinType.LEFT;
            else if (Match(TokenType.RIGHT))
                join.Type = JoinType.RIGHT;
            else if (Match(TokenType.OUTER))
                join.Type = JoinType.OUTER;
            else
                join.Type = JoinType.INNER; // Default

            Consume(TokenType.JOIN, "Expected JOIN keyword");
            join.TableName = Consume(TokenType.IDENTIFIER, "Expected table name").Value;

            Consume(TokenType.ON, "Expected ON keyword");
            join.Condition = ParseJoinCondition();

            return join;
        }

        /// <summary>
        /// Parse JOIN condition
        /// Grammar: table1.column1 = table2.column2
        /// </summary>
        private JoinCondition ParseJoinCondition()
        {
            var condition = new JoinCondition();

            // Left side: table.column
            condition.LeftTable = Consume(TokenType.IDENTIFIER, "Expected table name").Value;
            Consume(TokenType.DOT, "Expected dot in join condition");
            condition.LeftColumn = Consume(TokenType.IDENTIFIER, "Expected column name").Value;

            // Operator (usually =)
            if (Match(TokenType.EQUALS))
                condition.Operator = "=";
            else
                throw new SqlSyntaxException($"Expected = operator in join condition at position {_currentToken.Position}");

            // Right side: table.column
            condition.RightTable = Consume(TokenType.IDENTIFIER, "Expected table name").Value;
            Consume(TokenType.DOT, "Expected dot in join condition");
            condition.RightColumn = Consume(TokenType.IDENTIFIER, "Expected column name").Value;

            return condition;
        }

        #endregion

        #region ORDER BY Parser

        /// <summary>
        /// Parse ORDER BY clause
        /// Grammar: column [ASC|DESC], column [ASC|DESC], ...
        /// </summary>
        private List<OrderByClause> ParseOrderBy()
        {
            var orderByClauses = new List<OrderByClause>();

            do
            {
                var clause = new OrderByClause
                {
                    ColumnName = Consume(TokenType.IDENTIFIER, "Expected column name").Value
                };

                // Optional ASC/DESC
                if (_currentToken.Type == TokenType.IDENTIFIER)
                {
                    if (_currentToken.Value.Equals("ASC", StringComparison.OrdinalIgnoreCase))
                    {
                        clause.Order = SortOrder.ASC;
                        Advance();
                    }
                    else if (_currentToken.Value.Equals("DESC", StringComparison.OrdinalIgnoreCase))
                    {
                        clause.Order = SortOrder.DESC;
                        Advance();
                    }
                }

                orderByClauses.Add(clause);

            } while (Match(TokenType.COMMA));

            return orderByClauses;
        }

        #endregion

        #region Value Parser

        /// <summary>
        /// Parse a value literal or identifier
        /// </summary>
        private object ParseValue()
        {
            if (Check(TokenType.STRING_LITERAL))
            {
                var value = _currentToken.Value;
                Advance();
                return value;
            }

            if (Check(TokenType.NUMBER_LITERAL))
            {
                var value = _currentToken.Value;
                Advance();

                // Try to parse as int or decimal
                if (int.TryParse(value, out int intValue))
                    return intValue;
                if (decimal.TryParse(value, out decimal decimalValue))
                    return decimalValue;

                return value;
            }

            if (Check(TokenType.BOOLEAN_LITERAL))
            {
                var value = _currentToken.Value.Equals("TRUE", StringComparison.OrdinalIgnoreCase);
                Advance();
                return value;
            }

            if (Check(TokenType.NULL_LITERAL))
            {
                Advance();
                return null;
            }

            if (Check(TokenType.IDENTIFIER))
            {
                // Could be a column reference in WHERE clause
                var value = _currentToken.Value;
                Advance();
                return value;
            }

            throw new SqlSyntaxException($"Expected value at position {_currentToken.Position}");
        }

        #endregion
    }
}
