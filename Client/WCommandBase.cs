using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;

namespace HappyOracle.WellManagedDataAccess.Client
{
    /// <summary>
    /// Abstract base class for all Binders
    /// </summary>
    /// <remarks>
    /// <para>
    /// Sharad 8 Oct 2012: ActionName support: The static create functions determine the name of the calling method and pass it as the action name to the constructor.
    /// </para>
    /// </remarks>
    public abstract partial class WCommandBase
    {
        private readonly string _actionName;

        protected readonly BindParameterCollection _bindParameters;

        /// <summary>
        /// SqlBinder.Create() is recommended instead of using this cnstructor directly
        /// </summary>
        /// <param name="actionName"></param>
        protected WCommandBase()
        {
            _bindParameters = new BindParameterCollection();
        }

        public string ActionName { get; set; }

        public string CommandText { get; set; }

        ///// <summary>
        ///// People can enumerate parameters using this property.
        ///// This does not make the indexer available so that people are forced to use GetBindParameter
        ///// which provides better diagnostics
        ///// </summary>
        //internal IEnumerable<BindParameter> BindParameters
        //{
        //    get { return _bindParameters; }
        //}

        ///// <summary>
        ///// Provide proper diagnostics if parameter not found
        ///// </summary>
        ///// <param name="name"></param>
        ///// <returns></returns>
        //[Obsolete]
        //internal OracleParameter CreateOracleParameter(string name, OracleConnection conn)
        //{
        //    try
        //    {
        //        return _bindParameters[name].CreateOracleParameter(conn);
        //    }
        //    catch (KeyNotFoundException)
        //    {
        //        throw new KeyNotFoundException($"Parameter {name} has not been added to the binder");
        //    }
        //}

        /// <summary>
        /// By default, unspecified parameters raise an exception. Set this to true to assume that each unbound parameter is of type string with null value.
        /// </summary>
        public bool TolerateMissingParams { get; set; }

        /// <summary>
        /// Returns the parameter of the passed name. If it does not exist, creates the parameter
        /// </summary>
        /// <param name="field">Parameter name</param>
        /// <param name="dir">Bind Direction</param>
        /// <returns>Returns existing or newly created bind parameter</returns>
        /// <remarks>
        /// If it becomes necessary to create the parameter, only the direction and name are set. All other properties must be set by the caller.
        /// For existing parameters, the passed direction is merged with the existing direction. Thus if an input parameter is found, and the passed
        /// direction is output, then this parameter now becomes input/output.
        /// </remarks>
        protected BindParameter CreateOrUpdateParameter(string field, OracleDbType dbType, ParameterDirection dir)
        {
            BindParameter param;
            if (_bindParameters.Contains(field))
            {
                param = _bindParameters[field];

                // Make sure that it is of the same dbTtype
                if (param.OracleDbType != dbType)
                {
                    throw new ArgumentException($"Parameter {field} is of Type {param.OracleDbType}." +
                        $"Cannot add it again with type {dbType}");
                }
            }
            else
            {
                param = new BindParameter();
                param.ParameterName = field;
                param.Direction = dir;
                param.OracleDbType = dbType;
                _bindParameters.Add(param);
            }
            if (dir == ParameterDirection.ReturnValue || dir == ParameterDirection.InputOutput)
            {
                param.Direction = dir;
            }
            else if (param.Direction != dir)
            {
                switch (param.Direction)
                {
                    case ParameterDirection.Input:
                        if (dir == ParameterDirection.Output)
                        {
                            param.Direction = ParameterDirection.InputOutput;
                        }
                        break;

                    case ParameterDirection.InputOutput:
                        break;

                    case ParameterDirection.Output:
                        if (dir == ParameterDirection.Input)
                        {
                            param.Direction = ParameterDirection.InputOutput;
                        }
                        break;

                    case ParameterDirection.ReturnValue:
                        throw new NotSupportedException();

                    default:
                        throw new NotImplementedException();
                }
            }
            return param;
        }

        #region XML Query Parameters
        /// <summary>
        /// Bind string array parameter
        /// &lt;a pre="AND BKT.BUCKET_ID IN (" sep="," post=")" &gt;:BUCKETLIST &lt;/a &gt;
        /// </summary>
        /// <param name="field"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        /// <remarks>
        /// This parameter can only be used within the <![CDATA[<a>]]> tag of XML query as shown in the example.
        /// Using this parameter in any other context will result in severe exceptions.
        /// </remarks>
        /// <example>
        /// <code>
        /// <![CDATA[
        ///public int DeleteBols(IEnumerable<string> ShippingIdList)
        ///{
        ///    const string QUERY = @"
        ///    BEGIN
        ///            UPDATE <proxy />PS PS
        ///            SET PS.SHIPPING_ID =''
        ///            WHERE PS.SHIPPING_ID IN(<a sep=','>:SHIPPINGIDLIST</a>)
        ///            and PS.transfer_date is null;
        ///            DELETE FROM <proxy />SHIP S
        ///            WHERE S.SHIPPING_ID IN (<a sep=','>:SHIPPINGIDLIST</a>)
        ///            ;
        ///    END;
        ///    ";
        ///    var binder = SqlBinder.Create().Parameter("SHIPPINGIDLIST", ShippingIdList);
        ///    return _db.ExecuteDml(QUERY, binder);
        ///}
        /// ]]>
        /// </code>
        /// </example>
        public void ParameterRepeat(string field, IList<string> values)
        {
            var param = CreateOrUpdateParameter(field, OracleDbType.Varchar2, ParameterDirection.Input);
            if (values == null || !values.Any())
            {
                param.Value = DBNull.Value;
                param.XmlRepeatCount = 0;
            }
            else
            {
                param.XmlRepeatCount = values.Count;
                param.Value = "not null";
                for (var i = 0; i < values.Count; ++i)
                {
                    param = CreateOrUpdateParameter($"{field}{i}", OracleDbType.Varchar2, ParameterDirection.Input);
                    param.Value = values[i];
                }
            }
        }

        /// <summary>
        /// Bind an array of integer values.
        /// &lt;a pre="AND BKT.BUCKET_ID IN (" sep="," post=")" &gt;:BUCKETLIST &lt;/a &gt;
        /// </summary>
        /// <param name="field"></param>
        /// <param name="values"></param>
        /// <remarks>
        /// <para>
        /// array parameters are special handled to support XML array binding.
        /// </para>
        /// <example>
        /// <![CDATA[
        ///public void FreezeBuckets(IEnumerable<int> bucketList)
        ///{
        ///    var QUERY = @"
        ///        UPDATE <proxy />BUCKET BKT SET BKT.FREEZE = 'Y' 
        ///            WHERE BKT.FREEZE IS NULL
        ///            <a pre="AND BKT.BUCKET_ID IN (" sep="," post=")">:BUCKETLIST</a>
        ///    ";
        ///    var binder = new SqlBinder("Freeze Buckets");
        ///    binder.Parameter("BUCKETLIST", bucketList);
        ///    _db.ExecuteNonQuery(QUERY, binder);
        ///}
        /// ]]>
        /// </example>
        /// </remarks>
        public void ParameterRepeat(string field, IEnumerable<int> values)
        {
            var param = CreateOrUpdateParameter(field, OracleDbType.Int32, ParameterDirection.Input);
            //param.OracleDbType = OracleDbType.Int32;
            if (values == null)
            {
                param.Value = string.Empty;
            }
            else
            {
                param.Value = string.Join(",", values);
            }
            return;
        }

        ///// <summary>
        ///// Datatype is set to Int16. Value is set to null for false, 1 for true
        ///// </summary>
        ///// <param name="field"></param>
        ///// <param name="value"></param>
        ///// <returns></returns>
        ///// <remarks>
        ///// This parameter is primarily intended for XPath expressions. It is not very useful for binding to an Oracle query.
        ///// </remarks>
        //public void ParameterXPath(string field, bool value)
        //{
        //    var param = GetBindParameter(field, OracleDbType.Int16, ParameterDirection.Input);
        //    //param.OracleDbType = OracleDbType.Int16;
        //    param.Value = value ? (object)1 : null;
        //    return;
        //}
        #endregion

        #region In Parameters

        ///// <summary>
        ///// Bind an input string parameter.
        ///// </summary>
        ///// <param name="field">Parameter Name</param>
        ///// <param name="value">Parameter value</param>
        ///// <returns>The binder to enable fluent syntax</returns>
        ///// <remarks>
        ///// Oracle Doc says: When sending a null parameter value to the database, the user must specify DBNull, not null.
        ///// The null value in the system is an empty object that has no value. DBNull is used to represent null values.
        ///// The user can also specify a null value by setting Status to OracleParameterStatus.NullValue.
        ///// In this case, the provider sends a null value to the database.
        ///// </remarks>
        //[Obsolete]
        //protected void InParameterString(string field, string value)
        //{
        //    var param = GetBindParameter(field, OracleDbType.Varchar2, ParameterDirection.Input);
        //    param.Value = string.IsNullOrEmpty(value) ? DBNull.Value : (object)value;
        //}

        ///// <summary>
        ///// Bind a nullable integer parameter
        ///// </summary>
        ///// <param name="field"></param>
        ///// <param name="value"></param>
        ///// <returns></returns>
        //[Obsolete]
        //protected void InParameterInt(string field, int? value)
        //{
        //    var param = GetBindParameter(field, OracleDbType.Int32, ParameterDirection.Input);
        //    param.Value = value ?? (object)DBNull.Value;
        //    return;
        //}

        ///// <summary>
        ///// Bind a nullable date parameter
        ///// </summary>
        ///// <param name="field"></param>
        ///// <param name="value"></param>
        ///// <returns></returns>
        //[Obsolete]
        //protected void InParameterDateTime(string field, DateTime? value)
        //{
        //    var param = GetBindParameter(field, OracleDbType.Date, ParameterDirection.Input);
        //    param.Value = value ?? (object)DBNull.Value;
        //    return;
        //}

        protected void InParameterTimeStampTZ(string field, DateTimeOffset? value)
        {
            var param = CreateOrUpdateParameter(field, OracleDbType.TimeStampTZ, ParameterDirection.Input);
            if (value == null)
            {
                param.Value = DBNull.Value;
            }
            else
            {
                var tz = value.Value.Offset.ToString("c");
                param.Value = new OracleTimeStampTZ(value.Value.DateTime, tz);
            }
            return;
        }

        ///// <summary>
        ///// Bind a floating point parameter
        ///// </summary>
        ///// <param name="field"></param>
        ///// <param name="value"></param>
        ///// <returns></returns>
        //[Obsolete]
        //protected void InParameterDecimal(string field, decimal? value)
        //{
        //    var param = GetBindParameter(field, OracleDbType.Decimal, ParameterDirection.Input);
        //    param.Value = value ?? (object)DBNull.Value;
        //    return;
        //}

        //    [Obsolete]
        //protected void InParameterIntervalDS(string field, TimeSpan? value)
        //{
        //    var param = GetBindParameter(field, OracleDbType.IntervalDS, ParameterDirection.Input);
        //    param.Value = value ?? (object)DBNull.Value;
        //    return;
        //}
        #endregion

        #region Communicating with OracleDataStore

        //internal OracleParameter GetParameter(string name)
        //{
        //    try
        //    {
        //        var param = _bindParameters[name];
        //        return param.CreateOracleParameter();
        //    }
        //    catch (KeyNotFoundException ex)
        //    {
        //        if (this.TolerateMissingParams)
        //        {
        //            // Create a dummy parameter with null value now
        //            var param = new BindParameter();
        //            param.ParameterName = name;
        //            param.Size = 255;
        //            param.OracleDbType = OracleDbType.Varchar2;
        //            param.Direction = ParameterDirection.Input;
        //            _bindParameters.Add(param);
        //            return param.CreateOracleParameter();
        //        }
        //        throw new KeyNotFoundException(string.Format("Parameter {0} has not been bound", name), ex);
        //    }
        //}
        #endregion

        /// <summary>
        /// Opportunity to set binder specific OracleCommand Properties. Do not add parameters here
        /// </summary>
        /// <param name="cmd"></param>
        internal virtual void OnQueryExecuting(OracleCommand cmd)
        {
            cmd.BindByName = true;
            cmd.InitialLONGFetchSize = 1024;    // Retrieve first 1K chars from a long column
            cmd.InitialLOBFetchSize = 1024;
        }


    }
}
