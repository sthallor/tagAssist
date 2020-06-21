using System.Data.Entity;

namespace Common.Database.Context
{
    public class ReportingDbContext : DbContext
    {
        public ReportingDbContext() : base("name=ReportingDbContext")
        {
            Database.CommandTimeout = 600;
        }
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
        }
    }
}