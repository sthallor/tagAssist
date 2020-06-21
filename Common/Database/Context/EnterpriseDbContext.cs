using System.Data.Entity;

namespace Common.Database.Context
{
    public class EnterpriseDbContext : DbContext
    {
        public EnterpriseDbContext() : base("name=EnterpriseDbContext")
        {
            Database.CommandTimeout = 600;
        }
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
        }
    }
}