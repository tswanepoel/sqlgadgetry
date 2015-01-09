# SQL Gadgetry
The SQL Gadgetry project aims to demonstrate how a string of SQL text can be parsed into a lambda expression tree that, once compiled, can be run using a query provider.

This project was started after realizing how elegant the SQL syntax is for querying any IQueryable, especially in cases when queries need to be built at run-time.

In this example, the SQL query "SELECT Name FROM Customers" is parsed into a lambda expression tree, that is compiled and executed on an instance of List<Customer>.

> The SQL syntax supported by this initial commit is extremely limited, but hopefully some smart guys could get involved and add the useful stuff too. For now, only a mere select list from one table source will work.
