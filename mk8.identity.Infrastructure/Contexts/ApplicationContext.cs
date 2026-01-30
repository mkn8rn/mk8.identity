using Microsoft.EntityFrameworkCore;
using mk8.identity.Infrastructure.Models.Application;
using System;
using System.Collections.Generic;
using System.Text;

namespace mk8.identity.Infrastructure.Contexts
{
    public class ApplicationContext : DbContext
    {
        public ApplicationContext(DbContextOptions<ApplicationContext> options) : base(options)
        {
        }

        public DbSet<UserMembershipDB> Memberships => Set<UserMembershipDB>();
        public DbSet<ContributionDB> Contributions => Set<ContributionDB>();
        public DbSet<PrivilegesDB> Privileges => Set<PrivilegesDB>();
        public DbSet<MatrixAccountDB> MatrixAccounts => Set<MatrixAccountDB>();
        public DbSet<MessageDB> Messages => Set<MessageDB>();
        public DbSet<NotificationDB> Notifications => Set<NotificationDB>();
        public DbSet<ContactInfoDB> ContactInfos => Set<ContactInfoDB>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // UserMembership configuration
            modelBuilder.Entity<UserMembershipDB>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.UserId).IsUnique();

                entity.Property(e => e.ActivationDates).HasColumnType("jsonb");
                entity.Property(e => e.DeactivationDates).HasColumnType("jsonb");

                entity.HasOne(e => e.Privileges)
                    .WithOne(p => p.Membership)
                    .HasForeignKey<PrivilegesDB>(p => p.MembershipId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Contribution configuration
            modelBuilder.Entity<ContributionDB>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.MembershipId, e.Month, e.Year, e.Type });
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.ExternalReference).HasMaxLength(500);

                entity.HasOne(e => e.Membership)
                    .WithMany(m => m.Contributions)
                    .HasForeignKey(e => e.MembershipId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.SubmittedBy)
                    .WithMany()
                    .HasForeignKey(e => e.SubmittedByMembershipId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.ValidatedBy)
                    .WithMany()
                    .HasForeignKey(e => e.ValidatedByMembershipId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Privileges configuration
            modelBuilder.Entity<PrivilegesDB>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.MembershipId).IsUnique();
            });

            // MatrixAccount configuration
            modelBuilder.Entity<MatrixAccountDB>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.AccountId).IsUnique();
                entity.HasIndex(e => e.Username).IsUnique();
                entity.Property(e => e.AccountId).HasMaxLength(255).IsRequired();
                entity.Property(e => e.Username).HasMaxLength(100).IsRequired();

                entity.HasOne(e => e.Privileges)
                    .WithMany(p => p.MatrixAccounts)
                    .HasForeignKey(e => e.PrivilegesId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.CreatedBy)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedByMembershipId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.DisabledBy)
                    .WithMany()
                    .HasForeignKey(e => e.DisabledByMembershipId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Message configuration
            modelBuilder.Entity<MessageDB>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.Type, e.Status });
                entity.Property(e => e.Title).HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(4000);
                entity.Property(e => e.DesiredMatrixUsername).HasMaxLength(100);
                entity.Property(e => e.TemporaryPassword).HasMaxLength(100);
                entity.Property(e => e.SpecialInstructions).HasMaxLength(1000);

                entity.HasOne(e => e.Sender)
                    .WithMany(m => m.Messages)
                    .HasForeignKey(e => e.SenderMembershipId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.HandledBy)
                    .WithMany()
                    .HasForeignKey(e => e.HandledByMembershipId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.CreatedMatrixAccount)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedMatrixAccountId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Notification configuration
            modelBuilder.Entity<NotificationDB>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.MinimumRoleRequired, e.IsRead });
                entity.HasIndex(e => e.CreatedAt);
                entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
                entity.Property(e => e.Message).HasMaxLength(2000).IsRequired();

                entity.HasOne(e => e.RelatedMembership)
                    .WithMany()
                    .HasForeignKey(e => e.RelatedMembershipId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.AssignedTo)
                    .WithMany()
                    .HasForeignKey(e => e.AssignedToMembershipId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ContactInfo configuration
            modelBuilder.Entity<ContactInfoDB>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.MembershipId).IsUnique();
                entity.Property(e => e.Email).HasMaxLength(255);
                entity.Property(e => e.Matrix).HasMaxLength(255);

                entity.HasOne(e => e.Membership)
                    .WithOne(m => m.ContactInfo)
                    .HasForeignKey<ContactInfoDB>(e => e.MembershipId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
