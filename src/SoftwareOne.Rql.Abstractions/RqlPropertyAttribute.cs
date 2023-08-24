#pragma warning disable IDE0130
namespace SoftwareOne.Rql
{
    [AttributeUsage(AttributeTargets.Property)]
    public class RqlPropertyAttribute : Attribute
    {
        private RqlAction? _actionFlags;

        public RqlPropertyAttribute()
        {
        }

        public RqlPropertyAttribute(RqlAction actionFlags)
        {
            Actions = actionFlags;
        }

        public RqlAction Actions
        {
            get => _actionFlags ?? RqlAction.None;
            set
            {
                _actionFlags = value;
                ActionsSet = true;
            }
        }

        public bool ActionsSet { get; private set; }

        public bool IsCore { get; set; }
    }
}
