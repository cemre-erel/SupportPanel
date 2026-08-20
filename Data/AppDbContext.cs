using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SupportPanel.Constants;
using SupportPanel.Models;

namespace SupportPanel.Data
{
    public class AppDbContext : DbContext
    {
        public int CurrentTenantId { get; set; }
        public bool IsSystemAdmin { get; set; }
        public AppDbContext(DbContextOptions<AppDbContext> options, IHttpContextAccessor? httpContextAccessor = null)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.CreatedByUser)
                .WithMany(u => u.CreatedTickets)
                .HasForeignKey(t => t.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.AssignedUser)
                .WithMany(u => u.AssignedTickets)
                .HasForeignKey(t => t.AssignedUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasOne(u => u.Tenant)
                .WithMany(t => t.Users)
                .HasForeignKey(u => u.TenantId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PasswordResetToken>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PasswordResetToken>()
                .HasIndex(p => p.TokenHash)
                .IsUnique();

            modelBuilder.Entity<TicketAttachment>()
                .HasOne(a => a.UploadedByUser)
                .WithMany(u => u.UploadedAttachments)
                .HasForeignKey(a => a.UploadedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TicketHistory>()
                .HasOne(h => h.User)
                .WithMany(u => u.TicketHistory)
                .HasForeignKey(h => h.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Tenant)
                .WithMany(tn => tn.Tickets)
                .HasForeignKey(t => t.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Product)
                .WithMany(p => p.Tickets)
                .HasForeignKey(t => t.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.SlaLevel)
                .WithMany()
                .HasForeignKey(t => t.SlaLevelId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TicketHistory>()
                .HasOne(h => h.Ticket)
                .WithMany(t => t.History)
                .HasForeignKey(h => h.TicketId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SlaLevel>()
                .HasOne(s => s.Tenant)
                .WithMany(t => t.SlaLevels)
                .HasForeignKey(s => s.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SlaPause>()
                .HasOne(p => p.Ticket)
                .WithMany(t => t.SlaPauses)
                .HasForeignKey(p => p.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.Ticket)
                .WithMany()
                .HasForeignKey(n => n.TicketId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TenantProduct>()
                .HasOne(tp => tp.Tenant)
                .WithMany(t => t.TenantProducts)
                .HasForeignKey(tp => tp.TenantId);

            modelBuilder.Entity<TenantProduct>()
                .HasOne(tp => tp.Product)
                .WithMany(p => p.TenantProducts)
                .HasForeignKey(tp => tp.ProductId);

            modelBuilder.Entity<UserProduct>()
                .HasOne(up => up.User)
                .WithMany(u => u.UserProducts)
                .HasForeignKey(up => up.UserId);

            modelBuilder.Entity<UserProduct>()
                .HasOne(up => up.Product)
                .WithMany(p => p.UserProducts)
                .HasForeignKey(up => up.ProductId);

            modelBuilder.Entity<UserProduct>()
                .HasIndex(up => up.ProductId)
                .IsUnique()
                .HasFilter("[IsActive] = 1 AND [IsProductManager] = 1");

            // Seed Data - Roles
            modelBuilder.Entity<Role>().HasData(
                new Role
                {
                    Id = 1,
                    Name = RoleNames.SystemAdmin,
                    Description = "Sistem Yöneticisi"
                },
                new Role
                {
                    Id = 2,
                    Name = RoleNames.ProductManager,
                    Description = "Ürün Yöneticisi"
                },
                new Role
                {
                    Id = 3,
                    Name = RoleNames.SupportSpecialist,
                    Description = "Destek Uzmanı"
                },
                new Role
                {
                    Id = 4,
                    Name = RoleNames.CompanyManager,
                    Description = "Firma Yöneticisi"
                },
                new Role
                {
                    Id = 5,
                    Name = RoleNames.CompanyUser,
                    Description = "Firma Kullanıcısı"
                }

            );

            // Seed Data - Categories
            modelBuilder.Entity<Category>().HasData(
                new Category
                {
                    Id = 1,
                    Name = "Teknik Destek",
                    Description = "Teknik destek talepleri"
                },
                new Category
                {
                    Id = 2,
                    Name = "Hata Bildirimi",
                    Description = "Yazılım hataları"
                },
                new Category
                {
                    Id = 3,
                    Name = "Talep",
                    Description = "Yeni özellik talepleri"
                },
                new Category
                {
                    Id = 4,
                    Name = "Geliştirme",
                    Description = "Geliştirme istekleri"
                }
            );
            modelBuilder.Entity<Ticket>().HasQueryFilter(t =>
                IsSystemAdmin || t.TenantId == CurrentTenantId);

            modelBuilder.Entity<User>().HasQueryFilter(u =>
                IsSystemAdmin || u.TenantId == CurrentTenantId);

            modelBuilder.Entity<SlaLevel>().HasQueryFilter(s =>
                IsSystemAdmin || s.TenantId == CurrentTenantId);
        }
        public DbSet<Tenant> Tenants { get; set; }

        public DbSet<Ticket> Tickets { get; set; }

        public DbSet<Product> Products { get; set; }

        public DbSet<User> Users { get; set; }

        public DbSet<TicketComment> TicketComments { get; set; }

        public DbSet<TicketAttachment> TicketAttachments { get; set; }

        public DbSet<TicketHistory> TicketHistories { get; set; }

        public DbSet<TenantProduct> TenantProducts { get; set; }

        public DbSet<UserProduct> UserProducts { get; set; }

        public DbSet<Category> Categories { get; set; }

        public DbSet<Role> Roles { get; set; }

        public DbSet<SlaLevel> SlaLevels { get; set; }

        public DbSet<SlaPause> SlaPauses { get; set; }

        public DbSet<Notification> Notifications { get; set; }

        public DbSet<EmailSetting> EmailSettings { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
    }
}
