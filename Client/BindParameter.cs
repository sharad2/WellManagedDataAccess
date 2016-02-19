using Oracle.ManagedDataAccess.Client;
using System;
using System.Data;

namespace HappyOracle.WellManagedDataAccess.Client
{
    /// <summary>
    /// The class defined here is nested within WCommandBase
    /// </summary>
    public abstract partial class WCommandBase
    {
        /// <summary>
        /// A placeholder class to encapsulate the properties of an OracleParameter
        /// </summary>
        /// <remarks>
        /// This class is very similar to OracleParameter. It exists because the same OracleParameter cannot be added to multiple OracleCommands.
        /// </remarks>
        protected class BindParameter
        {
            public string ParameterName { get; set; }

            //[Obsolete]
            //internal OracleParameter CreateOracleParameter(OracleConnection conn)
            //{
            //    var param = new OracleParameter
            //    {
            //        ParameterName = this.ParameterName,
            //        Size = this.Size,
            //        OracleDbType = this.OracleDbType,
            //        Value = this.Value,
            //        Direction = this.Direction,
            //        CollectionType = this.CollectionType,
            //        ArrayBindSize = this.ArrayBindSize,
            //        ArrayBindStatus = this.ArrayBindStatus
            //    };

            //    if (this.OracleDbType == OracleDbType.Blob && Value != null && Value != DBNull.Value)
            //    {
            //        var blob = new OracleBlob(conn, false);
            //        var bytes = (byte[])Value;
            //        blob.Write(bytes, 0, bytes.Length);
            //        param.Value = blob;
            //    }
            //    else
            //    {
            //        param.Value = Value;
            //    }
            //    return param;
            //}

            public OracleParameterStatus[] ArrayBindStatus { get; set; }
            public ParameterDirection Direction { get; set; }

            public OracleDbType OracleDbType { get; set; }

            public object Value { get; set; }

            public int Size { get; set; }

            public OracleCollectionType CollectionType { get; set; }

            private Action<object> _outputValueUpdater;

            /// <summary>
            /// Non null for out parameters only. Sets the out value in the calling method variable
            /// </summary>
            public Action<object> OutputValueUpdater
            {
                get
                {
                    return _outputValueUpdater;
                }
            }

            public void SetOutputValueUpdater<TValue>(Action<TValue> converter)
            {
                _outputValueUpdater = (object val) => converter((TValue)val);
            }

            /// <summary>
            /// Used to specify the max size of each returned string in string OutParameterAssociativeArray
            /// </summary>
            public int[] ArrayBindSize { get; set; }

            public int XmlRepeatCount { get; internal set; } = -1;
        }


    }
}