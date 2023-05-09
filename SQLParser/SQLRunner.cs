using Microsoft.SqlServer.TransactSql.ScriptDom;
using NLog;
using System;
using System.Collections.Generic;
#if NET6_0_OR_GREATER
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
using System.IO;
using System.Linq;
using System.Text;

namespace SQLParser
{
    internal class SQLRunner : IDisposable
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();
        private SqlConnection connection;
        private bool disposed = false;
        private IEnumerable<int> errorsCode = new List<int>();
        private readonly StringBuilder runQuerySql = new StringBuilder();
        private bool stopOnError = false;
        private string pathToFile;

        public SQLRunner(string connectionString, int commandTimeOut)
            : this(new SqlConnection(connectionString), commandTimeOut)
        { }

        public SQLRunner(SqlConnection connection, int commandTimeOut)
        {
            this.connection = connection;
            connection.FireInfoMessageEventOnUserErrors = true;
            connection.InfoMessage += SqlInfoMessageEventHandler;
            CommandTimeOut = commandTimeOut;
        }

        public int CommandTimeOut { get; protected set; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Connect()
        {
            if (connection.State != System.Data.ConnectionState.Open)
            {
                log.Info("connect to db");
                connection.Open();
                log.Info("get list system errors from database");
                errorsCode = GetErrorsCode(connection);
            }
        }

        public bool RunSQLFile(string pathToFile)
        {
            log.Info($"Run file {pathToFile}");
            this.pathToFile = pathToFile;
            log.Info($"parse file {pathToFile}");
            TSqlFragment fragments;
            using (var stream = new StreamReader(pathToFile, Encoding.UTF8))
            {
                var parser = new TSql110Parser(true);
                fragments = parser.Parse(stream, out IList<ParseError> errors);

                if (errors.Count > 0)
                {
                    var errorMessage = "=================================================" + Environment.NewLine;
                    foreach (var error in errors)
                    {
                        errorMessage += $"Line:{error.Line} Number {error.Number} Error {error.Message} {Environment.NewLine}";
                    }
                    errorMessage += $"== Script == {Environment.NewLine}";

                    var sql = new StringBuilder();
                    var exit = false;
                    foreach (var scriptToken in fragments.ScriptTokenStream)
                    {
                        if (scriptToken.TokenType == TSqlTokenType.Go)
                        {
                            if (exit) break;
                            sql.Clear();
                            continue;
                        }
                        sql.Append(scriptToken.Text);
                        if (scriptToken.Offset == errors[0].Offset)
                        {
                            exit = true;
                        }
                    }

                    errorMessage += sql.ToString() + Environment.NewLine + "=================================================" + Environment.NewLine;

                    log.Error(errorMessage);
                    return false;
                }
            }

            return ExecFragments(pathToFile, fragments, connection);

        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed) return;
            disposed = true;
            if (disposing && connection != null)
            {
                connection.InfoMessage -= SqlInfoMessageEventHandler;
                connection.Dispose();
                connection = null;
            }
        }

        protected virtual IEnumerable<int> GetErrorsCode(SqlConnection connection)
        {
            var result = new HashSet<int>();
            var sqlQueryCodeError = "select distinct error from master.dbo.sysmessages where severity > 10";

            using (var command = new SqlCommand(sqlQueryCodeError, connection))
            {
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(reader.GetInt32(0));
                    }
                }
            }
            result.Add(50000);
            return result;
        }

        protected virtual bool ExecFragments(string pathToFile, TSqlFragment fragments, SqlConnection connection)
        {
            log.Info($"Start exec file {pathToFile} on database");
            runQuerySql.Clear();
            foreach (var item in fragments.ScriptTokenStream)
            {
                if (item.TokenType == TSqlTokenType.MultilineComment ||
                    item.TokenType == TSqlTokenType.SingleLineComment)
                    continue;
                if (item.TokenType == TSqlTokenType.Go)
                {
                    if (!runQuerySql.IsOnlyWhiteSpace())
                    {
                        using (var command = new SqlCommand(runQuerySql.ToString(), connection))
                        {
                            command.CommandTimeout = CommandTimeOut;
                            command.ExecuteNonQuery();
                        }
                    }
                    runQuerySql.Clear();
                }
                else
                {
                    runQuerySql.Append(item.Text);
                }
                if (stopOnError) return false;
            }

            if (!runQuerySql.IsOnlyWhiteSpace())
            {
                using (var command = new SqlCommand(runQuerySql.ToString(), connection))
                {
                    command.CommandTimeout = CommandTimeOut;
                    command.ExecuteNonQuery();
                }
            }

            log.Info($"Finished exec file {pathToFile} on database");
            return !stopOnError;
        }

        private void SqlInfoMessageEventHandler(object sender, SqlInfoMessageEventArgs e)
        {
            var errors = e.Errors.OfType<SqlError>().Where(er => errorsCode.Contains(er.Number));
            if (errors.Any())
            {
                var message = "------------------";
                message += Environment.NewLine + pathToFile;

                message += Environment.NewLine + string.Join(Environment.NewLine, errors.Select(error => $"State {error.State} Number {error.Number} LineNumber {error.LineNumber}   {error.Message}")) + Environment.NewLine;
                message += "------------------";
                message += Environment.NewLine + runQuerySql.ToString() + Environment.NewLine;
                message += "------------------";
                log.Error(message);
                stopOnError = true;
            }
            else
            {
                log.Info(e.Message);
            }
        }
    }
}
