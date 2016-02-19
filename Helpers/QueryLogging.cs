
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

namespace HappyOracle.WellManagedDataAccess.Helpers
{
    /// <summary>
    /// Provides useful functions for logging queries. I wanted to make this class internal, but it is being used by EclipseLibrary.WebForms
    /// </summary>
    public static class QueryLogging
    {
        [Obsolete("Use the overload which accepts actionName")]
        public static void TraceOracleCommand(ITraceContext ctx, DbCommand cmd)
        {
            TraceOracleCommand(ctx, cmd, "Unspecified Action");
        }
        /// <summary>
        /// Designed for MVC applications. They pass the controller trace context
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="cmd"></param>
        /// <param name="actionName"></param>
        public static void TraceOracleCommand(ITraceContext ctx, DbCommand cmd, string actionName)
        {
            //if (ctx == null)
            //{
            //    // This can happen during unit tests. Do nothing.
            //    return;
            //}
            ctx?.Write("");
            ctx?.Write($"----------QueryBegin for Action {actionName}");
            // Make sure that PersistSecurityInfo has not been set to true in the connection string. Otherwise, the password will be traced as well.
            // OracleDatastore takes the responsibility of explicitly setting PersistSecurityInfo=false.
            //ctx.Write("Connection String", cmd.Connection.ConnectionString);
            //string str = string.Format("QueryText [{0}] {1}", cmd.CommandType, cmd.CommandText.Trim());
            ctx?.Write($"[{cmd.CommandType}] { cmd.CommandText.Trim()}");
            TraceParameters(ctx, cmd.Parameters.Cast<OracleParameter>());
            return;
        }

        public static void TraceParameters(ITraceContext ctx, IEnumerable<OracleParameter> parameters)
        {
            if (ctx == null)
            {
                return;
            }
            string str;
            foreach (var param in parameters)
            {
                //ctx.Write("QueryParameter");
                if (param.Value == null)
                {
                    str = string.Format("Parameter {0} -> null (null {1}) {2})",
                        param.ParameterName, param.DbType, param.Direction);
                }
                else if (param.CollectionType == OracleCollectionType.PLSQLAssociativeArray)
                {
                    var x = string.Join(",", ((IEnumerable) param.Value).Cast<object>().Select(p => p ?? (object)"(null"));
                    str = string.Format("Parameter {0} -> {1} ({2} {3}) {4})",
                        param.ParameterName, x, param.Value.GetType(), param.DbType, param.Direction);
                }
                else
                {
                    str = string.Format("Parameter {0} -> {1} ({2} {3}) {4})",
                        param.ParameterName, param.Value, param.Value.GetType(), param.DbType, param.Direction);
                }
                ctx.Write(str);
            }
        }

        /// <summary>
        /// Call this after query has finished executing to trace the query end time.
        /// Writes to Diagnostics if ctx is null
        /// </summary>
        /// <param name="ctx"></param>
        public static void TraceQueryEnd(ITraceContext ctx)
        {
            //if (ctx == null)
            //{
            //    //System.Diagnostics.Trace.WriteLine(DateTime.Now.ToString("F"), "QueryEnd");
            //}
            //else
            //{
                ctx?.Write("----------QueryEnd");
                //ctx.Write("");
            //}
        }

    }
}
