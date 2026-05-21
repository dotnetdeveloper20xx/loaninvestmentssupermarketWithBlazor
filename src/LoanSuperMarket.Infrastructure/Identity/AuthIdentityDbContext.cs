using LoanSuperMarket.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LoanSuperMarket.Infrastructure.Identity;

public sealed class AuthIdentityDbContext : IdentityDbContext<ApplicationUser, CustomRole, string>
{
    public AuthIdentityDbContext(DbContextOptions<AuthIdentityDbContext> options)
        : base(options)
    {
    }

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<RecoveryCode> RecoveryCodes => Set<RecoveryCode>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureRefreshToken(modelBuilder);
        ConfigureUserSession(modelBuilder);
        ConfigureRolePermission(modelBuilder);
        ConfigureRecoveryCode(modelBuilder);
        ConfigureApplicationUser(modelBuilder);
        ConfigureCustomRole(modelBuilder);
    }

    private static void ConfigureRefreshToken(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Token)
                .IsRequired()
                .HasMaxLength(512);

            entity.HasIndex(e => e.Token)
                .IsUnique();

            entity.Property(e => e.UserId)
                .IsRequired()
                .HasMaxLength(450);

            entity.HasIndex(e => e.UserId);

            entity.Property(e => e.ReplacedByToken)
                .HasMaxLength(512);

            entity.Property(e => e.RevokedReason)
                .HasMaxLength(256);

            entity.HasOne(e => e.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureUserSession(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.ToTable("UserSessions");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.UserId)
                .IsRequired()
                .HasMaxLength(450);

            entity.HasIndex(e => e.UserId);

            entity.Property(e => e.RefreshTokenId)
                .IsRequired()
                .HasMaxLength(450);

            entity.Property(e => e.DeviceType)
                .HasMaxLength(100);

            entity.Property(e => e.IpAddress)
                .HasMaxLength(45);

            entity.Property(e => e.Browser)
                .HasMaxLength(256);

            entity.HasOne(e => e.User)
                .WithMany(u => u.Sessions)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureRolePermission(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.ToTable("RolePermissions");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.RoleId)
                .IsRequired()
                .HasMaxLength(450);

            entity.HasIndex(e => e.RoleId);

            entity.Property(e => e.GrantedBy)
                .HasMaxLength(450);

            entity.HasOne(e => e.Role)
                .WithMany(r => r.Permissions)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureRecoveryCode(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RecoveryCode>(entity =>
        {
            entity.ToTable("RecoveryCodes");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.UserId)
                .IsRequired()
                .HasMaxLength(450);

            entity.HasIndex(e => e.UserId);

            entity.Property(e => e.Code)
                .IsRequired()
                .HasMaxLength(128);
        });
    }

    private static void ConfigureApplicationUser(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.FirstName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.LastName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.AccountStatusReason)
                .HasMaxLength(500);

            entity.Property(e => e.AccountStatusChangedBy)
                .HasMaxLength(450);

            entity.Property(e => e.BlockedActivity)
                .HasMaxLength(50);

            entity.Property(e => e.CreditLimit)
                .HasPrecision(18, 2);

            entity.Property(e => e.CapitalLimit)
                .HasPrecision(18, 2);
        });
    }

    private static void ConfigureCustomRole(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CustomRole>(entity =>
        {
            entity.Property(e => e.Description)
                .HasMaxLength(500);

            entity.Property(e => e.CreatedBy)
                .HasMaxLength(450);
        });
    }
}
