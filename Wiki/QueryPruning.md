# Query Pruning with WellManagedDataAccess #

You already know that you can execute an SQL query with *WellManagedDataAccess*. However, in real life the query text often needs to be tweaked somewhat depending upon circumstances.

For example, you may have a UI which allows the user to filter a list of employees. The where clauses that you apply to the query will then depend upon the filters chosen by the user. Consider a function `SelectEmployees` which is supposed to return a list of employees based on filters passed.

	public IList<Employee> SelectEmployees(DateTime? hiredAfter, decimal? minSalary) {
		...
	}

How would you implement this function? The query this function needs to execute will be something like

	 SELECT employee_id, first_name, last_name, email, phone_number, 
			hire_date, salary, commission_pct
	  WHERE ...

The where clause to be applied depends upon the filters passed. You can of course write conditional query building code, but this gets messy very fast. WellManagedDataAccess allows you to write your whole query in one place.

	<query>
		 SELECT employee_id, first_name, last_name, email, phone_number, 
				hire_date, salary, commission_pct
		  WHERE 1 = 1
			<if>AND hire_date &gt;= :hire_date</if>
			<if>AND salary &gt;= :salary</if>
	</query>

The above query is quite readable and it conveys at a glance that some of the query text is conditional.

Notice at the outset that the whole query has been wrapped in the <query> xml element. This alerts the library that the query uses xml tags which need to be interpreted.

Now the query text must be valid xml. Due to this requirement we will sometimes need to encode characters which have a special meaning for xml. Here we could not write `<if>AND hire_date > :hire_date</if>` and instead had to write `<if>AND hire_date &gt;= :hire_date</if>`. Thankfully such situations are not very common.

When the library notices the &lt;if&gt; xml element, it parses the text within the tag for parameters. Within the first &lt;if&gt; element it will find the `hire_date` parameter. It will then check the bound parameter values to determine whether hire_date is non null. If it is, then the text within the &lt;if&gt; element will survive. Otherwise it will disappear from the query. This process repeats for each &lt;if&gt; element it encounters.

We can now write the complete `SelectEmployees` function.

	// Code for Employee class not shown here
	public IList<Employee> SelectEmployees(string connectStr,
		DateTime? hiredAfter, decimal? minSalary) {
            const string QUERY = @"
	<query>
	SELECT employee_id, first_name, last_name, email, phone_number,
 			hire_date, salary, commission_pct
	  FROM employees
	 WHERE 1 = 1
	<if>AND salary &gt;= :salary</if>
	<if>AND hire_date &gt;= :hire_date</if>
	</query>
";
        var binderEmployee = SqlBinder.Create(row => new Employee
        {
            EmployeeId = row.GetInteger("employee_id") ?? 0,
            CommisionPct = row.GetDecimal("commission_pct"),
            Email = row.GetString("email"),
            FirstName = row.GetString("first_name"),
            HireDate = row.GetDate("hire_date"),
            LastName = row.GetString("last_name"),
            PhoneNumber = row.GetString("phone_number"),
            Salary = row.GetDecimal("salary")
        }).Parameter("salary", minSalary)
		.Parameter("hire_date", hiredAfter);

		using (var db = new OracleDataStore(connectStr)) {
			return db.ExecuteReader(QUERY, binder);
		}
	}

Let us savor the elegance of the code. You simply passed the values you received to your intelligent xml query and the right things happen automatically. No messy conditional code for constructing the where clause.

## Deep Dive into the &lt;if&gt; Element ##

Now that we have seen what &lt;if&gt; can do for us, let us explore its full power.

You can explicitly specify the condition to evaluate as an attribute of the &lt;if&gt; element. `<if c='condition'>some text</if>`. We will get to the syntax of `conditon` in a moment. The point is that if this condition evaluates to true then the text within the &lt;if&gt; element survives, otherwise it does not. If you do not specify any condition, then the default condition is implied which is true if all the parameters references within the text have been bound with a non null value.

In other words,

	<if>AND salary >= :salary</if>

is equivalent to 

	<if c='$salary'>AND salary >= :salary</if>

If there are multiple parameters references within the &lt;if&gt; element, then they all must be non null for the condition to evaluate to true.

	<if>AND (salary >= :salary or hire_date >= :hire_date)</if>

is equivalent to

	<if c='$salary and $hire_date'>
		AND (salary >= :salary or hire_date >= :hire_date)
	</if>

Nested &lt;if&gt; elements are supported too. When an &lt;if> element contains other elements, it must explicityly specify the condition. Here is an example

	<if c='$salary and $hire_date'>
		AND (salary >= :salary or hire_date >= :hire_date
		<if c='$IncludeNullSalary'>
			OR salary is null
		</if>
		)
	</if>


The general format of the &lt;if> element is

	<if c="$condition">...</if>
	<elsif c="$condition">...</elsif>
	<elsif c="$condition">...</elsif>
	<else>...</else>

Next we discuss the &lt;else> and &lt;elsif> elements.

## &lt;else> and &lt;elsif> Elements ##

You can use the &lt;else&gt; clause in conjunction with &lt;if&gt;. In the following query we apply a default where clause if `salary` is null.

	<query>
		SELECT employee_id, first_name, last_name, email, phone_number,
	 			hire_date, salary, commission_pct
		  FROM employees
		 WHERE
		<if>AND salary &gt;= :salary</if>
		<else>
			rownum &lt; 20
		</else>
	</query>

The &lt;else> element must appear immediately after the &lt;if> element. Its text survives only if the immediately preceding &lt;if> and &lt;elsif> do not survive.

&lt;elsif> works the same as &lt;else> except that it allows you to specify a condition to check. The condition can be specified explicitly in the `c` attribute of &lt;elsif> or it can be deduced automatically using the same rules which apply for constructing the default condition of the &lt;if> element.

## Condition Expression Syntax ##

You can specify a condition within the &lt;if> and &lt;elsif> elments.

	<if c='condition'>...</if>
	<elsif c='condition'>...</elsif>

This condition can refer to the any bind parameter of the query by prefixing the parameter name with a $ symbol. The name is case insensitive. Again, an example is the best way of explaining this. Assume that our binder has these parameters bound to it.

	var binder = SqlBinder.Create()
				.Parameter("str1", "1")
				.Parameter("strNull", "")
				.Parameter("int1", 1)
				.Parameter("intNull", (int?) null)
				.Parameter("int0", 0);

Given this, all of the following are valid conditions. Null values are always treated as false. Non null values are treated as true. For integer values, 0 is treated as false as well.

- `<if c='$str1'>...</if>`. True because str1 is not empty or null
- `<if c='$strNull'>...</if>`. False because strNull is null.
- `<if c='$int1'>...</if>`. True because int1 is not null and not 0.
- `<if c='$intNull'>...</if>`. False because intNull is null.
- `<if c='$int0'>...</if>`. False because int0 is 0.

You can also use comparison operators =, !=, <, and > to build the condition.

- `<if c='$str1 = "1"'>...</if>`. True because str1 is 1. Notice that you need to use quotes for constant string "1".
- `<if c='$str1 != "1"'>...</if>`. False because '$str1 equals 1
- `<if c='$int1 = 1'>...</if>`. True because int1 is 1.
- `<if c='$int1 = "1"'>...</if>`. True because int1 is 1. Here we are comparing `int1` to the string `1` but this is allowed. Comparisons are loosely typed.
- `<if c='$str1 = 1'>...</if>`. True even though we compared string `str1` to number 1.
- - `<if c='$str1 = $int1'>...</if>`. True because the values of int1 and str1 are same, even though their data types are different.

Logical operators `and`, `or` and `not` are fully supported.

- `<if c='$str1 = "1" and $int0'>...</if>`. False because `$int0` is false.
- `<if c='$str1 = "1" and ($int0 or $str1)'>...</if>`. True. Demonstrates use of parenthesis. Nested parenthesis are fully supported.
- `<if c='not($str1 = "1") and ($int0 or not($str1))'>...</if>`. Any condition can be wrapped in `not()` to invert its truth value.

.NET Framework provides an engine for evaluating XPath expressions. We have used that engine to provide expression evaluation.


