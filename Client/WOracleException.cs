using System.Data.Common;
using System.Linq;
using System.Text;
using Oracle.ManagedDataAccess.Client;
using System.Collections.Generic;

namespace HappyOracle.WellManagedDataAccess.Client
{
    /// <summary>
    /// Dispays more useful diagnostics when oracle error occurs, but only in DEBUG mode
    /// </summary>
    /// <remarks>
    /// The contents of OracleErrorCollection are displayed. Raises <see cref="OracleDataStoreErrorEvent"/> so that it can be logged even if this exception
    /// is caught and handled
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2237:MarkISerializableTypesWithSerializable")]
    public class WOracleException : DbException
    {
        internal static WOracleException Create(OracleException ex, OracleCommand cmd)
        {
            switch (ex.Number)
            {
                case Helpers.OracleErrorNumber.PROXY_NOT_AUTHORIZED_TO_CONNECT:
                    return new WOracleProxyException(ex, cmd);

                case Helpers.OracleErrorNumber.ARRAY_BIND_ERROR:
                    return new WOracleArrayBindException(ex, cmd);

                default:
                    return new WOracleException(ex, cmd);
            }

        }

        protected readonly StringBuilder _sbMessage;
        protected WOracleException(OracleException ex, OracleCommand cmd)
            : base("", ex)
        {
            _sbMessage = new StringBuilder();
            _sbMessage.AppendLine(ex.Message);
            if (ex.Errors.Count > 1)
            {
                // The first error is already a part of the main exception
                foreach (var error in ex.Errors.OfType<OracleError>().Skip(1))
                {
                    _sbMessage.AppendLine(error.Message);
                }
            }
#if DEBUG
            if (cmd != null)
            {
                _sbMessage.AppendLine("Additional Debug Mode Information");
                _sbMessage.AppendLine(cmd.CommandText);
                foreach (OracleParameter param in cmd.Parameters)
                {
                    _sbMessage.AppendFormat("{0} ({1} {2}: *{3}*)", param.ParameterName, param.Direction, param.OracleDbType, param.Value);
                    _sbMessage.AppendLine();
                }
            }
#endif

            //var ev = new OracleDataStoreErrorEvent("OracleDataStoreException is being raised", cmd, this);
            //ev.Raise();
        }

        public override string Message
        {
            get
            {
                return _sbMessage.ToString();
            }
        }

        public int OracleErrorNumber
        {
            get
            {
                return ((OracleException)this.InnerException).Number;
            }
        }
    }

    public class WOracleProxyException : WOracleException
    {
        public WOracleProxyException(OracleException ex, OracleCommand cmd) : base(ex, cmd)
        {

        }
    }

    public class WOracleArrayBindException : WOracleException
    {
        private readonly Dictionary<int, string> _errors;

        public WOracleArrayBindException(OracleException ex, OracleCommand cmd) : base(ex, cmd)
        {
            _errors = (from OracleError e in ex.Errors
                       where e.ArrayBindIndex >= 0
                       select e).ToDictionary(p => p.ArrayBindIndex, p => p.Message);

            // error(s) in array DML
            // See http://docs.oracle.com/html/B14164_01/featOraCommand.htm
            foreach (var error in _errors)
            {
                _sbMessage.AppendLine($"[Row {error.Key}] {error.Value}");
            }
        }

        public Dictionary<int, string> RowErrors
        {
            get
            {
                return _errors;
            }
        }
    }

    //internal class OracleDataStoreErrorEvent: WebErrorEvent
    //{
    //    private StringBuilder _sb;
    //    public OracleDataStoreErrorEvent(string msg, OracleCommand cmd, OracleDataStoreException ex): base(msg, ex, WebEventCodes.WebExtendedBase + 1, ex)
    //    {
    //        _sb = new StringBuilder();
    //        if (cmd != null)
    //        {
    //            if (cmd.Connection != null)
    //            {
    //                _sb.AppendFormat("Connection DataSource {0}; Host Name: {1}; State: {2}", cmd.Connection.DataSource, cmd.Connection.HostName, cmd.Connection.State);
    //            }
    //            _sb.AppendLine(cmd.CommandText);
    //            foreach (OracleParameter param in cmd.Parameters)
    //            {
    //                _sb.AppendFormat("{0} ({1} {2}: *{3}*)", param.ParameterName, param.Direction, param.OracleDbType, param.Value);
    //                _sb.AppendLine();
    //            }
    //        }
    //    }

    //    public override void Raise()
    //    {
    //        _sb.AppendFormat("Event Raised at {0}", EventTime);
    //        base.Raise();
    //    }

    //    public override void FormatCustomEventDetails(WebEventFormatter formatter)
    //    {
    //        formatter.AppendLine(_sb.ToString());
    //        base.FormatCustomEventDetails(formatter);
    //    }
    //}
}
