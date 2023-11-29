#pragma warning disable IDE0130
using SoftwareOne.Rql.Abstractions.Exception;

namespace SoftwareOne.Rql
{
    [AttributeUsage(AttributeTargets.Property)]
    public class RqlPropertyAttribute : Attribute
    {
        private RqlActions? _actionFlags;
        private RqlOperators? _operatorFlags;
        private RqlSelectMode? _select;

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

        public RqlSelectMode Select
        {
            get => _select ?? RqlSelectMode.None;
            set
            {
                _select = value;
                SelectSet = true;
            }
        }

        public bool ActionsSet { get; private set; }
        
        public bool OperatorsSet { get; private set; }
        
        public bool SelectSet { get; private set; }

        public bool IsCore { get; set; }
        
        /// <summary>
        /// Specifies that property can be null
        /// </summary>
        public bool IsNullable { get; set; }

        /// <summary>
        /// Type used to define property actions at runtime
        /// Must implement <see cref="IActionStrategy">
        /// </summary>
        /// <exception cref="RqlInvalidActionStrategyException">Thrown when provided type does not implement <see cref="IActionStrategy"</exception>
        public Type? ActionStrategy { get; set; }

    }
}
