using AuthenticationServer.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthenticationServer.Data;

/// <summary>
/// AuthenticationServer 独占的账号数据库上下文。
/// </summary>
public sealed class AuthenticationDbContext : DbContext
{
    #region Public 公共成员

    /// <summary>
    /// 创建账号数据库上下文。
    /// </summary>
    public AuthenticationDbContext(DbContextOptions<AuthenticationDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// 获取账号集合。
    /// </summary>
    public DbSet<AccountEntity> Accounts => Set<AccountEntity>();

    #endregion

    #region Override 重写实现

    /// <summary>
    /// 配置账号唯一索引和长度约束。
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AccountEntity>(entity =>
        {
            entity.ToTable("accounts");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Account).HasMaxLength(64).IsRequired();
            entity.Property(item => item.PlayerName).HasMaxLength(32).IsRequired();
            entity.Property(item => item.PasswordSalt).HasMaxLength(128).IsRequired();
            entity.Property(item => item.PasswordHash).HasMaxLength(128).IsRequired();
            entity.HasIndex(item => item.Account).IsUnique();
            entity.HasIndex(item => item.PlayerName).IsUnique();
        });
    }

    #endregion
}
