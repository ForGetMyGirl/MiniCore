using DatabaseServer.Models;
using Microsoft.EntityFrameworkCore;

namespace DatabaseServer.Data;

/// <summary>
/// 每个数据库 RPC 独立创建的游戏数据上下文。
/// </summary>
public sealed class GameDbContext : DbContext
{
    #region Public 公共成员

    /// <summary>
    /// 创建游戏数据库上下文。
    /// </summary>
    public GameDbContext(DbContextOptions<GameDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// 获取玩家数据集合。
    /// </summary>
    public DbSet<PlayerDataEntity> Players => Set<PlayerDataEntity>();

    #endregion

    #region Override 重写实现

    /// <summary>
    /// 配置玩家主键、Revision 并发令牌和 MySQL 列类型。
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PlayerDataEntity>(entity =>
        {
            entity.ToTable("player_data");
            entity.HasKey(item => item.PlayerId);
            entity.Property(item => item.PlayerName).HasMaxLength(32).IsRequired();
            entity.Property(item => item.Revision).IsConcurrencyToken();
            entity.Property(item => item.Payload).HasColumnType("longblob").IsRequired();
        });
    }

    #endregion
}
