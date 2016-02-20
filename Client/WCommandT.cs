using HappyOracle.WellManagedDataAccess.Helpers;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.ExceptionServices;

namespace HappyOracle.WellManagedDataAccess.Client
{
    /// <summary>
    /// Provides a way to convert results returned by a query into strongly typed objects
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <remarks>
    /// </remarks>
    public class WCommand<T> : WCommandBase
    {
        private readonly Func<WDataRow, T> _factory;

        #region Constructors
        internal WCommand(Func<WDataRow, T> factory)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }
            _factory = factory;
        }

        #endregion

        #region Parameter chaining
        /// <summary>
        /// String Parameter
        /// </summary>
        /// <param name="field"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <remarks>
        /// Sharad 14 Feb 2016: Unit Test SelectQuery_StringParameter
        /// </remarks>
        public WCommand<T> Parameter(string field, string value)
        {
            var param = CreateOrUpdateParameter(field, OracleDbType.Varchar2, ParameterDirection.Input);
            param.Value = string.IsNullOrEmpty(value) ? DBNull.Value : (object)value;
            return this;
        }

        /// <summary>
        /// Nullable integer parameter
        /// </summary>
        /// <param name="field"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <remarks>
        /// Sharad 14 Feb 2016: Unit Test SelectQuery_IntParameter, SelectQuery_IntParameterNullValue
        /// </remarks>
        public WCommand<T> Parameter(string field, int? value)
        {
            var param = CreateOrUpdateParameter(field, OracleDbType.Int32, ParameterDirection.Input);
            param.Value = value ?? (object)DBNull.Value;
            return this;
        }

        /// <summary>
        /// This overload is needed. Passing an int to the Parameter does not automatically choose the int?
        /// overload. Surprisingly, the comiler complains of ambiguity with the string overload.
        /// </summary>
        /// <param name="field"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <remarks>
        /// Sharad 14 Feb 2016: Unit Test SelectQuery_IntParameter
        /// </remarks>
        public WCommand<T> Parameter(string field, int value)
        {
            var param = CreateOrUpdateParameter(field, OracleDbType.Int32, ParameterDirection.Input);
            param.Value = value;
            return this;
        }

        /// <summary>
        /// DateTime parameter
        /// </summary>
        /// <param name="field"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <remarks>
        /// There is no overload for DateTime? because this one is automatically selected by the compiler
        /// when a DateTime value is passed
        /// 
        /// Sharad 14 Feb 2016 Unit Tests SelectQuery_DateParameterNullValue, SelectQuery_DateParameter
        /// </remarks>
        public WCommand<T> Parameter(string field, DateTime? value)
        {
            var param = CreateOrUpdateParameter(field, OracleDbType.Date, ParameterDirection.Input);
            param.Value = value ?? (object)DBNull.Value;
            return this;
        }

        //public SqlBinder<T> Parameter(string field, DateTime value)
        //{
        //    var param = GetBindParameter(field, OracleDbType.Date, ParameterDirection.Input);
        //    param.Value = value;
        //    return this;
        //}

        /// <summary>
        /// DateTimeOffset parameter
        /// </summary>
        /// <param name="field"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <remarks>
        /// Sharad 14 Feb 2016 Unit Test SelectQuery_DateTimeOffset
        /// </remarks>
        public WCommand<T> Parameter(string field, DateTimeOffset? value)
        {
            base.InParameterTimeStampTZ(field, value);
            return this;
        }

        /// <summary>
        /// decimal parameters
        /// </summary>
        /// <param name="field"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <remarks>
        /// Sharad 15 Feb 2016 Unit Test SelectQuery_DecimalParameter
        /// </remarks>
        public WCommand<T> Parameter(string field, decimal? value)
        {
            var param = CreateOrUpdateParameter(field, OracleDbType.Decimal, ParameterDirection.Input);
            param.Value = value ?? (object)DBNull.Value;
            return this;
        }

        /// <summary>
        /// IntervalDS parameter
        /// </summary>
        /// <param name="field"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public WCommand<T> Parameter(string field, TimeSpan? value)
        {
            var param = CreateOrUpdateParameter(field, OracleDbType.IntervalDS, ParameterDirection.Input);
            param.Value = value ?? (object)DBNull.Value;
            return this;
        }

        #endregion

        #region Factory

        ///// <summary>
        ///// After the reader is executed, this function is responsible for caling the factory for each row retrieved
        ///// </summary>
        ///// <param name="reader"></param>
        ///// <returns></returns>
        ///// <remarks>
        ///// Only in DEBUG mode, it ensures that all columns retrieved by the query have been accessed.
        ///// The finally block ensures that an exception has not been thrown and then proceeds to call <see cref="OracleDataRow.DebugCheck"/>.
        ///// http://stackoverflow.com/questions/2788793/how-to-get-the-current-exception-without-having-passing-the-variable
        ///// </remarks>
        //internal IEnumerable<T> MapRows(OracleDataReader reader)
        //{
        //    Debug.Assert(_factory != null);
        //    return DoMapRows(reader, _factory);
        //}

        /// <summary>
        /// After the reader is executed, this function is responsible for caling the factory for each row retrieved
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="factory"></param>
        /// <returns></returns>
        /// <remarks>
        /// Only in DEBUG mode, it ensures that all columns retrieved by the query have been accessed.
        /// The finally block ensures that an exception has not been thrown and then proceeds to call <see cref="OracleDataRow.DebugCheck"/>.
        /// http://stackoverflow.com/questions/2788793/how-to-get-the-current-exception-without-having-passing-the-variable
        /// </remarks>
        internal IEnumerable<T> MapRows(OracleDataReader reader)
        {
#if DEBUG
            var inException = false;
            EventHandler<FirstChanceExceptionEventArgs> x = (object s, FirstChanceExceptionEventArgs e) => inException = true;
#endif
            var row = new WDataRow(reader);
            try
            {
#if DEBUG
                AppDomain.CurrentDomain.FirstChanceException += x;
#endif
                while (reader.Read())
                {
                    //row.SetValues(reader);
                    yield return _factory(row);
                }
            }
            finally
            {
#if DEBUG
                if (!inException)
                {
                    row.DebugCheck();
                }
                AppDomain.CurrentDomain.FirstChanceException -= x;
#endif
            }
        }
        #endregion

        /// <summary>
        /// Executes the query and returns the first row as a strongly typed object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="xmlQuery"></param>
        /// <param name="binder"></param>
        /// <returns>The first row as a strongly typed object, or null if no row was found</returns>
        /// <remarks>
        /// <para>
        /// See <see cref="WConnection"/> for a code example.
        /// </para>
        /// </remarks>
        public T ExecuteSingle(WConnection conn)
        {
            OracleCommand cmd = null;
            OracleDataReader reader = null;
            try
            {
                cmd = _bindParameters.CreateOracleCommand(conn.Connection, CommandText);
                OnQueryExecuting(cmd);
                QueryLogging.TraceOracleCommand(conn.TraceContext, cmd, ActionName);
                reader = cmd.ExecuteReader(CommandBehavior.SingleRow);
                var result = MapRows(reader).FirstOrDefault();
                return result;
            }
            catch (OracleException ex)
            {
                conn.TraceContext?.Write($"Exception {ex}");

                throw WOracleException.Create(ex, cmd);
            }
            finally
            {
                if (reader != null)
                {
                    reader.Dispose();
                }
               OdpHelpers.DisposeCommand(cmd);
                QueryLogging.TraceQueryEnd(conn.TraceContext);
            }
        }

        /// <summary>
        /// Executes the query using the information in the passed <paramref name="binder"/>. When no rows are found, an empty list is returned.
        /// </summary>
        /// <typeparam name="T">The type of each row in the list of results</typeparam>
        /// <param name="xmlQuery"></param>
        /// <param name="binder">Information which describes the query to execute</param>
        /// <param name="maxRows">Maximum number of rows to retrieve. 0 means all.</param>
        /// <returns>A list of rows returned by the query</returns>
        /// <exception cref="ArgumentNullException"><paramref name="binder"/> is null</exception>
        /// <exception cref="OperationCanceledException">Number of rows retturned by query exceeded <see cref="DefaultMaxRows"/></exception>
        /// <exception cref="OracleException">Query execution error</exception>
        /// <remarks>
        /// <para>
        /// The connection is opened if it is not already open.
        /// </para>
        /// <para>
        /// Sharad 17 Nov 2011: Added new parameter <paramref name="maxRows"/>
        /// </para>
        /// </remarks>
        public IList<T> ExecuteReader(WConnection conn, int maxRows)
        {
            if (conn == null)
            {
                throw new ArgumentNullException("conn");
            }
            OracleCommand cmd = null;
            OracleDataReader reader = null;
            try
            {
                cmd = _bindParameters.CreateOracleCommand(conn.Connection, CommandText);
                //PrepareConnection(binder.ActionName);

                reader = cmd.ExecuteReader(maxRows == 1 ? CommandBehavior.SingleRow : CommandBehavior.Default);
                if (reader.RowSize > 0 && maxRows > 0)
                {
                    // Safety check. FetchSize optimization is applied only if RowSize is known and maxRows is specified
                    // Treat default Fetch Size specified in configuration as the maximum allowed.
                    // See whether we can reduce the fetch size to save memory
                    reader.FetchSize = Math.Min(reader.RowSize * maxRows, reader.FetchSize);
                }
                // Lazy loading will not work because the map context will not be available for mapping
                var results = MapRows(reader).Take(maxRows > 0 ? maxRows : conn.DefaultMaxRows).ToList();
                if (maxRows == 0 && reader.Read())
                {
                    // User did not specify maxRows and we did not read all of them. Generate error.
                    var msg = string.Format("Query aborted because more than {0} rows were retrieved",
                                            conn.DefaultMaxRows);
                    conn.TraceContext?.Write(msg);

                    throw new OperationCanceledException(msg);
                }
                conn.TraceContext?.Write($"ExecuteReader returned {results.Count} rows");
                return results;
            }
            catch (OracleException ex)
            {
                conn.TraceContext?.Write($"Exception {ex}");
                //if (ex.Number == OracleErrorNumber.PROXY_NOT_AUTHORIZED_TO_CONNECT)
                //{
                //    var userId = _builder.UserID;
                //    var proxyUser = _builder.ProxyUserId;
                //    var msg = string.Format("User '{0}' was not authenticated to connect as proxy user", userId) + "\n\n Excute following script to get rid of this error: \n\n" +
                //              string.Format("ALTER USER {0} GRANT CONNECT THROUGH {1};", userId, proxyUser);
                //    throw new AuthenticationException(msg);
                //}
                throw WOracleException.Create(ex, cmd);
            }
            catch (Exception ex)
            {
                if (cmd == null)
                {
                    throw;
                }
                // Include the query as part of the exception
                throw new Exception(cmd.CommandText, ex);
            }
            finally
            {
                if (reader != null)
                {
                    reader.Dispose();
                }
                OdpHelpers.DisposeCommand(cmd);
                QueryLogging.TraceQueryEnd(conn.TraceContext);
            }
        }

        public IList<T> ExecuteReader(WConnection conn)
        {
            return ExecuteReader(conn, 0);
        }
    }
}
