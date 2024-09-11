using Microsoft.EntityFrameworkCore;
using MiniProject_GMD.Models;
using System;

namespace MiniProject_GMD.Context
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {

        }
        public DbSet<User> Users { get; set; }
        protected override void OnModelCreating(ModelBuilder builder)
        { 
        }

    }
}
