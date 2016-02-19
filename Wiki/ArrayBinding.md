# Array Binding with WellManagedDataAccess #

Array binding is a feature provided by ODP.NET which allows you to execute DML queries and PL/SQL blocks multiple times in a single round trip to the database server. This can significantly enhance performance.

The idea is that at the time of executing the query, you provide values for each parameter in parallel arrays. The database will execute the query once for each value in the arrays. If errors are encountered, it will return an array of errors for each row for which an error was encountered.

WellManagedDataAccess library makes executing array DML as simple as executing normal DML. Let us start by looking at a code example. The function `DeleteDepartments` shown below will delete all departments whose id is passed in the array `myArrayDeptNo`.

    static void DeleteDepartments(
		string connectStr, IList<int> myArrayDeptNo)
    {
        using (var db = new OracleDatastore(connectStr))
        {
			// Step 1: Create Binder
            var binder = SqlBinder.Create(myArrayDeptNo.Count)
				.Parameter("deptno", myArrayDeptNo);

			// Step 2: Execute query
            var nRows = db.ExecuteNonQuery(@"
				delete from departments
				 where department_id = :deptno",
				binder);
        }
    }

*Step 1*. Create the binder using the overload of `Create` which accepts an integer as an argument. Specify the bind count as the argument. This overload returns a binder of type `SqlBinderDmlArray` even though you did not explicitly need to mention this name in this code. I find the `var` keyword to be extremely helpful.

	var binder = SqlBinder.Create(myArrayDeptNo.Count)

Here we specify the entire length of the passed array as the bind count. This means that all values in the array will be looked. If we wanted to delete only the first few departments, we could have specified the bind
count to be number of rows we actually wanted to delete.

Then bind parameters to this binder. You can only bind arrays as parameter values. The size of the array must be at least equal to the bind count. The type of the parameter is deduced from the datatype of the array elements.

	binder.Parameter("deptno", myArrayDeptNo)

Because `myArrayDeptNo`is a list of `int` values, the type of the parameter is deduced to be integer and this function will internally map it to an appropriate datatype. For the curious, `int` parameters are mapped to Oracle `NUMBER` type. These are the overloads of `Parameter` function available.

> `public SqlBinderDmlArray Parameter(string field, IEnumerable<string>values)`
> `public SqlBinderDmlArray Parameter(string field, IEnumerable<int> values)`
> `public SqlBinderDmlArray Parameter(string field, IEnumerable<DateTime?> values)`
> `public SqlBinderDmlArray Parameter(string field, IEnumerable<int> values)`
> `public SqlBinderDmlArray Parameter(string field, IEnumerable<int?> values)`
> `public SqlBinderDmlArray Parameter(string field, IEnumerable<long> values)`
> `public SqlBinderDmlArray Parameter(string field, IEnumerable<long?> values)`

*Step 2*. Execute the query by passing in the query text and the binder.

This is self explanatory. The database provides the `ExecuteNonQuery` which takes query text and the binder as a parameter. It returns the number of rows affected.

    var nRows = db.ExecuteNonQuery(@"
		delete from departments
		 where department_id = :deptno",
		binder);

In this example the number of rows affected will be equal to the bind count which we specified.

# Array Binding with Transaction #

Here is an extended example which uses multiple array bind parameters. We first delete departments and then insert departments, all as a single transaction.

    static void DeleteAndInsertDepartments(string connectStr,
		IList<int> myArrayDeptNo, IList<string> myArrayDeptName)
    {
        const string QUERY_DELETE = @"
			delete from departments
			 where department_id = :deptno";

        const string QUERY_INSERT = @"
			insert into departments (department_id, department_name)
    			values(:deptno, :deptname)";

        var binder = SqlBinder.Create(myArrayDeptNo.Count())
            .Parameter("deptno", myArrayDeptNo)
            .Parameter("deptname", myArrayDeptName);

        using (var db = new OracleDatastore(connectStr))
        {
            using (var trans = db.BeginTransaction())
            {
                db.ExecuteNonQuery(QUERY_DELETE, binder);
                db.ExecuteNonQuery(QUERY_INSERT, binder);
                trans.Commit();
            }
        }
    }

Notice above that we have used the same `binder` for both `QUERY_DELETE` and `QUERY_INSERT`. Even though `binder` contains parameter values for parameter `deptname` which is not needed by `QUERY_DELETE`, this is quite all right with `db.ExecuteNonQuery(QUERY_DELETE, binder)`. While executing a query, we only look at parameter values which are needed. Extra parameter values are ignored. However, as you would expect, error is raised for missing parameter values.

## RETURNING with Array Binding ##

The following example demonstrates how to return values while executing a `delete` sql statement. Since we are using array binding here, so an array of values will be returned, one for each row deleted.

    static void DeleteDepartments(
		string connectStr, IList<int> myArrayDeptNo)
    {
        const string QUERY_DELETE = @"
            delete from departments
             where department_id = :deptno
         RETURNING department_id, department_name
              INTO :deptno_out, :deptname_out";

		// Prepare to receive RETURNING values
        IList<string> deptNames = null;
        IList<int> deptNumbers = null;

        var binder = SqlBinder.Create(myArrayDeptNo.Count)
			.Parameter("deptno", myArrayDeptNo)
            .OutParameter("deptno_out",
				values => deptNumbers = values.ToList())
            .OutParameter("deptname_out",
				values => deptNames = values.ToList());

        using (var db = new OracleDatastore(connectStr))
        {
            var nRows = db.ExecuteNonQuery(QUERY_DELETE, binder);
			// Now deptNames and deptNumbers will contain a list of values deleted.
			// The count of each list should be the same as nRows.
			for (var i = 0; i < nRows; ++i) {
				// Deleted department_id = deptNumbers[i], 
				// department_name = deptNames[i]
				// Do something with these values
			}
        }
    }

The query

	    delete from departments
	     where department_id = :deptno
	 RETURNING department_id, department_name
	      INTO :deptno_out, :deptname_out

includes a `RETURNING` clause which specifies that the returned values should be returned via out parameters `deptno_out` and `deptname_out`. We declare list variables which will receive these values and then bind out parameters to the binder.

    var binder = SqlBinder.Create(myArrayDeptNo.Count)
		.Parameter("deptno", myArrayDeptNo)
        .OutParameter("deptno_out",
			values => deptNumbers = values.ToList())
        .OutParameter("deptname_out",
			values => deptNames = values.ToList());

Each of these out parameters takes a lambda expression which is passed the values retrieved from the database. The responsibility of the lambda expression is to store these values in application variables. Here we store them in lists `deptNumbers` and `deptNames` respectively. The type of the list which we use for storing the values tells the `binder` the data type of the returned values so that it can prepare the underlying ODP.NET calls accordingly.

The binder here is of type `SqlBinderDmlArray` which provides overloads of the `OutParameter` for various data types.

> `public SqlBinderDmlArray OutParameter(string field, Action<IEnumerable<string>setter)`
> `public SqlBinderDmlArray OutParameter(string field, Action<IEnumerable<int>setter)`
> `public SqlBinderDmlArray OutParameter(string field, Action<IEnumerable<DateTime?>setter)`

## Array Binding RETURNING Limitations ##

Keep in mind that for array binding, the query which has the RETURNING clause should affect only a single row. In this example we are deleting based on department id, so this is guaranteed.

	    delete from departments
	     where department_id = :deptno
	 RETURNING department_id, department_name
	      INTO :deptno_out, :deptname_out

If on the other hand, the query affected multiple rows, then you will receive an error. Look at this contrived example which deletes all departments except the one with the passed id.

	    delete from departments
	     where department_id != :deptno
	 RETURNING department_id, department_name
	      INTO :deptno_out, :deptname_out

Here the RETURNING clause will need to return multiple values for each row. When executed, Oracle will return the error "ORA-24369: Required callbacks not registered for one or more bind handles". You might then be tempted to try this query:

	    delete from departments
	     where department_id != :deptno
	 RETURNING department_id, department_name
	BULK COLLECT INTO :deptno_out, :deptname_out

This does not work either. Oracle throws up with "ORA-00925: missing INTO keyword" which indicates that the BULK COLLECT syntax is not supported in DML queries.

Since you never give up, you decide to wrap this query in a PL/SQL anonymous block. You know that PL/SQL supports BULK COLLECT INTO syntax an therefore you feel confident that it should work.

	BEGIN
        delete from departments
         where department_id != :deptno
     RETURNING department_id, department_name
          BULK COLLECT INTO :deptno_out, :deptname_out;
	END;

Nice try, but now you will see the error "PLS-00497: cannot mix between single row and multi-row (BULK) in INTO list".

Perhaps there are other ways of accomplishing this task, but I do not feel so desperate. In my professional life, this limitation has never bothered me. I can execute the last query without array binding and everything will work just fine.

# Determining Rows which caused the Error #

During array binding, the same query is executed multiple times, once for each value of the parameter array. It is quite possible that some of the query invocations will succeed whereas others will fail. As an example, if you are inserting departments, some of the departments may cause unique key violations.

When you do not use transactions, none of the departments will get inserted. All the invocations of a query will be treated as one giant query and will be rolled back. If you do use transactions, then you have the ability to commit successful rows if you want.

Let us look at an example. We will insert 4 departments in the `departments` table which has a primary key on the `department_id` column. Notice that the value 6 appears twice in the `myArrayDeptNo` array. So we expect the last row to fail with a unique key exception. Our goal here is to commit the other rows anyway and ignore the last row.


    var myArrayDeptNo = new int[] { 4, 5, 6, 6 };
    var myArrayDeptName = new string[] { "d4", "d5", "d6", "d7" };

    var binder = SqlBinder.Create(myArrayDeptNo.Length)
        .Parameter("deptno", myArrayDeptNo)
        .Parameter("deptname", myArrayDeptName);

    using (var trans = __db.BeginTransaction())
    {
        try
        {
            var nRows = __db.ExecuteNonQuery(QUERY_INSERT, binder);
            Assert.Fail("Above query should have raised exception");
        }
        catch (OracleDataStoreArrayBindException ex)
        {
			// Perhaps analyze the rows which caused the error
            foreach (var item in ex.RowErrors)
            {
                // item.Key is the index of the row causing the error
                // item.Value is the Oracle error message;
            }
        }

        // Commit the successful rows
        trans.Commit();
    }

To accomplish our goal we must use a transaction and catch the `OracleDataStoreArrayBindException`. Within the exception handler we have access to the property `RowErrors` which is a dictionary. The key of this dictionary is the index of the row which caused the error. The value of the dictionary is the Oracle error message describing the error which occurred. While you could conceivably write some code here to analyze what is going on, I have never found it to be useful.

This example simply ignores the exception. Then the transaction is committed. Here we will be committing 3 rows, all except the last. If we had not bothered to catch and ignore the exception, nothing would have been committed.


