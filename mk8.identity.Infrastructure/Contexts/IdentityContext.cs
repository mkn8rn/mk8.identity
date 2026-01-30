using Microsoft.EntityFrameworkCore;
using mk8.identity.Infrastructure.Models.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace mk8.identity.Infrastructure.Contexts
{
    public class IdentityContext : DbContext
    {
        public IdentityContext(DbContextOptions<IdentityContext> options) : base(options)
        {
        }

        public DbSet<UserDB> Users => Set<UserDB>();
        public DbSet<RoleDB> Roles => Set<RoleDB>();
        public DbSet<UserRoleDB> UserRoles => Set<UserRoleDB>();
        public DbSet<AccessTokenDB> AccessTokens => Set<AccessTokenDB>();
        public DbSet<RefreshTokenDB> RefreshTokens => Set<RefreshTokenDB>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User configuration
            modelBuilder.Entity<UserDB>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Username).IsUnique();
                entity.Property(e => e.Username).HasMaxLength(50).IsRequired();
                entity.Property(e => e.PasswordHash).HasMaxLength(255).IsRequired();
                entity.Property(e => e.PasswordSalt).HasMaxLength(255).IsRequired();
            });

            // Role configuration
            modelBuilder.Entity<RoleDB>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.RoleName).IsUnique();
            });

            // UserRole (many-to-many join table)
            modelBuilder.Entity<UserRoleDB>(entity =>
            {
                entity.HasKey(e => new { e.UserId, e.RoleId });

                entity.HasOne(e => e.User)
                    .WithMany(u => u.UserRoles)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Role)
                    .WithMany(r => r.UserRoles)
                    .HasForeignKey(e => e.RoleId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // AccessToken configuration
            modelBuilder.Entity<AccessTokenDB>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Token).IsUnique();
                entity.Property(e => e.Token).HasMaxLength(512).IsRequired();

                entity.HasOne(e => e.User)
                    .WithMany(u => u.AccessTokens)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.RefreshToken)
                    .WithMany(r => r.AccessTokens)
                    .HasForeignKey(e => e.RefreshTokenId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // RefreshToken configuration
            modelBuilder.Entity<RefreshTokenDB>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Token).IsUnique();
                entity.Property(e => e.Token).HasMaxLength(512).IsRequired();

                entity.HasOne(e => e.User)
                    .WithMany(u => u.RefreshTokens)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Seed default roles
            modelBuilder.Entity<RoleDB>().HasData(
                new RoleDB { Id = Guid.Parse("00000000-0000-0000-0000-000000000001"), RoleName = RoleType.Administrator },
                new RoleDB { Id = Guid.Parse("00000000-0000-0000-0000-000000000002"), RoleName = RoleType.Assessor },
                new RoleDB { Id = Guid.Parse("00000000-0000-0000-0000-000000000003"), RoleName = RoleType.Moderator },
                new RoleDB { Id = Guid.Parse("00000000-0000-0000-0000-000000000004"), RoleName = RoleType.Support }
            );
        }
    }
}
