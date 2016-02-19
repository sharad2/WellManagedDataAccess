using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace HappyOracle.WellManagedDataAccess.Client
{
    /// <summary>
    /// This serves as the class to use for parameter binding for PL/SQL blocks executed by <see cref="WConnection"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="WNonQueryCommand"/> can be passed as a parameter to the functions <see cref="WConnection.ExecuteNonQuery"/>.
    /// </para>
    /// <para>
    /// RefCursor support. See <see cref="OutRefCursorParameter"/>
    /// </para>
    /// </remarks>
    /// <example>
    /// <para>
    /// Following is a simple use of how to pass parameters to <see cref="WConnection.ExecuteNonQuery"/>.
    /// The overloaded Parameter functions are used to specify the value of the parameter.
    /// </para>
    /// <code lang="c#">
    /// <![CDATA[
    ///public static void RemoveFromPallet(OracleDatabase db, string uccId)
    ///{
    ///    string QUERY = @"update <proxy />box  b
    ///set b.ia_id = (
    ///select i.ia_id from <proxy />iaconfig i where i.iaconfig_id = '$BADVERIFY'
    ///)
    ///where b.ucc128_id= :UccId";
    ///    var binder = SqlBinder.Create();
    ///    binder.Parameter("UccId", uccId);
    ///    db.ExecuteNonQuery(QUERY, binder);
    ///}
    /// ]]>
    /// </code>
    /// </example>
    public partial class WNonQueryCommand : WNonQueryCommandBase
    {
        #region Constructors

        /// <summary>
        /// SqlBinder.Create() is recommended instead of using this constructor directly
        /// </summary>
        /// <param name="actionName"></param>
        /// <remarks>
        /// This should be made private after warnings have been removed from applications.
        /// </remarks>
        public WNonQueryCommand()
        {
        }
        #endregion

        #region Ref Cursor

        /// <summary>
        /// The parameter returns a ref cursor. Specify a factory which will convert the returned data to strongly typed objects
        /// </summary>
        /// <param name="field"></param>
        /// <param name="factory"></param>
        /// <remarks>
        /// <code>
        /// <![CDATA[
        ///        IList<int?> list = null;
        ///        var cmd = HOracleCommand.Create()
        ///            .OutRefCursorParameter("out_cursor", rows => list = rows.Select(p => p.GetInteger(0)).ToList());
        ///        __db.ExecuteNonQuery(@"
        ///BEGIN
        ///    :out_cursor := TEST_refcursor.Ret1Cur;
        ///END;
        ///", cmd);
        /// ]]>
        /// </code>
        /// </remarks>
        public WNonQueryCommand OutRefCursorParameter(string field, Action<IEnumerable<WDataRow>> factory)
        {
            var param = CreateOrUpdateParameter(field, OracleDbType.RefCursor, ParameterDirection.Output);

            param.SetOutputValueUpdater((OracleRefCursor p) =>
            {
                if (!p.IsNull)
                {
                    using (var reader = p.GetDataReader())
                    {
                        factory(ReadReader(reader));
                    }
                }
            });
            return this;
        }

        private IEnumerable<WDataRow> ReadReader(OracleDataReader reader)
        {
            var row = new WDataRow(reader.GetSchemaTable());
            while (reader.Read())
            {
                row.SetValues(reader);
                yield return row;
            }
        }

        #endregion

        #region Scalar In Parameters
        /// <summary>
        /// Bind an input string parameter with specified max size. Should this be obsolete?
        /// </summary>
        /// <param name="field">Parameter Name</param>
        /// <param name="value">Parameter value</param>
        /// <returns>The binder to enable fluent syntax</returns>
        /// <remarks>
        /// Oracle Doc says: When sending a null parameter value to the database, the user must specify DBNull, not null.
        /// The null value in the system is an empty object that has no value. DBNull is used to represent null values.
        /// The user can also specify a null value by setting Status to OracleParameterStatus.NullValue. In this case, the provider sends a null value to the database.
        /// </remarks>
        public WNonQueryCommand Parameter(string field, string value)
        {
            //base.InParameterString(field, value);
            var param = CreateOrUpdateParameter(field, OracleDbType.Varchar2, ParameterDirection.Input);
            param.Value = string.IsNullOrEmpty(value) ? DBNull.Value : (object)value;
            return this;
        }


        /// <summary>
        /// Bind a nullable integer parameter
        /// </summary>
        /// <param name="field"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public WNonQueryCommand Parameter(string field, int? value)
        {
            var param = CreateOrUpdateParameter(field, OracleDbType.Int32, ParameterDirection.Input);
            param.Value = value ?? (object)DBNull.Value;
            return this;
        }

        public WNonQueryCommand Parameter(string field, int value)
        {
            var param = CreateOrUpdateParameter(field, OracleDbType.Int32, ParameterDirection.Input);
            param.Value = value;
            return this;
        }

        /// <summary>
        /// Bind a nullable date parameter
        /// </summary>
        /// <param name="field"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public WNonQueryCommand Parameter(string field, DateTime? value)
        {
            var param = CreateOrUpdateParameter(field, OracleDbType.Date, ParameterDirection.Input);
            param.Value = value ?? (object)DBNull.Value;
            return this;
        }

        //public HOracleCommandNonQuery Parameter(string field, DateTime value)
        //{
        //    var param = CreateOrUpdateParameter(field, OracleDbType.Date, ParameterDirection.Input);
        //    param.Value = value;
        //    return this;
        //}

        /// <summary>
        /// Bind a floating point parameter
        /// </summary>
        /// <param name="field"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public WNonQueryCommand Parameter(string field, decimal? value)
        {
            var param = CreateOrUpdateParameter(field, OracleDbType.Decimal, ParameterDirection.Input);
            param.Value = value ?? (object)DBNull.Value;
            return this;
        }

        //public HOracleCommandNonQuery Parameter(string field, decimal value)
        //{
        //    var param = CreateOrUpdateParameter(field, OracleDbType.Decimal, ParameterDirection.Input);
        //    param.Value = value;
        //    return this;
        //}

        /// <summary>
        /// Bind an interval value
        /// </summary>
        /// <param name="field"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public WNonQueryCommand Parameter(string field, TimeSpan? value)
        {
            var param = CreateOrUpdateParameter(field, OracleDbType.IntervalDS, ParameterDirection.Input);
            param.Value = value ?? (object)DBNull.Value;
            return this;
        }

        /// <summary>
        /// Bind a TimeStampTZ value
        /// </summary>
        /// <param name="field"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public WNonQueryCommand Parameter(string field, DateTimeOffset? value)
        {
            base.InParameterTimeStampTZ(field, value);
            return this;
        }

        #endregion

        #region Scalar Out Parameters

        public WNonQueryCommand OutParameter(string parameterName, Action<string> setter)
        {
            var param = CreateOrUpdateParameter(parameterName, OracleDbType.Varchar2, ParameterDirection.Output);
            //param.OracleDbType = OracleDbType.Varchar2;
            param.Size = 255;
            param.SetOutputValueUpdater((OracleString val) => setter(val.IsNull ? string.Empty : val.Value));
            return this;
        }

        public WNonQueryCommand OutParameter(string parameterName, Action<int?> setter)
        {
            var param = CreateOrUpdateParameter(parameterName, OracleDbType.Int32, ParameterDirection.Output);
            //param.OracleDbType = OracleDbType.Int32;
            param.SetOutputValueUpdater((OracleDecimal val) => setter(val.IsNull ? (int?)null : val.ToInt32()));
            return this;
        }

        public WNonQueryCommand OutParameter(string parameterName, Action<DateTime?> setter)
        {
            var param = CreateOrUpdateParameter(parameterName, OracleDbType.Date, ParameterDirection.Output);
            //param.OracleDbType = OracleDbType.Date;
            param.SetOutputValueUpdater((OracleDate val) => setter(val.IsNull ? (DateTime?)null : val.Value));
            return this;
        }

        public WNonQueryCommand OutParameter(string parameterName, Action<DateTimeOffset?> setter)
        {
            var param = CreateOrUpdateParameter(parameterName, OracleDbType.TimeStampTZ, ParameterDirection.Output);
            param.SetOutputValueUpdater(
                (OracleTimeStampTZ val) => setter(val.IsNull ? (DateTimeOffset?)null : new DateTimeOffset(val.Value, val.GetTimeZoneOffset()))
            );
            return this;
        }

        public WNonQueryCommand OutParameter(string parameterName, Action<TimeSpan?> setter)
        {
            var param = CreateOrUpdateParameter(parameterName, OracleDbType.IntervalDS, ParameterDirection.Output);
            // param.OracleDbType = OracleDbType.IntervalDS;
            param.SetOutputValueUpdater((OracleIntervalDS val) => setter(val.IsNull ? (TimeSpan?)null : val.Value));
            return this;
        }

        #endregion

        #region Associative Array Parameters

        /// <summary>
        /// Passes an aray to a PL/SQL Procedure
        /// </summary>
        /// <param name="field"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        /// <remarks>
        /// <para>
        /// Inspired by http://www.oracle.com/technetwork/issue-archive/2007/07-jan/o17odp-093600.html
        /// See <see cref="OutParameterAssociativeArray"/> for an example
        /// </para>
        /// <para>
        /// Oracle Limitation. zero length arrays cannot be bound
        /// https://docs.oracle.com/html/E41125_02/OracleParameterClass.htm#i1011933
        /// </para>
        /// </remarks>
        public WNonQueryCommand ParameterAssociativeArray(string field, IEnumerable<string> values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }
            var param = CreateOrUpdateParameter(field, OracleDbType.Varchar2, ParameterDirection.Input);
            param.Size = values.Count();
            if (param.Size == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(values), "Empty Associative Arrays cannot be bound");
            }
            param.Value = values;
            param.ArrayBindSize = values.Select(p => p == null ? 0 : p.Length).ToArray();
            param.ArrayBindStatus = Enumerable.Repeat(OracleParameterStatus.Success, param.Size).ToArray();
            // param.OracleDbType = OracleDbType.Varchar2;
            param.CollectionType = OracleCollectionType.PLSQLAssociativeArray;
            return this;
        }

        /// <summary>
        /// Passes an integer array to a PL/SQL procedure
        /// </summary>
        /// <param name="field"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        /// <remarks>
        /// See <see cref="OutParameterAssociativeArray"/> for an example.
        /// </remarks>
        public WNonQueryCommand ParameterAssociativeArray(string field, IEnumerable<int?> values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }


            var param = CreateOrUpdateParameter(field, OracleDbType.Int32, ParameterDirection.Input);

            param.CollectionType = OracleCollectionType.PLSQLAssociativeArray;

            param.Size = values.Count();
            if (param.Size == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(values), "Empty Associative Arrays cannot be bound");
            }
            param.Value = values.Select(p => p.HasValue ? (object)p.Value : DBNull.Value).ToArray();
            param.ArrayBindStatus = Enumerable.Repeat(OracleParameterStatus.Success, param.Size).ToArray();

            return this;
        }

        public WNonQueryCommand ParameterAssociativeArray(string field, IEnumerable<int> values)
        {
            return ParameterAssociativeArray(field, values?.Cast<int?>());
        }


        public WNonQueryCommand ParameterAssociativeArray(string field, IEnumerable<long?> values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }


            var param = CreateOrUpdateParameter(field, OracleDbType.Int64, ParameterDirection.Input);

            param.CollectionType = OracleCollectionType.PLSQLAssociativeArray;

            param.Size = values.Count();
            if (param.Size == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(values), "Empty Associative Arrays cannot be bound");
            }
            param.Value = values.Select(p => p.HasValue ? (object)p.Value : DBNull.Value).ToArray();
            param.ArrayBindStatus = Enumerable.Repeat(OracleParameterStatus.Success, param.Size).ToArray();

            return this;
        }

        public WNonQueryCommand ParameterAssociativeArray(string field, IEnumerable<long> values)
        {
            return ParameterAssociativeArray(field, values?.Cast<long?>());
        }

        public WNonQueryCommand OutParameterAssociativeArray(string field, Action<IEnumerable<long?>> setter, int maxElements)
        {
            var param = CreateOrUpdateParameter(field, OracleDbType.Int64, ParameterDirection.Output);
            param.CollectionType = OracleCollectionType.PLSQLAssociativeArray;

            if (maxElements <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxElements), "Must be greater than 0");
            }
            param.Size = maxElements;  // Maximum number of array elements that can be returned

            // Non integer values raise exception.
            param.SetOutputValueUpdater((OracleDecimal[] p) => setter(p.Select(q => q.IsNull ? (long?)null : q.ToInt64())));
            return this;
        }

        /// <summary>
        /// Bind an output PL/SQL associative array parameter
        /// </summary>
        /// <param name="field"></param>
        /// <param name="setter"></param>
        /// <param name="maxElements">Maximum number of elements which can be returned. ORA-06513 raised if this is too small.</param>
        /// <param name="maxSizePerElement">Maximum size of each element. ORA-06502 raised if this is too small.</param>
        /// <returns></returns>
        /// <remarks>
        /// Null values are returned as empty strings within the array.
        /// </remarks>
        /// <example>
        /// <code>
        /// <![CDATA[
        ///create or replace package sharad_test is
        ///  -- Public type declarations
        ///  type string_list_t is table of varchar2(256) index by pls_integer;
        ///  -- Public function and procedure declarations
        ///  function test_func(a in string_list_t) return string_list_t;
        ///end sharad_test;
        /// ]]>
        /// </code>
        /// <code>
        /// <![CDATA[
        ///        public IEnumerable<Area> GetInventoryAreas()
        ///        {
        ///            const string QUERY = @"
        ///BEGIN
        ///:a := sharad_test.test_func(:a);
        ///END;
        ///            ";
        ///            var arr = new[] { "a", "b" };
        ///            var binder = SqlBinder.Create()
        ///                .ParameterAssociativeArray("a", arr)
        ///                .OutParameterAssociativeArray("a", val => arr = val.ToArray(), Enumerable.Repeat<int>(255, 3).ToArray());
        ///
        ///            _db.ExecuteNonQuery(QUERY, binder);
        ///        }
        /// ]]>
        /// </code>
        /// </example>
        public WNonQueryCommand OutParameterAssociativeArray(string field, Action<IEnumerable<string>> setter, int maxElements, int maxSizePerElement)
        {
            var param = CreateOrUpdateParameter(field, OracleDbType.Varchar2, ParameterDirection.Output);
            //param.OracleDbType = OracleDbType.Varchar2;
            param.CollectionType = OracleCollectionType.PLSQLAssociativeArray;
            param.ArrayBindSize = Enumerable.Repeat(maxSizePerElement, maxElements).ToArray();  // Max Size of each returned element
            param.Size = maxElements;  // Maximum number of elements that can be returned
            param.SetOutputValueUpdater((OracleString[] p) => setter(p.Select(q => q.IsNull ? null : q.Value)));
            param.ArrayBindStatus = Enumerable.Repeat(OracleParameterStatus.Success, param.Size).ToArray();
            return this;
        }

        /// <summary>
        /// Receives an integer array from a PL/SQL procedure
        /// </summary>
        /// <param name="field"></param>
        /// <param name="setter"></param>
        /// <param name="maxElements"></param>
        /// <returns></returns>
        /// <example>
        /// <code>
        /// <![CDATA[
        ///public IEnumerable<Area> GetInventoryAreas()
        ///{
        ///    const string QUERY = @"
        ///BEGIN
        ///:a := sharad_test.test_func(:a);
        ///END;
        ///    ";
        ///    var arr = new int?[] { 1, 2 };
        ///    var binder = SqlBinder.Create()
        ///        .ParameterAssociativeArray("a", arr)
        ///        .OutParameterAssociativeArray("a", val => arr = val.ToArray());
        ///    _db.ExecuteNonQuery(QUERY, binder);
        ///    throw new NotImplementedException();
        ///}
        /// ]]>
        /// </code>
        /// </example>
        public WNonQueryCommand OutParameterAssociativeArray(string field, Action<IEnumerable<int?>> setter, int maxElements)
        {
            var param = CreateOrUpdateParameter(field, OracleDbType.Int32, ParameterDirection.Output);
            //param.OracleDbType = OracleDbType.Int32;
            param.CollectionType = OracleCollectionType.PLSQLAssociativeArray;
            param.Size = maxElements;  // Maximum number of array elements that can be returned

            // Non integer values raise exception.
            param.SetOutputValueUpdater((OracleDecimal[] p) => setter(p.Select(q => q.IsNull ? (int?)null : q.ToInt32())));
            return this;
        }

        #endregion


        public WNonQueryCommand OutParameter(string parameterName, Action<byte[]> setter)
        {
            var param = CreateOrUpdateParameter(parameterName, OracleDbType.Blob, ParameterDirection.Output);
            // param.OracleDbType = OracleDbType.IntervalDS;
            param.SetOutputValueUpdater((OracleBlob val) => setter(val.IsNull ? (byte[]) null : val.Value));
            return this;
        }

        public WNonQueryCommand Parameter(string field, byte[] value)
        {
            var param = CreateOrUpdateParameter(field, OracleDbType.Blob, ParameterDirection.Input);
            param.Value = value ?? (object)DBNull.Value;
            return this;
        }

    }

}
