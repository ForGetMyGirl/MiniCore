using AuthenticationServer.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AuthenticationServer.Migrations;

/// <summary>
/// 创建 AuthenticationServer 独占账号表；应用不会自动执行该迁移。
/// </summary>
[DbContext(typeof(AuthenticationDbContext))]
[Migration("202608150001_InitialAccounts")]
public sealed partial class InitialAccounts : Migration
{
    #region Override 重写实现

    /// <summary>
    /// 创建账号表和唯一索引。
    /// </summary>
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "accounts",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                Account = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                PlayerName = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                PasswordSalt = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                PasswordHash = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_accounts", item => item.Id));
        migrationBuilder.CreateIndex("IX_accounts_Account", "accounts", "Account", unique: true);
        migrationBuilder.CreateIndex("IX_accounts_PlayerName", "accounts", "PlayerName", unique: true);
    }

    /// <summary>
    /// 删除账号表。
    /// </summary>
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("accounts");
    }

    #endregion
}
