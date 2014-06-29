using System.Data.Common;
using System.Data.Entity.Infrastructure.Interception;
using System.Data.SqlClient;
using Utility.Logging;
using Utility.Logging.NLog;

namespace Eddard
{
    public class DbLogger : IDbCommandInterceptor
    {
        private ILogger _logger;

        public DbLogger()
        {
            var loggerFactory = new NLogLoggerFactory();
            _logger =  loggerFactory.GetLogger("sql");
            
        }

        public void NonQueryExecuting(
            DbCommand command, DbCommandInterceptionContext<int> interceptionContext)
        {
            LogIfNonAsync(command, interceptionContext);
        }

        public void NonQueryExecuted(
            DbCommand command, DbCommandInterceptionContext<int> interceptionContext)
        {
            LogIfError(command, interceptionContext);
        }

        public void ReaderExecuting(
            DbCommand command, DbCommandInterceptionContext<DbDataReader> interceptionContext)
        {
            LogIfNonAsync(command, interceptionContext);
        }

        public void ReaderExecuted(
            DbCommand command, DbCommandInterceptionContext<DbDataReader> interceptionContext)
        {
            LogIfError(command, interceptionContext);
        }

        public void ScalarExecuting(
            DbCommand command, DbCommandInterceptionContext<object> interceptionContext)
        {
            LogIfNonAsync(command, interceptionContext);
        }

        public void ScalarExecuted(
            DbCommand command, DbCommandInterceptionContext<object> interceptionContext)
        {
            LogIfError(command, interceptionContext);
        }

        private void LogIfNonAsync<TResult>(
            DbCommand command, DbCommandInterceptionContext<TResult> interceptionContext)
        {
            if (!interceptionContext.IsAsync)
            {
                string query = command.CommandText;
                foreach (SqlParameter p in command.Parameters)
                {
                    query = query.Replace(p.ParameterName, p.Value.ToString());
                }

                _logger.Info("Non-async command used: {0}", query);
                
            }
        }

        private void LogIfError<TResult>(
            DbCommand command, DbCommandInterceptionContext<TResult> interceptionContext)
        {
            if (interceptionContext.Exception != null)
            {
                string query = command.CommandText;
                foreach (SqlParameter p in command.Parameters)
                {
                    query = query.Replace(p.ParameterName, p.Value.ToString());
                }
                
                _logger.Error("Command {0} failed with exception {1}",
                    query, interceptionContext.Exception);
            }
        }
    }
}