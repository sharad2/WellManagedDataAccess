using System;

namespace HappyOracle.WellManagedDataAccess.Client
{
    public static class WCommand
    {
        /// <summary>
        /// Create a binder for retrieving the results of an SQL query
        /// <example>
        /// SqlBinder.Create(row => new RestockCarton
        ///  {
        ///    CartonId = row.GetString("CARTON_ID"),
        ///    ...
        ///  });
        /// </example>
        /// </summary>
        /// <typeparam name="T">Type of each row</typeparam>
        /// <param name="factory">Lambda which takes a row and returns a strongly typed object
        /// <example>
        ///row => new RestockCarton {
        ///        CartonId = row.GetString("CARTON_ID"),
        ///        QualityCode = row.GetString("QUALITY_CODE"),
        ///        SkuInCarton = new Sku {
        ///                                SkuId = row.GetInteger("SKU_ID"),
        ///                                Style = row.GetString("STYLE")
        ///                            }
        ///    }
        /// </example>
        /// </param>
        /// <returns></returns>
        public static WCommand<T> Create<T>(string query, Func<WDataRow, T> factory)
        {
            var binder = new WCommand<T>(factory);
            binder.CommandText = query;
            return binder;
        }

        /// <summary>
        /// Create a binder for executing PL/SQL blocks
        /// </summary>
        /// <returns></returns>
        public static WNonQueryCommand Create(string query)
        {
            var binder = new WNonQueryCommand();
            binder.CommandText = query;
            return binder;
        }

        /// <summary>
        /// Create a binder for for executing DML multiple times using DML array binding
        /// </summary>
        /// <param name="arrayBindCount"></param>
        /// <returns></returns>
        public static WNonQueryArrayCommand Create(string query, int arrayBindCount)
        {
            var binder = new WNonQueryArrayCommand(arrayBindCount);
            binder.CommandText = query;
            return binder;
        }
    }
}
