using System.Collections.Generic;
using System.Reflection.Emit;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Models;

namespace TaskManagement.Data
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
        public DbSet<BoardColumn> BoardColumns { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // 1. Configurare Cheie Compusa pentru ProjectMember (M-N)
            builder.Entity<ProjectMember>()
                .HasKey(pm => new { pm.ProjectId, pm.UserId });

            // 2. Relatia Project - Members
            builder.Entity<ProjectMember>()
                .HasOne(pm => pm.Project)
                .WithMany(p => p.Members)
                .HasForeignKey(pm => pm.ProjectId)
                .OnDelete(DeleteBehavior.Cascade); // Daca sterg proiectul, sterg si asocierile membrilor

            builder.Entity<ProjectMember>()
                .HasOne(pm => pm.User)
                .WithMany(u => u.ProjectsJoined)
                .HasForeignKey(pm => pm.UserId)
                .OnDelete(DeleteBehavior.Restrict); // Daca sterg userul, nu sterg proiectul automat, doar asocierea (necesita logica extra sau Cascade atent)

            // 3. Relatia Project - Organizer
            builder.Entity<Project>()
                .HasOne(p => p.Organizer)
                .WithMany(u => u.OwnedProjects)
                .HasForeignKey(p => p.OrganizerId)
                .OnDelete(DeleteBehavior.Restrict); // Important: Restrict pentru a evita cicluri la stergere cu Identity

            // 4. Relatia Task - Assigned User
            builder.Entity<AppTask>()
                .HasOne(t => t.AssignedToUser)
                .WithMany(u => u.AssignedTasks)
                .HasForeignKey(t => t.AssignedToUserId)
                .OnDelete(DeleteBehavior.SetNull); // Daca membrul pleaca, task-ul ramane neasignat

             // 5. Relatia BoardColumn - Tasks
             builder.Entity<AppTask>()
                 .HasOne(t => t.BoardColumn)
                 .WithMany(c => c.Tasks)
                 .HasForeignKey(t => t.BoardColumnId)
                 .OnDelete(DeleteBehavior.Cascade); // Delete column -> delete tasks
        }
    }
}