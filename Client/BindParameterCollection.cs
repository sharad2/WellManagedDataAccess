using HappyOracle.WellManagedDataAccess.XPath;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;

namespace HappyOracle.WellManagedDataAccess.Client
{
    public abstract partial class WCommandBase
    {
        /// <summary>
        /// Stores a list of BindParameters using case insensiive parameter names as the key
        /// </summary>
        protected class BindParameterCollection : KeyedCollection<string, BindParameter>
        {
            public BindParameterCollection()
                : base(StringComparer.InvariantCultureIgnoreCase, 4)
            {

            }
            protected override string GetKeyForItem(BindParameter item)
            {
                return item.ParameterName;
            }

            public OracleCommand CreateOracleCommand(OracleConnection conn, string sql)
            {
                if (conn == null)
                {
                    throw new ArgumentNullException(nameof(conn));
                }
                if (string.IsNullOrWhiteSpace(sql))
                {
                    throw new ArgumentNullException(nameof(sql));
                }
                //var query = binder.CommandText;
                if (sql.SkipWhile(p => char.IsWhiteSpace(p)).First() == '<')
                {
                    sql = XmlToSql.BuildQuery(sql,
                        this.ToDictionary(p => p.ParameterName, p => p.Value,
                            StringComparer.InvariantCultureIgnoreCase),
                        this.Where(p => p.XmlRepeatCount >= 0)
                        .ToDictionary(p => p.ParameterName, p => p.XmlRepeatCount,
                            StringComparer.InvariantCultureIgnoreCase)
                        );
                }

                var cmd = conn.CreateCommand();
                cmd.CommandText = sql;

                var paramsUsed = XmlToSql.GetParametersUsed(sql);

                foreach (var name in paramsUsed)
                {
                    BindParameter bindParam;
                    try
                    {
                        bindParam = this[name];
                    }
                    catch (KeyNotFoundException)
                    {
                        throw new KeyNotFoundException($"Parameter {name} has not been added to the command");
                    }

                    var param = new OracleParameter
                    {
                        ParameterName = bindParam.ParameterName,
                        Size = bindParam.Size,
                        OracleDbType = bindParam.OracleDbType,
                        Value = bindParam.Value,
                        Direction = bindParam.Direction,
                        CollectionType = bindParam.CollectionType,
                        ArrayBindSize = bindParam.ArrayBindSize,
                        ArrayBindStatus = bindParam.ArrayBindStatus
                    };

                    if (bindParam.OracleDbType == OracleDbType.Blob && bindParam.Value != null && bindParam.Value != DBNull.Value)
                    {
                        var blob = new OracleBlob(conn, false);
                        var bytes = (byte[])bindParam.Value;
                        blob.Write(bytes, 0, bytes.Length);
                        param.Value = blob;
                    }
                    else
                    {
                        param.Value = bindParam.Value;
                    }

                    cmd.Parameters.Add(param);
                }
                //binder.OnQueryExecuting(cmd);

                if (conn.State == ConnectionState.Closed)
                {
                    conn.Open();
                }
                //conn.ActionName = binder?.ActionName;
                //QueryLogging.TraceOracleCommand(_traceContext, cmd, binder?.ActionName);
                return cmd;
            }

        }

    }
}
