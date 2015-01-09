using System;
using System.IO;
using System.Text;

namespace SqlGadgetry
{
    public class SqlLexer : IDisposable
    {
        private readonly StringReader _reader;
        private readonly SqlLexerOptions _options;
        private SqlLexerState _state = SqlLexerState.None;
        private bool _disposed;

        public SqlLexer(string sql)
            : this(sql, new SqlLexerOptions { IgnoreCase = true })
        {
            _reader = new StringReader(sql);
        }

        public SqlLexer(string sql, SqlLexerOptions options)
        {
            _reader = new StringReader(sql);
            _options = options;
        }

        public SqlLexerState State
        {
            get { return _state; }
        }

        public SqlToken Next()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("SqlLexer");
            }

            switch (_state)
            {
                case SqlLexerState.None:
                    string keyword = ReadWord();

                    if (string.Equals(keyword, "SELECT", 
                        _options.IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                    {
                        _state = SqlLexerState.SelectList;
                        return new SqlToken(SqlTokenType.SelectKeyword, keyword);
                    }

                    throw new NotSupportedException();

                case SqlLexerState.SelectList:
                    string column = ReadColumnTable();

                    if (string.Equals(column, "FROM",
                        _options.IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                    {
                        _state = SqlLexerState.TableSource;
                        return new SqlToken(SqlTokenType.FromKeyword, column);
                    }

                    return new SqlToken(SqlTokenType.SelectListColumn, column);

                case SqlLexerState.TableSource:
                    string table = ReadColumnTable();

                    _state = SqlLexerState.End;
                    return new SqlToken(SqlTokenType.TableSource, table);

                default:
                    return null;
            }
        }

        private string ReadColumnTable()
        {
            int ch = _reader.Peek();

            if (ch != '[')
            {
                while ((ch = _reader.Peek()) != -1 && "\r\n\t ,".IndexOf((char)ch) != -1)
                {
                    _reader.Read();
                }

                return ReadWord();
            }

            var sb = new StringBuilder();

            while ((ch = _reader.Read()) != -1 && (char)ch != ']')
            {
                sb.Append((char)ch);
            }

            while ((ch = _reader.Peek()) != -1 && "\r\n\t ,".IndexOf((char)ch) != -1)
            {
                _reader.Read();
            }

            return sb.ToString();
        }

        private string ReadWord()
        {
            var sb = new StringBuilder();
            int ch;

            while ((ch = _reader.Read()) != -1 && "\r\n\t ,".IndexOf((char)ch) == -1)
            {
                sb.Append((char)ch);
            }

            return sb.ToString();
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _reader.Dispose();
                _disposed = true;
            }
        }
    }
}
