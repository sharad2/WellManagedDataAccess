using Oracle.ManagedDataAccess.Client;
using System;

namespace HappyOracle.WellManagedDataAccess.Helpers
{
    public static class OdpHelpers
    {
        public static void DisposeCommand(OracleCommand cmd)
        {
            if (cmd == null)
            {
                return;
            }
            foreach (OracleParameter parameter in cmd.Parameters)
            {
                var dis = parameter.Value as IDisposable;
                if (dis != null)
                {
                    // We expect to get here for BLOB parameters
                    dis.Dispose();
                }
                parameter.Dispose();
            }
            cmd.Dispose();
        }

    }
}
