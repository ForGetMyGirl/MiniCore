using DatabaseServer.Data;
using DatabaseServer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace DatabaseServer.Migrations;

/// <summary>
/// 保存游戏数据库当前迁移模型，供后续 EF Core 迁移计算差异。
/// </summary>
[DbContext(typeof(GameDbContext))]
public sealed class GameDbContextModelSnapshot : ModelSnapshot
{
    #region Override 重写实现

    /// <summary>
    /// 构建与 InitialPlayerData 对应的玩家数据模型快照。
    /// </summary>
    /// <param name="modelBuilder">EF Core 模型构建器。</param>
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "9.0.0");
        modelBuilder.Entity<PlayerDataEntity>(entity =>
        {
            entity.Property<long>(nameof(PlayerDataEntity.PlayerId)).HasColumnType("bigint");
            entity.Property<string>(nameof(PlayerDataEntity.PlayerName)).IsRequired().HasMaxLength(32).HasColumnType("varchar(32)");
            entity.Property<long>(nameof(PlayerDataEntity.Revision)).IsConcurrencyToken().HasColumnType("bigint");
            entity.Property<byte[]>(nameof(PlayerDataEntity.Payload)).IsRequired().HasColumnType("longblob");
            entity.HasKey(item => item.PlayerId);
            entity.ToTable("player_data");
        });
    }

    #endregion
}
