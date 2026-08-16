using DatabaseServer.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DatabaseServer.Migrations;

/// <summary>
/// 创建首条 LoadPlayerData/SavePlayerData 链路使用的玩家表；应用不会自动执行该迁移。
/// </summary>
[DbContext(typeof(GameDbContext))]
[Migration("202608150001_InitialPlayerData")]
public sealed partial class InitialPlayerData : Migration
{
    #region Override 重写实现

    /// <summary>
    /// 创建玩家数据表。
    /// </summary>
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "player_data",
            columns: table => new
            {
                PlayerId = table.Column<long>(type: "bigint", nullable: false),
                PlayerName = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                Revision = table.Column<long>(type: "bigint", nullable: false),
                Payload = table.Column<byte[]>(type: "longblob", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_player_data", item => item.PlayerId));
    }

    /// <summary>
    /// 删除玩家数据表。
    /// </summary>
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("player_data");
    }

    #endregion
}
