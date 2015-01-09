using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace SqlGadgetry
{
    public class SqlParser
    {
        private readonly SqlLexerOptions _lexerOptions;

        public SqlParser()
            : this(null)
        {
        }

        public SqlParser(SqlLexerOptions lexerOptions)
        {
            _lexerOptions = lexerOptions;
        }

        public LambdaExpression Parse<TContext>(string sql)
        {
            SqlToken[] columnTokens;
            SqlToken[] tableSourceTokens;
            ParseCore(sql, out columnTokens, out tableSourceTokens);

            if (columnTokens.Length == 0)
            {
                throw new InvalidOperationException();
            }

            if (tableSourceTokens.Length == 0)
            {
                throw new InvalidOperationException();
            }

            if (tableSourceTokens.Length > 1)
            {
                throw new NotSupportedException();
            }

            return CreateQueryExpression<TContext>(columnTokens, tableSourceTokens);
        }

        private void ParseCore(string sql, out SqlToken[] columnTokens, out SqlToken[] tableSourceTokens)
        {
            var localColumnTokens = new List<SqlToken>();
            var localTableSourceTokens = new List<SqlToken>();

            using (var lexer = CreateLexer(sql))
            {
                SqlToken token;

                while ((token = lexer.Next()) != null)
                {
                    switch (lexer.State)
                    {
                        case SqlLexerState.SelectList:
                            if (token.Type == SqlTokenType.SelectKeyword)
                            {
                                continue;
                            }

                            if (token.Type == SqlTokenType.SelectListColumn)
                            {
                                localColumnTokens.Add(token);
                                continue;
                            }

                            throw new NotSupportedException();

                        case SqlLexerState.TableSource:
                            if (token.Type == SqlTokenType.FromKeyword)
                            {
                                continue;
                            }

                            throw new NotSupportedException();

                        case SqlLexerState.End:
                            if (token.Type == SqlTokenType.TableSource)
                            {
                                localTableSourceTokens.Add(token);
                                continue;
                            }

                            throw new NotSupportedException();
                    }
                }
            }

            columnTokens = localColumnTokens.ToArray();
            tableSourceTokens = localTableSourceTokens.ToArray();
        }

        private LambdaExpression CreateQueryExpression<TContext>(SqlToken[] columnTokens, SqlToken[] tableSourceTokens)
        {
            ParameterExpression contextExp = Expression.Parameter(typeof(TContext));
            MemberExpression sourceExp = Expression.PropertyOrField(contextExp, tableSourceTokens[0].Text);
            Type sourceType = sourceExp.Type.GenericTypeArguments[0];

            Type resultType = CreateDynamicAnonymousType(columnTokens.Select(columnToken => sourceType.GetProperty(columnToken.Text)));
            NewExpression newResultExp = Expression.New(resultType);
            ParameterExpression sourceParameterExp = Expression.Parameter(sourceType);

            IEnumerable<MemberBinding> resultTypeBindings = from columnToken in columnTokens
                                                            select Expression.Bind(
                                                                resultType.GetField(columnToken.Text),
                                                                Expression.MakeMemberAccess(sourceParameterExp, sourceType.GetProperty(columnToken.Text)));
            
            MemberInitExpression initExp = Expression.MemberInit(newResultExp, resultTypeBindings);
            Expression selectorExp = Expression.Lambda(initExp, sourceParameterExp);

            MethodCallExpression selectExp = Expression.Call(
                    typeof(Queryable),
                    "Select",
                    new[] { sourceType, resultType },
                    sourceExp,
                    Expression.Quote(selectorExp));

            return Expression.Lambda(selectExp, contextExp);
        }

        private static Type CreateDynamicAnonymousType(IEnumerable<PropertyInfo> prototypeProperties)
        {
            var dynamicAssemblyName = new AssemblyName("DynamicAssembly");
            AssemblyBuilder dynamicAssembly = AssemblyBuilder.DefineDynamicAssembly(dynamicAssemblyName, AssemblyBuilderAccess.Run);
            ModuleBuilder dynamicModule = dynamicAssembly.DefineDynamicModule("DynamicModule");
            TypeBuilder dynamicAnonymousType = dynamicModule.DefineType("DynamicAnonymous_" + Guid.NewGuid());

            foreach (var property in prototypeProperties)
            {
                dynamicAnonymousType.DefineField(property.Name, property.PropertyType, FieldAttributes.Public);
            }

            return dynamicAnonymousType.CreateType();
        }

        private SqlLexer CreateLexer(string sql)
        {
            return _lexerOptions != null
                ? new SqlLexer(sql, _lexerOptions)
                : new SqlLexer(sql);
        }
    }
}
