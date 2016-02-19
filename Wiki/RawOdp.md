Contrast this with the raw ODP.NET you would need to write to create the same list of `Employee` objects.

    var list = new List<Employee>();
    using (var conn = new OracleConnection(CONNECT_STRING))
    {
        conn.Open();
        using (var cmd = new OracleCommand(QUERY, conn))
        {
            using (OracleDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var row = new Employee();
                    var i = reader.GetOrdinal("employee_id");
                    if (!reader.IsDBNull(i))
                    {
                        row.EmployeeId = reader.GetInt32(i);
                    }

                    i = reader.GetOrdinal("first_name");
                    if (!reader.IsDBNull(i))
                    {
                        row.FirstName = reader.GetString(i);
                    }

                    i = reader.GetOrdinal("last_name");
                    if (!reader.IsDBNull(i))
                    {
                        row.LastName = reader.GetString(i);
                    }
                    list.Add(row);
                }
                return list;
            }
        }
    }
    foreach (var row in list)
    {
        Console.WriteLine("EmployeeId: {0}; First Name: {1}",
            row.EmployeeId, row.FirstName);
    }
