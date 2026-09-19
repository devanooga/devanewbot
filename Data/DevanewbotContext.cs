namespace devanewbot.Data;

using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using devanewbot.Data.Models;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class DevanewbotContext(DbContextOptions<DevanewbotContext> options)
    : IdentityDbContext<User, Role, Guid>(options), IDataProtectionKeyContext
{
    public DbSet<ChannelBan> ChannelBans => Set<ChannelBan>();
    public DbSet<Invite> Invites => Set<Invite>();
    public DbSet<HamWatch> HamWatches => Set<HamWatch>();
    public DbSet<HamSpot> HamSpots => Set<HamSpot>();
    public DbSet<HamSpotSession> HamSpotSessions => Set<HamSpotSession>();
    public DbSet<HamCallsignLocation> HamCallsignLocations => Set<HamCallsignLocation>();
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        builder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampCreatedAt();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        StampCreatedAt();
        return base.SaveChanges();
    }

    private void StampCreatedAt()
    {
        foreach (var entry in ChangeTracker.Entries<ICreateable>())
        {
            if (entry.State == EntityState.Added && entry.Entity.CreatedAt == default)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
            }
        }
    }
}
