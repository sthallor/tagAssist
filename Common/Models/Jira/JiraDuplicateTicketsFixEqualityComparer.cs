using System.Collections.Generic;

namespace Common.Models.Jira
{
    class JiraDuplicateTicketsFixEqualityComparer : IEqualityComparer<JiraTicket>
    {
        public bool Equals(JiraTicket x, JiraTicket y)
        {
            return x.RigNumber == y.RigNumber;
        }

        public int GetHashCode(JiraTicket obj)
        {
            return obj.RigNumber.GetHashCode();
        }
    }
}