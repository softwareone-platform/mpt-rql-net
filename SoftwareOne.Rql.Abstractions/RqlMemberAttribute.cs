#pragma warning disable IDE0130
namespace SoftwareOne.Rql
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
    public class RqlMemberAttribute : Attribute
    {
        public RqlMemberAttribute(MemberFlag flags)
        {
            Flags = flags;
        }

        public MemberFlag Flags { get; set; }
    }
}
