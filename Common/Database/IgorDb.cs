using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using Common.Models.Igor;

namespace Common.Database
{
    public class IgorDb : DbContext
    {
        public DbSet<ToDoList> ToDoList { get; set; }
        public DbSet<RigIgorServiceVersion> IgorVersion { get; set; }
        public DbSet<RealTimeRigStateVersion> RtrsVersion { get; set; }
        public DbSet<ResetTrial> ResetTrial { get; set; }
        protected override void OnModelCreating( DbModelBuilder dbModelBuilder)
        {
            dbModelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
        }
    }
}