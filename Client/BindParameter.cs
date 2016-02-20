using Oracle.ManagedDataAccess.Client;
using System;
using System.Data;

namespace HappyOracle.WellManagedDataAccess.Client
{

    /// <summary>
    /// A placeholder class to encapsulate the properties of an OracleParameter
    /// </summary>
    /// <remarks>
    /// This class is very similar to OracleParameter. It exists because the same OracleParameter cannot be added to multiple OracleCommands.
    /// </remarks>
    public class BindParameter
    {
        public string ParameterName { get; set; }

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