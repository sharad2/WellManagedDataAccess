using System;
using System.Collections.Generic;
using System.Linq;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System.Data;

namespace HappyOracle.WellManagedDataAccess.Client
{
    /// <summary>
    /// Represents an instance of a row returned from an SQL query
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class is used extensively by the querying functions of <see cref="WConnection"/> class.
    /// In debug mode, this class throws exception if the same column name is used more than once within the query. It also raises
    /// exception if the query retrieves 
    /// </para>
    /// </remarks>
    public class WDataRow
    {
        private readonly OracleDataReader _reader;

        //[Obsolete]
        //private readonly object[] _values;

        /// <summary>
        /// Maps field name to its ordinal. It is possible that the same field name will be retrieved multiple times from the query.
        /// We are using Lookup instead of Dictionary because multiple columns in the query may have same fieldname. This can happen when the caller intends
        /// to access values by index. Inquiry does this.
        /// </summary>
        private readonly ILookup<string, int> _fieldMap;

#if DEBUG
        /// <summary>
        /// When a query column is accessed, its name is added here
        /// </summary>
        private readonly HashSet<string> _debugAccessedFieldNames = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
#endif

        /// <summary>
        /// Create an instance once you have recieved a reader
        /// </summary>
        /// <param name="schemaTable"></param>
        internal WDataRow(OracleDataReader reader)
        {
            var schemaTable = reader.GetSchemaTable();
            //_values = new object[schemaTable.Rows.Count];
            _reader = reader;
            _fieldMap = (from DataRow item in schemaTable.Rows
                         select new
                         {
                             ColumnName = (string)item["ColumnName"],
                             ColumnOrdinal = (int)item["ColumnOrdinal"]
                         }).ToLookup(p => p.ColumnName, p => p.ColumnOrdinal, StringComparer.InvariantCultureIgnoreCase);
        }

        ///// <summary>
        ///// Call this each time the reader is repositioned
        ///// </summary>
        ///// <param name="reader"></param>
        //[Obsolete]
        //internal void SetValues(OracleDataReader reader)
        //{
        //    foreach (var dis in _values.OfType<IDisposable>()) {
        //        throw new NotImplementedException();
        //    }
        //    reader.GetOracleValues(_values);
        //}

        /// <summary>
        /// Thros KeyNotFoundException if the fieldname is not part of the result set
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        private int GetIndex(string fieldName)
        {
#if DEBUG
            _debugAccessedFieldNames.Add(fieldName);
#endif
            try
            {
                return _fieldMap[fieldName].Single();
            }
            catch (InvalidOperationException)
            {
                if (_fieldMap.Contains(fieldName))
                {
                    // Duplicate column names
                    throw new DuplicateNameException($@"Duplicate column name {fieldName}
                        at indexes {string.Join(", ", _fieldMap[fieldName].Select(p => p.ToString()))}");
                }
                else {
                    // Sequence contains no elements
                    throw new KeyNotFoundException($"There is no column named {fieldName}");
                }
            }
        }

        /// <summary>
        /// String column
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public string GetString(int index)
        {
            return _reader.GetString(index);  // GetValueAs<OracleString>(index);
            //if (val.IsNull)
            //{
            //    return null;
            //}
            //return val.Value;
        }

        /// <summary>
        /// String column
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        /// <remarks>
        /// Sharad 15 Feb 2016: Most unit tests in SelectQuery
        /// </remarks>
        public string GetString(string fieldName)
        {

            return GetString(GetIndex(fieldName));
            //var val = GetValueAs<OracleString>(fieldName);
            //if (val.IsNull)
            //{
            //    return string.Empty;
            //}
            //return val.Value;
        }

        /// <summary>
        /// Integer column
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        /// <remarks>
        /// Sharad 15 Feb 2016: Most unit tests in SelectQuery
        /// </remarks>
        public int? GetInteger(string fieldName)
        {
            return GetInteger(GetIndex(fieldName));
            //var val = GetValueAs<OracleDecimal>(fieldName);
            //if (val.IsNull)
            //{
            //    return null;
            //}
            //if (!val.IsInt)
            //{
            //    throw new InvalidCastException(fieldName + " is not an integer. Retrieved value is " + val.ToString());
            //}
            //return val.ToInt32();
        }

        public int? GetInteger(int index)
        {
            if (_reader.IsDBNull(index))
            {
                return null;
            }
            return _reader.GetInt32(index);  // GetValueAs<OracleDecimal>(index);
            //if (val.IsNull)
            //{
            //    return null;
            //}
            //if (!val.IsInt)
            //{
            //    throw new InvalidCastException(string.Format("Field {0} is not an integer. Retrieved value is {1}", index, val.ToString()));
            //}
            //return val.ToInt32();
        }

        //Ankit: Added long data handler function to handle long integer data send from SAP.

        /// <summary>
        /// Long column
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        /// <remarks>
        /// Sharad 15 Feb 2016: Unit test SelectQuery_LongParameter
        /// </remarks>
        public long? GetLong(int index)
        {
            if (_reader.IsDBNull(index))
            {
                return null;
            }
            return _reader.GetInt64(index);  // GetValueAs<OracleDecimal>(index);
            //if (val.IsNull)
            //{
            //    return null;
            //}
            //if (!val.IsInt)
            //{
            //    throw new InvalidCastException($@"Field {index} is not an long. Retrieved value is {val.Value}");
            //}
            //return val.ToInt64();
        }

        public long? GetLong(string fieldName)
        {
            return GetLong(GetIndex(fieldName));
        }

        /// <summary>
        /// Returns a fractional value stored in the database
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        /// <remarks>
        /// If you attempt to retrieve <c>Weight/12</c> from the database, 
        /// you will get an overflow error because the precision of the returned result is not
        /// sufficient to fit into a decimal. 
        /// We catch this exception and reduce the precision before returning the value.
        /// 
        /// Sharad 15 Feb 2016: Most Unit Tests in SelectQueryUnitTests
        /// </remarks>
        public Decimal? GetDecimal(int index)
        {
            var val = _reader.GetOracleDecimal(index);  // GetValueAs<OracleDecimal>(index);
            if (val.IsNull)
            {
                return null;
            }
            try
            {
                return val.Value;
            }
            catch (OverflowException)
            {
                return OracleDecimal.SetPrecision(val, 28).Value;
            }
        }

        public Decimal? GetDecimal(string fieldName)
        {
            return GetDecimal(GetIndex(fieldName));
        }

        /// <summary>
        /// Retrieves the date value from an OracleDate, OracleTimeStamp
        /// </summary>
        /// <param name="fieldName">Column name</param>
        /// <returns></returns>
        /// <remarks>
        /// Sharad 15 Feb 2016: Most unit tests in SelectQueryUnitTests
        /// </remarks>
        public DateTime? GetDate(int index)
        {
            var val = (INullable)_reader.GetProviderSpecificValue(index);  //.GetOracleDate(index);  // GetValueAs<INullable>(index);
            if (val.IsNull)
            {
                return null;
            }
            var type = val.GetType();
            if (type == typeof(OracleDate))
            {
                return ((OracleDate)val).Value;
            }
            if (type == typeof(OracleTimeStamp))
            {
                return ((OracleTimeStamp)val).Value;
            }
            throw new NotSupportedException("Unsupported Date type " + type.FullName);
        }

        public byte[] GetBlob(int index)
        {
            var val = _reader.GetOracleBlob(index);  // GetValueAs<OracleBlob>(index);
            if (val.IsNull)
            {
                return null;
            }
            return val.Value;
        }

        public byte[] GetBlob(string fieldName)
        {
            return GetBlob(GetIndex(fieldName));
        }

        public DateTime? GetDate(string fieldName)
        {
            return GetDate(GetIndex(fieldName));
        }

        /// <summary>
        /// OracleTimeStampTZ and OracleTimeStampLTZ values
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        /// <remarks>
        /// Sharad 15 Feb 2016: Unit Test SelectQuery_DateTimeOffsetParameter
        /// </remarks>
        public DateTimeOffset? GetDateTimeOffset(int index)
        {
            var val = (INullable)_reader.GetProviderSpecificValue(index);  // GetValueAs<INullable>(index);
            if (val.IsNull)
            {
                return null;
            }
            var type = val.GetType();
            //if (type == typeof(OracleDate))
            //{
            //    return ((OracleDate)val).Value;
            //}
            //if (type == typeof(OracleTimeStamp))
            //{
            //    return ((OracleTimeStamp)val).Value;
            //}
            if (type == typeof(OracleTimeStampTZ))
            {
                var v = (OracleTimeStampTZ)val;
                return new DateTimeOffset(v.Value, v.GetTimeZoneOffset());
            }
            if (type == typeof(OracleTimeStampLTZ))
            {
                return ((OracleTimeStampLTZ)val).Value;
            }
            throw new NotSupportedException("Unsupported Time Zone Date type " + type.FullName);
        }

        public DateTimeOffset? GetDateTimeOffset(string fieldName)
        {
            return GetDateTimeOffset(GetIndex(fieldName));
        }

        ///// <summary>
        ///// For Timestamp with TimeZone database columns
        ///// </summary>
        ///// <param name="fieldName"></param>
        ///// <returns></returns>
        //public DateTimeOffset? GetTimeStampTZ(string fieldName)
        //{
        //    var val = GetValueAs<OracleTimeStampTZ>(fieldName);
        //    if (val.IsNull)
        //    {
        //        return null;
        //    }
        //    return new DateTimeOffset(val.Value, val.GetTimeZoneOffset());
        //}

        /// <summary>
        /// OracleIntervalDS column
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public TimeSpan? GetTimeSpan(string fieldName)
        {
            return GetTimeSpan(GetIndex(fieldName));
        }

        public TimeSpan? GetTimeSpan(int index)
        {
            if (_reader.IsDBNull(index))
            {
                return null;
            }
            return _reader.GetTimeSpan(index);   //.GetOracleIntervalDS(index); // GetValueAs<OracleIntervalDS>(index);
            //if (val.IsNull)
            //{
            //    return null;
            //}
            //return val.Value;
        }

        //public long? GetIntervalYM(string fieldName)
        //{
        //    return GetIntervalYM(GetIndex(fieldName));
        //}

        ///// <summary>
        ///// IntervalYM. Returns number of months
        ///// </summary>
        ///// <param name="index"></param>
        ///// <returns></returns>
        //public long? GetIntervalYM(int index)
        //{
        //    var val = _reader.GetOracleIntervalYM(index);  // GetValueAs<OracleIntervalYM>(index);
        //    if (val.IsNull)
        //    {
        //        return null;
        //    }
        //    return val.Value;
        //}

        //#if DEBUG
        //        [Obsolete]
        //        private HashSet<string> _debugFieldNames;
        //#endif

        //        [Obsolete]
        //        private T GetValueAs<T>(string fieldName)
        //        {
        //#if DEBUG
        //            // Additional debug check to ensure that same field is not being retrieved twice.
        //            if (_debugFieldNames == null)
        //            {
        //                _debugFieldNames = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
        //                foreach (var item in _fieldMap)
        //                {
        //                    var b = _debugFieldNames.Add(item.Key);
        //                    if (!b)
        //                    {
        //                        // Key already added
        //                        var msg = string.Format("DEBUG only exception: Query retrieves column {0} more than once.", item.Key);
        //                        throw new OperationCanceledException(msg);
        //                    }
        //                }
        //            }
        //            // The dispose method raises exception if all fields are not accessed
        //            _debugFieldNames.Remove(fieldName);
        //#endif
        //            object obj;
        //            try
        //            {
        //                obj = _values[_fieldMap[fieldName].Single()];
        //            }
        //            catch (InvalidOperationException ex)
        //            {
        //                throw new InvalidOperationException(fieldName, ex);
        //            }
        //#if DEBUG
        //            catch (KeyNotFoundException ex)
        //            {
        //                // Reraise the exception with a clearer message
        //                throw new KeyNotFoundException(fieldName, ex);
        //            }
        //#else
        //            catch (KeyNotFoundException)
        //            {
        //                // Eat the exception to prevent the application from crashing because some column was dropped.
        //                obj = null;
        //            }
        //#endif
        //            if (obj == null)
        //            {
        //                return default(T);
        //            }
        //            try
        //            {
        //                return (T)obj;
        //            }
        //            catch (InvalidCastException ex)
        //            {
        //                throw new InvalidCastException(string.Format("Field {0}: Could not cast {1} to {2}", fieldName, obj.GetType().FullName, typeof(T).FullName), ex);
        //            }
        //        }
        //[Obsolete]
        //private T GetValueAs<T>(int index)
        //{
        //    object obj = _values[index];
        //    if (obj == null)
        //    {
        //        return default(T);
        //    }
        //    try
        //    {
        //        return (T)obj;
        //    }
        //    catch (InvalidCastException)
        //    {
        //        var fieldName = _fieldMap.Where(p => p.Contains(index)).Select(p => p.Key).First();
        //        throw new InvalidCastException($@"Field {fieldName} at index {index} could not be cast from
        //            {obj.GetType().FullName} to {typeof(T).FullName}");
        //    }
        //}

#if DEBUG
        /// <summary>
        /// This is called after we are done with retrieving all values. It ensures that every field retrieved by the query is used.
        /// </summary>
        internal void DebugCheck()
        {
            //foreach (var dis in _values.OfType<IDisposable>())
            //{
            //    dis.Dispose();
            //}
            if (_debugAccessedFieldNames.Count == 0)
            {
                // OK. They must have accessed all fields by index
                return;
            }

            var unusedColumns = _fieldMap.Select(p => p.Key)
                .Where(p => !p.EndsWith("_"))
                .Except(_debugAccessedFieldNames, StringComparer.InvariantCultureIgnoreCase);

            // Make sure that all fields retrieved by the query were accessed
            if (unusedColumns.Any())
            {
                // If you have query columns which are conditionally used, you should suffix them with _ to suppress this exception.
                throw new OperationCanceledException(string.Format("DEBUG only exception: Unused columns retrieved by the query: {0}",
                    string.Join(",", unusedColumns)));
            }
        }
#endif
    }
}
