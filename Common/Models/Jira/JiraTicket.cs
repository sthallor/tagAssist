using System;

namespace Common.Models.Jira
{
    public class JiraTicket
    {
        public string Id { get; set; }
        public string Key { get; set; }
        public string EgnNumber { get; set; }
        public string RigNumber { get; set; }
        public string Summary { get; set; }
        public string Assignee { get; set; }
        public DateTime? Created { get; set; }
        public DateTime? Updated { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public override string ToString()
        {
            return $"Id:{Id}, Key:{Key}, EGN:{EgnNumber}, Rig:{RigNumber}, Assignee:{Assignee}, Created:{Created:yyyy-MM-dd}, Updated:{Updated:yyyy-MM-dd} - {Summary}";
        }
    }
}