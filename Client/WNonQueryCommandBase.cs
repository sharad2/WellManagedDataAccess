using HappyOracle.WellManagedDataAccess.Helpers;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Data;
using System.Linq;

namespace HappyOracle.WellManagedDataAccess.Client
{
    /// <summary>
    /// This class exists to provide a common base for Array Binding and non array binding PL/SQL
    /// </summary>
    public class WNonQueryCommandBase : WCommandBase
    {
        /// <summary>
        /// Call the setters for each out parameter. Setter is not called if the out value is null.
        /// </summary>
        /// <param name="nRowsAffected">Return value of ExecuteDML</param>
        /// <param name="parameters">List of oracle parameters</param>
        internal void OnQueryExecuted(ITraceContext ctx, OracleParameterCollection parameters)
        {
            var x = from OracleParameter p in parameters
                    where _bindParameters.Contains(p.ParameterName)
                    let setter = _bindParameters[p.ParameterName].OutputValueUpdater
                    where setter != null
                    select p;
            // For array parameters, _bindParameters contains "BUCKETLIST" whereas parameters contains "BUCKETLIST0"
            // For this reason we cannot expect all names in parameters to be available in _bindParameters
            // Thus we are including the _bindParameters.Contains() condition
            var query = from OracleParameter p in parameters
                        where _bindParameters.Contains(p.ParameterName)
                        let setter = _bindParameters[p.ParameterName].OutputValueUpdater
                        where setter != null
                        select new
                        {
                            Value = p.Value,
                            Setter = setter
                        };
            foreach (var item in query)
            {
                item.Setter(item.Value);
            }
            if (ctx != null)
            {
                var outParams = from OracleParameter p in parameters
                                where p.Direction == ParameterDirection.InputOutput || p.Direction == ParameterDirection.Output
                                select p;

                if (outParams.Any())
                {
                    ctx.Write("Output Parameter Values");
                    QueryLogging.TraceParameters(ctx, outParams);
                }
            }
        }

        public int ExecuteNonQuery(WConnection conn)
        {
            if (conn == null)
            {
                throw new ArgumentNullException(nameof(conn));
            }
            OracleCommand cmd = null;
            try
            {
                cmd = _bindParameters.CreateOracleCommand(conn.Connection, this.CommandText);
                conn.Connection.ActionName = ActionName;
                OnQueryExecuting(cmd);
                QueryLogging.TraceOracleCommand(conn.TraceContext, cmd, ActionName);
                var rowsAffected = cmd.ExecuteNonQuery();
                OnQueryExecuted(conn.TraceContext, cmd.Parameters);
                return rowsAffected;
            }
            catch (OracleException ex)
            {
                conn.TraceContext?.Write($"Exception {ex}");
                throw WOracleException.Create(ex, cmd);
            }
            finally
            {
                OdpHelpers.DisposeCommand(cmd);
                QueryLogging.TraceQueryEnd(conn.TraceContext);
            }
        }

    }
}
