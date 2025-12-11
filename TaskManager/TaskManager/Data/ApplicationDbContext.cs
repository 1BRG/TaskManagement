using System.Collections.Generic;
using System.Reflection.Emit;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TaskManager.Models;

namespace TaskManager.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Project> Projects { get; set; }
        public DbSet<AppTask> AppTasks { get; set; }
        public DbSet<ProjectMember> ProjectMembers { get; set; }
        public DbSet<Comment> Comments { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // 1. Configurare Cheie Compusă pentru ProjectMember (M-N)
            builder.Entity<ProjectMember>()
                .HasKey(pm => new { pm.ProjectId, pm.UserId });

            // 2. Relatia Project - Members
            builder.Entity<ProjectMember>()
                .HasOne(pm => pm.Project)
                .WithMany(p => p.Members)
                .HasForeignKey(pm => pm.ProjectId)
                .OnDelete(DeleteBehavior.Cascade); // Dacă șterg proiectul, șterg și asocierile membrilor

            builder.Entity<ProjectMember>()
                .HasOne(pm => pm.User)
                .WithMany(u => u.ProjectsJoined)
                .HasForeignKey(pm => pm.UserId)
                .OnDelete(DeleteBehavior.Restrict); // Dacă șterg userul, nu șterg proiectul automat, doar asocierea (necesită logică extra sau Cascade atent)

            // 3. Relatia Project - Organizer
            builder.Entity<Project>()
                .HasOne(p => p.Organizer)
                .WithMany(u => u.OwnedProjects)
                .HasForeignKey(p => p.OrganizerId)
                .OnDelete(DeleteBehavior.Restrict); // Important: Restrict pentru a evita cicluri la ștergere cu Identity

            // 4. Relatia Task - Assigned User
            builder.Entity<AppTask>()
                .HasOne(t => t.AssignedToUser)
                .WithMany(u => u.AssignedTasks)
                .HasForeignKey(t => t.AssignedToUserId)
                .OnDelete(DeleteBehavior.SetNull); // Dacă membrul pleacă, task-ul rămâne neasignat
        }
    }
}