using Contracts;
using Microsoft.EntityFrameworkCore;

namespace SupportService.Database
{
    public class TicketsContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<Comment> Comments { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(@"Server=.;Database=test;User=sa;Password=ChangeThis_Password123;");
        }
    }
}