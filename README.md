# What is *WellManagedDataAccess* Library#

ODP.NET was invented during the .NET 2.0 days. Whereas it provides fairly comprehensive access to Oracle database functionality, it does not make it easy to perform common tasks. `OracleDataReader` and `OracleDataset` return query results as loosely typed object values which must then be cast to the .NET type you are interested in. Today's modern applications need to have the results in strongly typed generic collections so that they can be processed further by the application. Converting the results from loosely typed values to a strongly typed collection requires a lot of boiler plate code. This library which we call *WellManagedDataAccess* library encapsulates all this boiler plate code using the power of .NET generic features. Thus you can write succinct code and focus on the functionality of your application.

# Quick Start #

Let us start by looking at code fragments which implement CRUD operations for the `employees` table. You can read the code easily without knowing anything about *WellManagedDataAccess* library.

First we will select a few employees.

    public class Employee
    {
        public int EmployeeId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }

	// Pass lastNameLike = null to select all employees
	public IList<Employee> SelectEmployees(string lastNameLike) {
		const string QUERY = @"
				SELECT employee_id, first_name, last_name
				  FROM employees
			WHERE last_name LIKE :last_name || '%'";

	    var cmd = HOracleCommand.Create(QUERY, row => new Employee
	    {
	        EmployeeId = row.GetInteger("employee_id") ?? 0,
	        FirstName = row.GetString("first_name"),
			LastName = row.GetString("last_name")
	    }).Parameter("last_name", lastNameLike);
	
	    using (var conn = new HOracleConnection(
			"User Id=scott; Password=tiger; Data Source=localhost/dcmsprd1"))
	    {
	        return conn.ExecuteReader(cmd);
	    }
	}

Here is a function to delete an employee.

	public void DeleteEmployee(int employeeId) {
		const string QUERY = @"
				DELETE FROM employees
				 WHERE employee_id = :employee_id;

	    var cmd = HOracleCommand.Create(QUERY)
			.Parameter("employee_id", employeeId);
	
	    using (var conn = new HOracleConnection(
			"User Id=scott; Password=tiger; Data Source=localhost/dcmsprd1"))
	    {
	        conn.ExecuteNonQuery(cmd);
	    }
	}

Function to update an employee. 

	public void UpdateEmployee(Employee emp) {
		const string QUERY = @"
				UPDATE employees
				   SET first_name = first_name, last_name = :last_name
				 WHERE employee_id = :employee_id
		";

	    var cmd = HOracleCommand.Create(QUERY)
			.Parameter("employee_id", emp.EmployeeId)
			.Parameter("first_name", emp.FirstName)
			.Parameter("last_name", emp.LastName);
	
	    using (var conn = new HOracleConnection(
			"User Id=scott; Password=tiger; Data Source=localhost/dcmsprd1"))
	    {
	        conn.ExecuteNonQuery(cmd);
	    }
	}

Inserting an employee will be very similar to updating an employee.

	public void InsertEmployee(Employee emp) {
		const string QUERY = @"
			insert into EMPLOYEES (employee_id, first_name, last_name)
			     values (:employee_id, :first_name, :last_name)
		";

	    var cmd = HOracleCommand.Create(QUERY)
			.Parameter("employee_id", emp.EmployeeId)
			.Parameter("first_name", emp.FirstName)
			.Parameter("last_name", emp.LastName);
	
	    using (var conn = new HOracleConnection(
			"User Id=scott; Password=tiger; Data Source=localhost/dcmsprd1"))
	    {
	        conn.ExecuteNonQuery(cmd);
	    }
	}


# Code Walkthrough #

These are the basic steps involved in executing a query. They are very similar to the steps you use when using ODP.NET directly.


1. Create an `HOracleCommand`. You can think of this as a strongly typed version of the ODP.NET `OracleCommand`. It can only be created by using the static `HOracleCommand.Create()` function. Depending on the arguments passed to the `Create()` function, a command of the appropriate derived type of `HOracleCommand` is created. There are various specialized types available for the task at hand. You normally do not have to worry about the specific type. `Create()` takes care of that for you
2. Add Parameters. Add to the `HOracleCommand` instance all the parameters needed by your query. There are overloads of `Parameter()` function available for most .NET datatypes. You just specify the name and the value of the parameter. All other meta information is inferred based on the value data type.

2. Create `HOracleConnection`. As the name implies, this is analogous to ODP.NET `OracleConnection`.

3. Execute the query. `HOracleConnection` has functions for executing queries such as `ExecuteReader()` and `ExecuteNonQuery()`. In ODP.NET these functions would have been available in the `OracleCommand` type, but I think they should have really been in the `OracleConnection` class.

4. Dispose `HOracleConnection`. This is the only disposing you are responsible for. All other disposing is taken care of for you.

5. Do something with results. If your query returned results, use them as needed.

## SELECT Queries ##

Let us now walk through the code of `SelectEmployees` function. To select records from the database you need to first write the SELECT query.

	const string QUERY = @"
			SELECT employee_id, first_name, last_name
			  FROM employees
		     WHERE last_name LIKE :last_name || '%'";

*WellManagedDataAccess* never writes queries for you. Every single query your application will execute would be manually written by you. This is in sharp contrast to the philosophy of Entity frameworks who are always trying to save you the trouble of writing queries. They provide low level hooks for those "rare" cases when you might need a custom query. However over my decades of experience as a software developer I have realized that in real world applications these so called rare cases are actually the norm.

Therefore *WellManagedDataAccess* does away with any pretense of automatically generating queries. I have used this library in dozens of real world large scale applications and have never regretted this choice. I have also found that my team members are more productive with this approach compared to debugging what is going on behind the scenes in Entity frameworks.

Next step is to create `HOracleCommand` which will define how each column returned by the query will map to application variables. It will also provide values for the parameters needed by the query.

    var cmd = HOracleCommand.Create(QUERY, row => new Employee
    {
        EmployeeId = row.GetInteger("employee_id") ?? 0,
        FirstName = row.GetString("first_name"),
		LastName = row.GetString("last_name")
    });

A command is created by the static `HOracleCommand.Create` function whose prototype is:

	public static HOracleCommand<T> Create<T>(string query,
		Func<OracleDataRow, T> factory)

There are other overloads of the `Create` function available but we will focus on this overload which is used for creating a command specialized for SELECT queries. This overload is a generic function that takes a lambda expression which receives and `OracleDataRow` object. The lambda expression must extract column values from the passed row instance and populate the values in any application type.

In our example we return an `Employee` instance. This means that our command is of type `HOracleCommand<Employee>`. This lambda expression is called for each row, as it is being retrieved from the database. Functions are available for retrieving all types of data. Here is a full list:


<table>
<caption>Accessing Oracle Column values</caption>
<tr>
<th>OracleDataRow Functions</th>
<th>Oracle Data Type</th>
</tr>
<tr>
<td>
public string GetString(string fieldName)
</td>
<td>
CHAR, LONG, LONG RAW, NCHAR, NVARCHAR2, ROWID, UROWID, VARCHAR2
</td>
</tr>
<tr>
<td>
public int? GetInteger(string fieldName)
</td>
<td>
NUMBER, PLS_INTEGER. Exception if the value is fractional or too big to fit in an int.
</td>
</tr>
<tr>
<td>
public long? GetLong(string fieldName)
</td>
<td>
NUMBER. Exception if the value is fractional or too big to fit in a long
</td>

</tr>
<tr>
<td>
public Decimal? GetDecimal(string fieldName)
</td>
<td>
NUMBER
</td>
</tr>
<tr>
<td>
public DateTime? GetDate(string fieldName)
</td>
<td>
DATE, TIMESTAMP
</td>
</tr>
<tr>
<td>
public DateTimeOffset? GetDateTimeOffset(string fieldName)
</td>
<td>
TIMESTAMP WITH TIME ZONE, TIMESTAMP WITH LOCAL TIME ZONE
</td>
</tr>
<tr>
<td>
public TimeSpan? GetInterval(string fieldName)
</td>
<td>
INTERVAL DAY TO SECOND
</td>
</tr>
</table>

All of the above functions throw an `InvalidCastException` if the type of the database column being retrieved does not match the type expected by the function. No attempt is made to convert the type. As an example, `GetInteger` will fail for string columns even if the string can be converted to a number. Similarly, `GetString` will fail for `NUMBER` columns even though we could have chosen to represent the number as a string.

Also note that each of the above functions returns a nullable type. If you know that the underlying column is a not null column, you can code this yourself as in `EmployeeId = row.GetInteger("employee_id") ?? 0`.

After the command is created, we can add parameter values to it.

	cmd.Parameter("last_name", lastNameLike);

This internally creates an `OracleParameter` whose name is `last_name`. Since `lastNameLike` is a string variable, this becomes a string parameter. The value of the this parameter is the value of the `lastNameLike` variable. You can add as many parameters as needed by the query. These are the common overloads available for the `Parameter` function.

    public HOracleCommand<T> Parameter(string field, string value)
    public HOracleCommand<T> Parameter(string field, int? value)
    public HOracleCommand<T> Parameter(string field, int value)
    public HOracleCommand<T> Parameter(string field, DateTime? value)
    public HOracleCommand<T> Parameter(string field, DateTimeOffset? value)
    public HOracleCommand<T> Parameter(string field, decimal? value)

At this point our command definition is complete. We are ready to execute the query.

    using (var conn = new HOracleConnection(
		"User Id=scott; Password=tiger; Data Source=localhost/dcmsprd1"))
    {
        return conn.ExecuteReader(cmd);
    }

To execute the query we create an `HOracleConnection` instance by supplying the connection string to the constructor. The `ExecuteReader` function executes the query by taking the the command as a parameter. 

	public IList<T> ExecuteReader<T>(HOracleCommand<T> binder)

This is also a generic function. Since the command we are passing to this function is of type `HOracleCommand<Employee>`, our `ExecuteReader` return a list of employees, i.e. `IList<Employee>`  This achieves our goal of returning the database rows in a list of strongly typed objects.

To prevent memory related crashes, `ExecuteReader` limits the number of rows it returns. This limit is about 1000 rows. If the query returns more than these many rows, an exception will be raised. Another overload is available where you can specify your own limit.

	public IList<T> ExecuteReader<T>(HOracleCommand<T> binder, int maxRows)

If we know that this query will return only a single row, we can use `ExeuteSingle` instead of `ExecuteReader`.

    using (var conn = new HOracleConnection(connectStr))
    {
        Employee rec = conn.ExecuteSingle(cmd);

		if (rec == null) {
			// Query did not return any row
		} else {
	        Console.WriteLine("EmployeeId: {0}; First Name: {1}",
	            rec.EmployeeId, rec.FirstName);
		}
    }


# Executing NonQueries #

Any query which is not a SELECT query is a non query. This includes all DML, DDL and PL/SQL blocks. Executing a non query is even simpler than executing SELECT queries because there is no need to define column mappings. All you need is the query text and parameter values. The steps for executing non queries is the same as steps for executing SELECT queries.

## DML Query Walkthrough ##

This is the `DeleteEmployee()` which you saw earlier in this document.

	public void DeleteEmployee(int employeeId) {
		const string QUERY = @"
				DELETE FROM employees
				 WHERE employee_id = :employee_id;

	    var cmd = HOracleCommand.Create(QUERY)
			.Parameter("employee_id", employeeId);
	
	    using (var conn = new HOracleConnection(
			"User Id=scott; Password=tiger; Data Source=localhost/dcmsprd1"))
	    {
	        conn.ExecuteNonQuery(cmd);
	    }
	}



Here we created the command instance by using the `HOracleCommand.Create()` overload which simply takes query text as an argument. This overload creates a command of type `HOracleCommandNonQuery`. This is the signature of this function.

	public static HOracleCommandNonQuery Create(string query)

`HOracleCommandNonQuery` also allows you to specify parameters of various types.

	public HOracleCommandNonQuery Parameter(string field, string value)
	public HOracleCommandNonQuery Parameter(string field, int? value)
	public HOracleCommandNonQuery Parameter(string field, int value)
	public HOracleCommandNonQuery Parameter(string field, DateTime? value)
	public HOracleCommandNonQuery Parameter(string field, decimal? value)
	public HOracleCommandNonQuery Parameter(string field, TimeSpan? value)
	public HOracleCommandNonQuery Parameter(string field, DateTimeOffset? value)

In this example we implicitly create an integer parameter because `employee_id` is int. We could have created as many parameters as needed by the query. Here is some sample code from the `InsertEmployee()` function which creates one integer parameter (`employee_id`) and two string parameters (`first_name` and `last_name`).

	const string QUERY = @"
		insert into EMPLOYEES (employee_id, first_name, last_name)
		     values (:employee_id, :first_name, :last_name)
	";

    var cmd = HOracleCommand.Create(QUERY)
		.Parameter("employee_id", emp.EmployeeId)
		.Parameter("first_name", emp.FirstName)
		.Parameter("last_name", emp.LastName);

Executing this query follows the same pattern.

	    using (var conn = new HOracleConnection(
			"User Id=scott; Password=tiger; Data Source=localhost/dcmsprd1"))
	    {
			// Return value is the number of rows affected
	        var nRows = conn.ExecuteNonQuery(cmd);
	    }

There are two overloads of the `ExecuteNonQuery()` function in the `HOracleConnection` class.

	public int ExecuteNonQuery(HOracleCommandNonQueryBase binder);
	public int ExecuteNonQuery(string query)

We use the first overload here because `HOracleCommandNonQuery` derives from `HOracleCommandNonQueryBase`.

The second overload is useful when the query does not require any parameters at all. It saves us from having to create the `HOracleCommand` command instance.

	conn.ExecuteNonQuery("drop table t1");

In fact, the above one line code is equivalent to these two lines of code.

	var cmd = HOracleCommand.Create(QUERY);
	conn.ExecuteNonQuery(cmd);

Since parameterless queries are so common, especially while executing DDL, I decided to provide this special shortcut.

## Transactions ##

By default, each statement is automatically committed immediately after it is executed. If we wish to execute multiple statements as a single transaction, we need to wrap all of them in a transaction object. This code extract deletes all departments whose id is stored in the array `myArrayDeptNo`.

    var cmd = SqlBinder.Create(@"
					delete from departments
					 where department_id = :deptno");

    using (var conn = new HOracleConnection(connectStr))
    {
		using (var trans = db.BeginTransaction()) {
            foreach (var id in myArrayDeptNo)
            {
                cmd.Parameter("deptno", id);
                conn.ExecuteNonQuery(cmd);
            }
			trans.Commit();
		}
    }

Notice that `trans.Commit()` is required. Otherwise the transaction will be automatically rolled back when the transaction object `trans` is disposed off. This is very useful, since if an exception occurs, you do not have to remember to rollback. Here we commit after all the departments have been deleted.

## Nonqueries with OUT and INOUT Parameters ##

This simple PL/SQL anonymous block simply concatenates the two input strings `str1` and `str2` and returns then in the out parameter `str3`.

    const string QUERY = @"
		begin
			:str3 := :str1 || :str2;
		end;
		";
    string str1 = "Sharad";
    string str2 = "Singhal";
    string str3 = null;
    var cmd = HOracleCommand.Create(QUERY)
        .Parameter("str1", str1)
        .Parameter("str2", str2)
        .OutParameter("str3", val => str3 = val);
	using (var conn = new HOracleConnection("my connect string")) {
    	conn.ExecuteNonQuery(cmd);
	}
    Assert.AreEqual(str1 + str2, str3);

We already know how to bind input parameters `str1` and `str2`. `str3` is an out parameter which is bound using the function `OutParameter()`. `HOracleCommandNonQuery` provides several overloads of the `OutParameter()` function, one for each parameter data type.

	public HOracleCommandNonQuery OutParameter(string parameterName,
		Action<string> setter);
	public HOracleCommandNonQuery OutParameter(string parameterName,
		Action<int?> setter);
	public HOracleCommandNonQuery OutParameter(string parameterName,
		Action<DateTime?> setter);
	public HOracleCommandNonQuery OutParameter(string parameterName,
		Action<DateTimeOffset?> setter);
	public HOracleCommandNonQuery OutParameter(string parameterName,
		Action<TimeSpan?> setter);

Each of these overloads takes a lambda expression which is passed the value of the out parameter after the query has been executed. The job of the lambda expression is to store this value in an application variable. Let us take one more look at how we bound our string output parameter in our example.

	string str3 = null;
	cmd.OutParameter("str3", val => str3 = val);

This lambda expression stores the passed value `val` in a `string` variable `str3`. This causes .NET to correctly infer that the value being passed must be of `string` type. This in turn causes invocation of the first overload of `OutParameter()` function whose Action passes a `string`. This overload has code to create an output oracle parameter which is of string type. Same magic happens for other data types as well.

Here is another example which returns the database date. This also demonstrates `DateTimeOffset` out parameters. `CURRENT_TIMESTAMP` is an Oracle function which returns server time along with time zone.

        const string QUERY = @"
	begin
		select CURRENT_TIMESTAMP
		INTO :server_timestamp
		from dual;
	end;
	";
    DateTimeOffset? serverTime = null;
    var cmd = HOracleCommand.Create(QUERY)
        .OutParameter("server_timestamp",
			(DateTimeOffset? val) => serverTime = val);
    conn.ExecuteNonQuery(cmd);
	// serverTime now contains the value returned by db

Input output parameters are created by adding the parameter twice. Once as an input parameter and then again as an output parameter. Here is an example. `client_timestamp` is an input output parameter. On input it contains current time with time zone. On output, 1 day gets added to this value.

    const string QUERY = @"
		begin
			select :client_timestamp + INTERVAL '1' DAY
			INTO :client_timestamp
			from dual;
		end;
		";

    DateTimeOffset clientTimeOriginal = DateTimeOffset.Now;
    DateTimeOffset? clientTime = clientTimeOriginal;

    var cmd = HOracleCommand.Create(QUERY)
        .Parameter("client_timestamp", clientTime)
        .OutParameter("client_timestamp",
			(DateTimeOffset? val) => clientTime = val);
    __db.ExecuteNonQuery(cmd);

    Assert.AreEqual(clientTimeOriginal.AddDays(1), clientTime);

Notice that I had to explicitly specify `DateTimeOffset?` in the lambda expression passed to `OutParameter`. I would have much preferred to be able to write:

	.OutParameter("client_timestamp", val => clientTime = val);

However this generates a compiler error.

> Error	CS0121	The call is ambiguous between the following methods or properties:
>  'HOracleCommandNonQuery.OutParameter(string, Action<DateTime?>)' and
>  'HOracleCommandNonQuery.OutParameter(string, Action<DateTimeOffset?>)'

I think this is a compiler bug. To me it is very evident that `DateTimeOffset? clientTime` should have made it clear to the compiler that `HOracleCommandNonQuery.OutParameter(string, Action<DateTimeOffset?>)` is the only reasonable choice.

If you run into ambiguity errors when specifying lambda expressions, just explicitly qualify the type of the parameter passed to the lambda expression.

When you add a parameter multiple times, the datatype of the parameter must remain the same. Failure to do this will raise an exception. Adding the same parameter is useful for two reasons.

1. Creating an Input Output Parameter
2. Changing the value of the parameter which has already been added.

# DML with RETURNING Clause #

Oracle provides the RETURNING clause which can be used with most DML queries. It is very useful to select the values in rows affected without a separate round trip to the database. You can easily take advantage of this oracle feature by using out parameters.

The values returned by the RETURNING clause are received as values of `out` parameters bound to the query.

    const string QUERY_DELETE = @"
    delete from departments
     WHERE department_id = :deptno
	RETURNING department_id, department_name
      INTO :deptno_out, :deptname_out";

    // Prepare to receive RETURNING values
    string deptName = null;
    int? deptNumber = null;

    var cmd = HOracleCommand.Create(QUERY_DELETE)
        .Parameter("deptno", 10)
        .OutParameter("deptno_out",
            val => deptNumber = val)
        .OutParameter("deptname_out",
            val => deptName = val);

    var nRows = conn.ExecuteNonQuery(cmd);
    Assert.AreEqual(1, nRows);
    Assert.AreEqual(10, deptNumber);
	// deptName will contain the department name of the deleted row

## RefCursor Parameters ##

You can receive ref cursors which are returned by Pl/SQL procedures and execute them to retrieve their data.

While reading this code extract, assume that `Department` is a class which represents a department. Also assume that `TEST_NonQuery.GetDepartmentCursor` returns a query like `SELECT department_id, department_name FROM departments WHERE ...`.

    IList<Department> list = null;

    var cmd = HOracleCommand.Create(@"
		BEGIN
			:out_cursor := TEST_NonQuery.GetDepartmentCursor;
		END;
		");
        .OutRefCursorParameter("out_cursor",
			rows => list = rows.Select(p => new Department {
				DepartmentId = row.GetInteger("department_id"),
				DepartmentName = row.GetString("department_name")
			}).ToList());

    conn.ExecuteNonQuery(cmd);

Here we are using the `OutRefCursorParameter` function of `HOracleCommandNonQuery` class whose signature is:

	public HOracleCommandNonQuery OutRefCursorParameter(string field,
		Action<IEnumerable<HOracleDataReader>> factory);

Like all out parameter functions, this function is also passed a lambda expression which is responsible for transferring the the values retrieved from the database into application variables. The passed value is a collection of rows which will be retrieved by the ref cursor if you choose to enumerate this collection. Your only choice is to enumerate this collection and store the results in your application variable. This is what this example does.

Do not attempt to store the passed value itself. It will be disposed off after the lambda expression is complete. So you will not be able to dispose it off any time later.

`HOracleDataReader` should be familiar to you. This is the same reader that you encountered during SELECT queries. So the functions to retrieve values such as `GetString()` and `GetInteger()` are same as what you used there.

If the database returns a null ref cursor, this lambda expression is not called.

Be careful about how many rows you retrieve from the ref cursor. If the ref cursor returns too many rows, your application could be in memory trouble. Always place some practical limits on the number of rows you retrieve to guard against a buggy ref cursor from memory crashing your application. Simply use the `Take()` LINQ function to limit your rows.

    .OutRefCursorParameter("out_cursor",
		rows => list = rows.Select(p => new Department {
			DepartmentId = row.GetInteger("department_id"),
			DepartmentName = row.GetString("department_name")
		}).Take(1000).ToList());

# CLOBs and BLOBs #

If you have very large LOBs and you were interested in reading and writing CLOBs or LOBs in chunks, then *WellManagedDataAccess* cannot help you. You will need to deal with ODP.NET directly. If on the other hand you simply want to read and write LOBs as one huge variable, then it is very easy to do.

*WellManagedDataAccess* does not have any specialized support built in for CLOBs. Don't think of this as a limitation. You can treat all CLOBs as strings and everything will work. Create a string parameter wherever you need to create a string parameter. Similarly, retrieve a string whenever you want to retrieve a CLOB. 

BLOBs are read and written as byte streams. Let us look at quick examples. All these examples assume that you have this table in your database.

	create table MULTIMEDIA_TAB
	(
	  thekey NUMBER(4) not null,
	  story  CLOB,
	  sound  BLOB
	)

We will start by selecting the `sound` column which is a BLOB.

    const string QUERY = @"
        select sound from multimedia_tab where thekey = 1";

    var cmd = HOracleCommand.Create(QUERY, row => row.GetBlob(0));

    var lobOut = conn.ExecuteSingle(cmd);

Here you will notice the use of the new `GetBlob()` function in class `HOracleDataReader` which has these signatures.

	public byte[] GetBlob(string fieldName)
	public byte[] GetBlob(int index)

BLOBS are returned as byte arrays. In SELECT queries, you cannot pass a BLOB as a parameter. You *can* pass BLOBs as parameter to non queries as we will demonstrate next.

This is how you receive a BLOB in an OUT parameter.

    const string QUERY = @"
		begin
			select sound into :1 from multimedia_tab where thekey = 1;
		end;
";
    byte[] blobOut = null;
    var cmd = HOracleCommand.Create(QUERY)
        .OutParameter("1", val => blobOut = val);

    conn.ExecuteNonQuery(cmd);

Here you are using the `OutParameter()` function for BLOBs whose signature is

	public HOracleCommandNonQuery OutParameter(string parameterName,
		Action<byte[]> setter)

The lambda expression needed by this `OutParameter` function is passed a byte array and you are supposed to store this array in your application variable.

Now let us see how we could pass a BLOB in an input parameter which will be useful if we wanted to insert or update a BLOB.

    const string QUERY = @"
        update multimedia_tab set sound = :lob_in where thekey = 1
            RETURNING sound into :lob_out";

    byte[] lobIn;
	// Code to populate this byte array is not shown here. A real
	// application will probably read a binary file to populate this

    var cmd = HOracleCommand.Create(QUERY)
        .Parameter("lob_in", lobIn);
    __db.ExecuteNonQuery(cmd);

Here you are using the parameter function for BLOBs whose signature is:

	public HOracleCommandNonQuery Parameter(string field, byte[] value)

Whenever you pass a byte array to the `Parameter` function, a BLOB parameter is created.

# TODO #
- Refcursor parameters (Done)

- AppInfo and Action Name
- Nested classes while retrieving data
- public void ParameterXmlArray(string field, IEnumerable<string> values)
- Proxy user
- No support for AQ
- Transactions (Done)
- DefaultMaxRows = 1000
- `OracleDataAdapter` Not supported. Oracle designed it for DataGrid. We focus on LINQ.
- TODO: Clob, LOB support (Done)
- ClientInfo
- OracleTimeStampLTZ
- OutRefCursorParameter (Done)
- Diagnostic tests for select queries
- Migrating from ODP.NET
- Getting Binaries
- End to end examples
- How it works
- How to use
- Quick start
- Why WellManagedDataAccess
- WMDA alternatives