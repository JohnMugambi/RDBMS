using RDBMS.Core.Models;
using RDBMS.Core.Parsing;
using RDBMS.Core.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDBMS.Core.Execution
{
    /// <summary>
    /// Main query executor - delegates to specific executors based on query type
    /// </summary>
    /// 
    public class QueryExecutor
    {
        private readonly StorageEngine _storage;
        private readonly SelectExecutor _selectExecutor;
        private readonly InsertExecutor _insertExecutor;
        private readonly UpdateExecutor _updateExecutor;
        private readonly DeleteExecutor _deleteExecutor;

        public QueryExecutor(StorageEngine storage)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _selectExecutor = new SelectExecutor(_storage);
            _insertExecutor = new InsertExecutor(_storage);
            _updateExecutor = new UpdateExecutor(_storage);
            _deleteExecutor = new DeleteExecutor(_storage);
        }

        /// <summary>
        /// Executes any query and returns result
        /// </summary>
        /// 
        public QueryResult Execute(IQuery query)
        {
            return query.Type switch
            {
                QueryType.SELECT => _selectExecutor.Execute((SelectQuery)query),
                QueryType.INSERT => _insertExecutor.Execute((InsertQuery)query),
                QueryType.UPDATE => _updateExecutor.Execute((UpdateQuery)query),
                QueryType.DELETE => _deleteExecutor.Execute((DeleteQuery)query),
                QueryType.CREATE_TABLE => ExecuteCreateTable((CreateTableQuery)query),
                QueryType.DROP_TABLE => ExecuteDropTable((DropTableQuery)query),
                QueryType.CREATE_INDEX => ExecuteCreateIndex((CreateIndexQuery)query),
                _ => throw new NotSupportedException($"Query type {query.Type} is not supported")
            };
        }

        #region DDL Operations (CREATE TABLE, DROP TABLE, CREATE INDEX)

        private QueryResult ExecuteCreateTable(CreateTableQuery query)
        {
            try
            {
                // Convert column definitions to Column objects
                var columns = new List<Column>();

                foreach (var colDef in query.Columns)
                {
                    var column = new Column
                    {
                        Name = colDef.Name,
                        Type = ParseDataType(colDef.DataType),
                        MaxLength = colDef.MaxLength,
                        IsPrimaryKey = colDef.IsPrimaryKey,
                        IsUnique = colDef.IsUnique,
                        IsNotNull = colDef.IsNotNull
                    };
                    columns.Add(column);
                }

                // Create table
                var table = new Table(query.TableName, columns);
                _storage.CreateTable(table);

                return new QueryResult
                {
                    Success = true,
                    Message = $"Table '{query.TableName}' created successfully",
                    RowsAffected = 0
                };

            }
            catch (Exception ex)
            {
                return new QueryResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        private QueryResult ExecuteDropTable(DropTableQuery query)
        {
            try
            {
                _storage.DropTable(query.TableName);

                return new QueryResult
                {
                    Success = true,
                    Message = $"Table '{query.TableName}' dropped successfully",
                    RowsAffected = 0
                };
            }
            catch (Exception ex)
            {
                return new QueryResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        private QueryResult ExecuteCreateIndex(CreateIndexQuery query)
        {
            try
            {
                // Use the storage engine's CreateIndex method - it handles everything
                _storage.CreateIndex(query.TableName, query.IndexName, query.ColumnName);

                return new QueryResult
                {
                    Success = true,
                    Message = $"Index '{query.IndexName}' created successfully",
                    RowsAffected = 0
                };
            }
            catch (TableNotFoundException ex)
            {
                return new QueryResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
            catch (StorageException ex)
            {
                return new QueryResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
            catch (Exception ex)
            {
                return new QueryResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        #endregion

        #region Helper Methods

        private DataType ParseDataType(string dataType)
        {
            return dataType.ToUpper() switch
            {
                "INT" => DataType.INT,
                "VARCHAR" => DataType.VARCHAR,
                "BOOLEAN" => DataType.BOOLEAN,
                "DATETIME" => DataType.DATETIME,
                "DECIMAL" => DataType.DECIMAL,
                _ => throw new NotSupportedException($"Data type '{dataType}' is not supported")
            };
        }
        #endregion
    }

    /// <summary>
    /// Result of query execution
    /// </summary>
    /// 
    public class QueryResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string ErrorMessage { get; set; }
        public int RowsAffected { get; set; }
        public List<Dictionary<string, object>> Data { get; set; } = new List<Dictionary<string, object>>();
        public List<string> ColumnNames { get; set; } = new List<string>();

        public override string ToString()
        {
            if (!Success)
                return $"Error: {ErrorMessage}";

            if (Data.Count > 0)
                return $"Success: {Data.Count} rows returned";

            return $"Success: {Message} ({RowsAffected} rows affected)";
        }
    }
}
