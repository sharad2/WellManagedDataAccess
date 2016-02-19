using HappyOracle.WellManagedDataAccess.Helpers;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Data;
using System.Data.Common;
using System.Text.RegularExpressions;

namespace HappyOracle.WellManagedDataAccess.Client
{
    /// <summary>
    /// Use this class to conveniently execute queries against Oracle database
    /// </summary>
    /// <remarks>
    /// <para>
    /// You must explicitly create the connection using <see cref="CreateConnection"/>.
    /// Proxy authentication is supported.
    /// Query results are returned as strongly typed classes by ExecuteSingle() and ExecuteReader().
    /// </para>
    /// <para>
    /// Retrieving only the first column. In this situation, you can create a binder for a scalar type and not specify any mapping.
    /// </para>
    /// <para>
    /// Sharad 7 Jun 2011: New properties <see cref="ClientInfo"/> and <see cref="ModuleName"/> added.
    /// </para>
    /// <para>
    /// Sharad 12 Aug 2011: Added support for transactions <see cref="BeginTransaction"/>.
    /// </para>
    /// <para>
    /// Sharad 20 Aug: Adding query info to all OracleExceptions. The custom page of DcmsMobile is capable of printing this extra info.
    /// </para>
    /// </remarks>
    /// <example>
    /// <para>
    /// The following is an example of executing a typical query in a repository function.
    /// </para>
    /// <code>
    /// <![CDATA[
    ///internal IList<Box> GetArchivePickslipBoxes(int pickslipId)
    ///{
    ///    const string QUERY_ArchivePickslipBoxes = @"
    ///        SELECT MAX(DEM_BOX.UCC128_ID) AS UCC128_ID,
    ///            COUNT(*) AS SKU_IN_BOX,
    ///            NULL AS AREA,
    ///            SUM(DEM_PSDET.QUANTITY_ORDERED) AS CURRENT_PIECES,
    ///            SUM(DEM_PSDET.QUANTITY_ORDERED) AS EXPECTED_PIECES
    ///    FROM DEM_PICKSLIP_DETAIL_H DEM_PSDET
    ///    INNER JOIN DEM_BOX_H DEM_BOX
    ///        ON DEM_PSDET.PICKSLIP_ID = DEM_BOX.CHECKING_ID
    ///    WHERE DEM_PSDET.PICKSLIP_ID = :PICKSLIP_ID
    ///    GROUP BY DEM_PSDET.STYLE,
    ///            DEM_PSDET.COLOR,
    ///            DEM_PSDET.DIMENSION,
    ///            DEM_PSDET.SKU_SIZE
    ///        ";
    ///        
    ///    var binder = new SqlBinder<Box>("GetArchivePickslipBoxes");
    ///    binder.CreateMapper(QUERY_ArchivePickslipBoxes, config =>
    ///    {
    ///        config.CreateMap<Box>()
    ///            .MapField("UCC128_ID", dest => dest.Ucc128Id)
    ///            .MapField("AREA", dest => dest.Area)
    ///            .MapField("EXPECTED_PIECES", dest => dest.ExpectedPieces)
    ///            .MapField("CURRENT_PIECES", dest => dest.CurrentPieces)
    ///            .MapField("SKU_IN_BOX", dest => dest.CountSku)
    ///            ;
    ///    });
    ///    binder.Parameter("PICKSLIP_ID", pickslipId);
    ///    var result = _db.ExecuteReader(binder);
    ///    return result;
    ///}
    /// ]]>
    /// </code>
    /// </example>
    public class WConnection : IDisposable
    {
        #region Construction and Destruction

        private readonly ITraceContext _traceContext;

        private readonly OracleConnection _conn;

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// </remarks>
        public WConnection(ITraceContext traceContext)
        {
            _traceContext = traceContext;

            _conn = new OracleConnection();

            _conn.StateChange += Connection_StateChange;
        }

        public WConnection(string connectStr)
        {
            _conn = new OracleConnection();

            _conn.StateChange += Connection_StateChange;

            CreateConnection(connectStr);
        }

        public int DefaultMaxRows { get; set; } = 100;

        public ITraceContext TraceContext { get; }

        /// <summary>
        /// Disposes the connection
        /// </summary>
        public void Dispose()
        {
            _conn.Dispose();
        }
        #endregion

        #region Connection

        /// <summary>
        /// For use by the ProxyTagResolver
        /// </summary>
        private OracleConnectionStringBuilder _builder;

        /// <summary>
        /// The value which will replace <proxy/> tag in queries, e.g. dcms8.
        /// </summary>
        private string _proxyTagValue;

        /// <summary>
        /// Creates a connection on behalf of the passed <paramref name="userId"/>.
        /// </summary>
        /// <param name="connectString"></param>
        /// <param name="userId"></param>
        /// <remarks>
        /// <para>
        /// If you are not using proxy authentication, then the <paramref name="connectString"/> is used as is and <paramref name="userId"/>
        /// is ignored.
        /// </para>
        /// <para>
        /// The normal case is that the passed <paramref name="connectString"/> contains the <c>ProxyUserId</c> and
        /// <c>ProxyPassword</c> attributes. The passed userId is simply set as the <c>UserId</c> attribute of the connect string.
        /// </para>
        /// <para>
        /// If the user is not authenticated, <paramref name="userId"/> will be null. In this case the passed <paramref name="connectString"/> is modified
        /// to make the ProxyUserId as the real user id. This connection will therefore not use a proxy.
        /// </para>
        /// </remarks>
        public void CreateConnection(string connectString, string userId)
        {
            _builder = new OracleConnectionStringBuilder(connectString);
            if (string.IsNullOrEmpty(_builder.ProxyUserId))
            {
                // Proxy not being used. Nothing to do. Ignore userId. Use connect string as is.
            }
            else if (string.IsNullOrEmpty(userId) || string.Compare(_builder.ProxyUserId, userId, true) == 0)
            {
                // Anonymous user wants to execute a query
                // Special case: If userId is same as proxy user, Create a direct connection for the passed user.
                // This prevents the unreasonable error "dcms4 not allowed to connect on behalf of dcms4".
                // Treat the proxy as the real user and remove the proxy attributes.
                _builder.UserID = _builder.ProxyUserId;
                _builder.Password = _builder.ProxyPassword;
                _builder.ProxyUserId = string.Empty;
                _builder.ProxyPassword = string.Empty;
            }
            else
            {
                // Proxy is being used.
                _builder.UserID = userId;
            }

            if (_builder.PersistSecurityInfo)
            {
                // Is null during unit tests
                _traceContext?.Write(
                    "Info: The connection string specifies PersistSecurityInfo=true. This setting is not recommended and is not necessary.");
                _builder.PersistSecurityInfo = false;
            }

            if (_conn.State != ConnectionState.Closed)
            {
                _conn.Close();
            }
            _conn.ConnectionString = _builder.ConnectionString; // factory.CreateConnection();
            //_proxyUserId = (_builder.ProxyUserId ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(_builder.ProxyUserId))
            {
                _proxyTagValue = null;
            }
            else
            {
                _proxyTagValue = _builder.ProxyUserId.Trim() + ".";
            }
        }

        public void CreateConnection(string connectString)
        {
            CreateConnection(connectString, null);
        }

        /// <summary>
        /// Called whenever the connection is opened. Set the module and client info
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Connection_StateChange(object sender, StateChangeEventArgs e)
        {
            if (e.CurrentState == ConnectionState.Open)
            {
                var conn = (OracleConnection)sender;
                conn.ModuleName = this.ModuleName;
                conn.ClientInfo = this.ClientInfo;
                if (_traceContext != null)
                {
                    //var msg = string.Format("Connection opened at {0:F}. ModuleName: {1}; ClientInfo: {2}", DateTimeOffset.Now, this.ModuleName, this.ClientInfo);
                    _traceContext?.Write($"Connection opened. ModuleName: {ModuleName}; ClientInfo: {ClientInfo}");
                    //Encrypt the password with '****'
                    _traceContext?.Write("Connection String " +
                        Regex.Replace(conn.ConnectionString, @"(\bPASSWORD=\S*;\b)", "PASSWORD=****;"));
                }
            }
        }

        /// <summary>
        /// The connection to be used for executing queries
        /// </summary>
        /// <remarks>
        /// <para>
        /// The connection can be created using the <see cref="CreateConnection"/> method.
        /// </para>
        /// </remarks>
        public OracleConnection Connection
        {
            get
            {
                // This may be closed but it will not be null
                return _conn;
            }
        }

        private string _moduleName;

        /// <summary>
        /// Set via DBMS_APPLICATION_INFO. Max 48 characters. If it is too long, we chop off the initial characters.
        /// </summary>
        /// <remarks>
        /// You should set this property immediately after creating the connection. It will be passed along with each query which will
        /// be executed against this connection.
        /// </remarks>
        public string ModuleName
        {
            get
            {
                return _moduleName;
            }
            set
            {
                if (_moduleName != value)
                {
                    _moduleName = value.Substring(Math.Max(value.Length - 48, 0));
                }
            }
        }

        private string _clientInfo;
        /// <summary>
        /// Set via DBMS_APPLICATION_INFO.
        /// </summary>
        /// <remarks>
        /// You should set this property immediately after creating the connection. It will be passed along with each query which will
        /// be executed against this connection.
        /// Max 64 characters
        /// </remarks>
        public string ClientInfo
        {
            get
            {
                return _clientInfo;
            }
            set
            {
                if (_clientInfo != value)
                {
                    _clientInfo = value.Substring(Math.Max(value.Length - 64, 0));
                }
            }
        }

        #endregion

        #region Transactions
        /// <summary>
        /// This is the only function needed to create a transaction
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// The commit and rollback methods are available on the returned transaction object.
        /// If you forget to commit, none of your changes will be saved. This actually is helpful behavior because we do not have to worry about
        /// rolling back in case of an exception.
        /// </remarks>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using (var trans = _db.BeginTransaction()) {
        ///   foreach (var dispos in dispositions)
        ///   {
        ///       _db.ExecuteNonQuery(...);
        ///   }
        ///   trans.Commit();
        /// }
        /// ]]>
        /// </code>
        /// </example>
        public DbTransaction BeginTransaction(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            //Contract.Assert(_conn != null, "Connection must be created");
            if (_conn.State == ConnectionState.Closed)
            {
                _conn.Open();
            }
            return _conn.BeginTransaction();
        }
        #endregion

        #region Public Sql Execution Functions

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
        public int ExecuteNonQuery(string query)
        {
            OracleCommand cmd = null;
            try
            {
                cmd = _conn.CreateCommand();
                cmd.CommandText = query;

                if (_conn.State == ConnectionState.Closed)
                {
                    _conn.Open();
                }
                QueryLogging.TraceOracleCommand(_traceContext, cmd, "");
                return cmd.ExecuteNonQuery();
            }
            catch (OracleException ex)
            {
                _traceContext?.Write($"Exception {ex}");
                throw WOracleException.Create(ex, cmd);
            }
            finally
            {
                OdpHelpers.DisposeCommand(cmd);
                QueryLogging.TraceQueryEnd(_traceContext);
            }
        }

        //[Obsolete]
        //public int ExecuteNonQuery(HOracleCommandNonQueryBase binder)
        //{
        //    if (binder == null)
        //    {
        //        throw new ArgumentNullException(nameof(binder));
        //    }
        //    OracleCommand cmd = null;
        //    try
        //    {
        //        cmd = CreateCommand(binder);

        //        var rowsAffected = cmd.ExecuteNonQuery();
        //        binder.OnQueryExecuted(_traceContext, cmd.Parameters);
        //        return rowsAffected;
        //    }
        //    catch (OracleException ex)
        //    {
        //        _traceContext?.Write($"Exception {ex}");
        //        throw OracleDataStoreException.Create(ex, cmd);
        //    }
        //    finally
        //    {
        //        DisposeCommand(cmd);
        //        QueryLogging.TraceQueryEnd(_traceContext);
        //    }
        //}

        ///// <summary>
        ///// Executes the query and returns the first row as a strongly typed object.
        ///// </summary>
        ///// <typeparam name="T"></typeparam>
        ///// <param name="xmlQuery"></param>
        ///// <param name="binder"></param>
        ///// <returns>The first row as a strongly typed object, or null if no row was found</returns>
        ///// <remarks>
        ///// <para>
        ///// See <see cref="HOracleConnection"/> for a code example.
        ///// </para>
        ///// </remarks>
        //[Obsolete]
        //public T ExecuteSingle<T>(HOracleCommand<T> binder)
        //{
        //    Contract.Assert(binder != null);
        //    OracleCommand cmd = null;
        //    OracleDataReader reader = null;
        //    try
        //    {
        //        cmd = CreateCommand(binder);
        //        reader = cmd.ExecuteReader(CommandBehavior.SingleRow);
        //        var result = binder.MapRows(reader).FirstOrDefault();
        //        return result;
        //    }
        //    catch (OracleException ex)
        //    {
        //        _traceContext?.Write($"Exception {ex}");

        //        throw OracleDataStoreException.Create(ex, cmd);
        //    }
        //    finally
        //    {
        //        if (reader != null)
        //        {
        //            reader.Dispose();
        //        }
        //        if (cmd != null)
        //        {
        //            foreach (OracleParameter parameter in cmd.Parameters)
        //            {
        //                parameter.Dispose();
        //            }
        //            cmd.Dispose();
        //        }
        //        QueryLogging.TraceQueryEnd(_traceContext);
        //    }
        //}


        //[Obsolete]
        //public IList<T> ExecuteReader<T>(HOracleCommand<T> binder)
        //{
        //    return ExecuteReader(binder, 0);
        //}


        ///// <summary>
        ///// Executes the query using the information in the passed <paramref name="binder"/>. When no rows are found, an empty list is returned.
        ///// </summary>
        ///// <typeparam name="T">The type of each row in the list of results</typeparam>
        ///// <param name="xmlQuery"></param>
        ///// <param name="binder">Information which describes the query to execute</param>
        ///// <param name="maxRows">Maximum number of rows to retrieve. 0 means all.</param>
        ///// <returns>A list of rows returned by the query</returns>
        ///// <exception cref="ArgumentNullException"><paramref name="binder"/> is null</exception>
        ///// <exception cref="OperationCanceledException">Number of rows retturned by query exceeded <see cref="DefaultMaxRows"/></exception>
        ///// <exception cref="OracleException">Query execution error</exception>
        ///// <remarks>
        ///// <para>
        ///// The connection is opened if it is not already open.
        ///// </para>
        ///// <para>
        ///// Sharad 17 Nov 2011: Added new parameter <paramref name="maxRows"/>
        ///// </para>
        ///// </remarks>
        //[Obsolete]
        //public IList<T> ExecuteReader<T>(HOracleCommand<T> binder, int maxRows)
        //{
        //    if (binder == null)
        //    {
        //        throw new ArgumentNullException("binder");
        //    }
        //    OracleCommand cmd = null;
        //    OracleDataReader reader = null;
        //    try
        //    {
        //        cmd = CreateCommand(binder);
        //        //PrepareConnection(binder.ActionName);

        //        reader = cmd.ExecuteReader(maxRows == 1 ? CommandBehavior.SingleRow : CommandBehavior.Default);
        //        if (reader.RowSize > 0 && maxRows > 0)
        //        {
        //            // Safety check. FetchSize optimization is applied only if RowSize is known and maxRows is specified
        //            // Treat default Fetch Size specified in configuration as the maximum allowed.
        //            // See whether we can reduce the fetch size to save memory
        //            reader.FetchSize = Math.Min(reader.RowSize * maxRows, reader.FetchSize);
        //        }
        //        // Lazy loading will not work because the map context will not be available for mapping
        //        //var rowcount = 0;
        //        //foreach (var result in from object row in reader select binder.Mapper.Engine.Map<IOracleDataRow, T>(dict))
        //        var results = binder.MapRows(reader).Take(maxRows > 0 ? maxRows : this.DefaultMaxRows).ToList();
        //        if (maxRows == 0 && reader.Read())
        //        {
        //            // User did not specify maxRows and we did not read all of them. Generate error.
        //            var msg = string.Format("Query aborted because more than {0} rows were retrieved",
        //                                    this.DefaultMaxRows);
        //            _traceContext?.Write(msg);

        //            throw new OperationCanceledException(msg);
        //        }
        //        _traceContext?.Write($"ExecuteReader returned {results.Count} rows");

        //        //var b = reader.NextResult();
        //        return results;
        //    }
        //    catch (OracleException ex)
        //    {
        //        _traceContext?.Write($"Exception {ex}");
        //        if (ex.Number == OracleErrorNumber.PROXY_NOT_AUTHORIZED_TO_CONNECT)
        //        {
        //            var userId = _builder.UserID;
        //            var proxyUser = _builder.ProxyUserId;
        //            var msg = string.Format("User '{0}' was not authenticated to connect as proxy user", userId) + "\n\n Excute following script to get rid of this error: \n\n" +
        //                      string.Format("ALTER USER {0} GRANT CONNECT THROUGH {1};", userId, proxyUser);
        //            throw new AuthenticationException(msg);
        //        }
        //        throw OracleDataStoreException.Create(ex, cmd);
        //    }
        //    catch (Exception ex)
        //    {
        //        if (cmd == null)
        //        {
        //            throw;
        //        }
        //        // Include the query as part of the exception
        //        throw new Exception(cmd.CommandText, ex);
        //    }
        //    finally
        //    {
        //        if (reader != null)
        //        {
        //            reader.Dispose();
        //        }
        //        OdpHelpers.DisposeCommand(cmd);
        //        QueryLogging.TraceQueryEnd(_traceContext);
        //    }

        //}

        //[Obsolete]
        //private void DisposeCommand(OracleCommand cmd)
        //{
        //    if (cmd == null)
        //    {
        //        return;
        //    }
        //    foreach (OracleParameter parameter in cmd.Parameters)
        //    {
        //        var dis = parameter.Value as IDisposable;
        //        if (dis != null)
        //        {
        //            // We expect to get here for BLOB parameters
        //            dis.Dispose();
        //        }
        //        parameter.Dispose();
        //    }
        //    cmd.Dispose();
        //}

        ///// <summary>
        ///// If there are no parameters to bind, pass a null binder
        ///// </summary>
        ///// <param name="query"></param>
        ///// <param name="binder"></param>
        ///// <returns></returns>
        ///// 
        //[Obsolete]
        //private OracleCommand CreateCommand(HOracleCommandBase binder)
        //{
        //    if (binder == null)
        //    {
        //        throw new ArgumentNullException(nameof(binder));
        //    }
        //    var query = binder.CommandText;
        //    if (query.SkipWhile(p => char.IsWhiteSpace(p)).First() == '<')
        //    {
        //        query = XmlToSql.BuildQuery(query,
        //            binder.BindParameters
        //            .ToDictionary(p => p.ParameterName, p => p.Value,
        //                StringComparer.InvariantCultureIgnoreCase),
        //            binder.BindParameters
        //            .Where(p => p.XmlRepeatCount >= 0)
        //            .ToDictionary(p => p.ParameterName, p => p.XmlRepeatCount,
        //                StringComparer.InvariantCultureIgnoreCase)
        //            );
        //    }

        //    var cmd = _conn.CreateCommand();
        //    cmd.CommandText = query;
        //    // Null binder means no parameters
        //    var paramsUsed = XmlToSql.GetParametersUsed(query);

        //    foreach (var name in paramsUsed)
        //    {
        //        cmd.Parameters.Add(binder.CreateOracleParameter(name, _conn));
        //    }
        //    binder.OnQueryExecuting(cmd);

        //    if (_conn.State == ConnectionState.Closed)
        //    {
        //        _conn.Open();
        //    }
        //    _conn.ActionName = binder?.ActionName;
        //    QueryLogging.TraceOracleCommand(_traceContext, cmd, binder?.ActionName);
        //    return cmd;
        //}


        #endregion


    }
}
