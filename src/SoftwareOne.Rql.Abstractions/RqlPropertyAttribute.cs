#pragma warning disable IDE0130
namespace SoftwareOne.Rql
{
    [AttributeUsage(AttributeTargets.Property)]
    public class RqlPropertyAttribute : Attribute
    {
        private RqlActions? _actionFlags;
        private RqlOperators? _operatorFlags;

        public RqlPropertyAttribute()
        {
        }

        public RqlPropertyAttribute(RqlActions actionFlags)
        {
            Actions = actionFlags;
        }

        public RqlActions Actions
        {
            get => _actionFlags ?? RqlActions.None;
            set
            {
                _actionFlags = value;
                ActionsSet = true;
            }
        }

        public RqlOperators Operators
        {
            get => _operatorFlags ?? RqlOperators.None;
            set
            {
                _operatorFlags = value;
                OperatorsSet = true;
            }
        }

        public bool ActionsSet { get; private set; }
        public bool OperatorsSet { get; private set; }

        public bool IsCore { get; set; }
        public bool IsHidden { get; set; }
    }
}
