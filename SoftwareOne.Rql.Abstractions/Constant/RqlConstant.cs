namespace SoftwareOne.Rql.Abstractions.Constant
{
    public class RqlConstant : RqlArgument
    {
        private readonly string _value;
        internal RqlConstant(string value)
        {
            _value = value;
        }

        public string Value => _value;
    }
}
