using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Newtonsoft.Json;

namespace SqlGadgetry
{
    class Program
    {
        static void Main()
        {
            const string sql = "SELECT Name FROM Customers";
            Console.WriteLine("SQL: {0}", sql);

            var parser = new SqlParser();
            LambdaExpression queryExp = parser.Parse<Context>(sql);

            var context = new Context();
            IEnumerable<dynamic> results = Execute(context, queryExp).Cast<dynamic>();

            Console.WriteLine("Results: {0}", JsonConvert.SerializeObject(results));

            Console.Write("Press any key to quit.");
            Console.ReadKey(true);
        }

        private static IQueryable Execute<TContext>(TContext context, LambdaExpression queryExp)
        {
            Delegate queryMethod = queryExp.Compile();
            return (IQueryable)queryMethod.DynamicInvoke(context);
        }
    }

    public class Context
    {
        private readonly List<Customer> _customerRepository = new List<Customer>
        {
            new Customer { Name = "Joe", Age = 28 },
            new Customer { Name = "Fred", Age = 28 }
        };

        public IQueryable<Customer> Customers
        {
            get { return _customerRepository.AsQueryable(); }
        }
    }

    public class Customer
    {
        public string Name { get; set; }
        public int Age { get; set; }
    }
}
