using AuthenticationServer.Data;
using AuthenticationServer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace AuthenticationServer.Migrations;

/// <summary>
/// 保存账号数据库当前迁移模型，供后续 EF Core 迁移计算差异。
/// </summary>
[DbContext(typeof(AuthenticationDbContext))]
public sealed class AuthenticationDbContextModelSnapshot : ModelSnapshot
{
    #region Override 重写实现

    /// <summary>
    /// 构建与 InitialAccounts 对应的账号模型快照。
    /// </summary>
    /// <param name="modelBuilder">EF Core 模型构建器。</param>
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "9.0.0");
        modelBuilder.Entity<AccountEntity>(entity =>
        {
            entity.Property<long>(nameof(AccountEntity.Id))
                .ValueGeneratedOnAdd()
                .HasColumnType("bigint")
                .HasAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);
            entity.Property<string>(nameof(AccountEntity.Account)).IsRequired().HasMaxLength(64).HasColumnType("varchar(64)");
            entity.Property<string>(nameof(AccountEntity.PlayerName)).IsRequired().HasMaxLength(32).HasColumnType("varchar(32)");
            entity.Property<string>(nameof(AccountEntity.PasswordSalt)).IsRequired().HasMaxLength(128).HasColumnType("varchar(128)");
            entity.Property<string>(nameof(AccountEntity.PasswordHash)).IsRequired().HasMaxLength(128).HasColumnType("varchar(128)");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => item.Account).IsUnique();
            entity.HasIndex(item => item.PlayerName).IsUnique();
            entity.ToTable("accounts");
        });
    }

    #endregion
}
