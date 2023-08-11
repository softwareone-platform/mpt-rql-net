#pragma warning disable IDE0130
namespace SoftwareOne.Rql
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
    public class RqlMemberAttribute : Attribute
    {
        private RqlAction? _actions;

        public RqlMemberAttribute()
        {
        }

        public RqlMemberAttribute(RqlAction flags)
        {
            Actions = flags;
        }

        public RqlAction Actions
        {
            get
            {
                return _actions ?? RqlAction.None;
            }
            set
            {
                _actions = value;
                ActionsSet = true;
            }
        }

        public bool ActionsSet { get; private set; }

        public bool IsDefault { get; set; }
    }
}
