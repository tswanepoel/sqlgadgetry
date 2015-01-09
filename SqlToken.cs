using System.Diagnostics;

namespace SqlGadgetry
{
    [DebuggerDisplay("Type = {Type}, Text = {Text}")]
    public class SqlToken
    {
        private readonly SqlTokenType _type;
        private readonly string _text;

        public SqlToken(SqlTokenType type, string text)
        {
            _type = type;
            _text = text;
        }

        public SqlTokenType Type
        {
            get { return _type; }
        }

        public string Text
        {
            get { return _text; }
        }
    }
}
