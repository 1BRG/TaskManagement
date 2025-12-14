using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskManagement.Migrations
{
    /// <inheritdoc />
    public partial class RefactorToBoardColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BoardColumnId",
                table: "AppTasks",
                type: "INTEGER",
                nullable: true);



            migrationBuilder.CreateTable(
                name: "BoardColumns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    ProjectId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoardColumns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BoardColumns_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // DATA MIGRATION: Preserve existing columns
            migrationBuilder.Sql(
                @"
                INSERT INTO BoardColumns (Title, ProjectId, ""Order"")
                SELECT DISTINCT ColumnName, ProjectId, 0
                FROM AppTasks
                WHERE ColumnName IS NOT NULL AND ColumnName != '';
                ");

            migrationBuilder.Sql(
                @"
                UPDATE AppTasks
                SET BoardColumnId = (
                    SELECT Id
                    FROM BoardColumns
                    WHERE BoardColumns.Title = AppTasks.ColumnName
                      AND BoardColumns.ProjectId = AppTasks.ProjectId
                )
                WHERE ColumnName IS NOT NULL AND ColumnName != '';
                ");

            migrationBuilder.CreateIndex(
                name: "IX_AppTasks_BoardColumnId",
                table: "AppTasks",
                column: "BoardColumnId");

            migrationBuilder.CreateIndex(
                name: "IX_BoardColumns_ProjectId",
                table: "BoardColumns",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_AppTasks_BoardColumns_BoardColumnId",
                table: "AppTasks",
                column: "BoardColumnId",
                principalTable: "BoardColumns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppTasks_BoardColumns_BoardColumnId",
                table: "AppTasks");

            migrationBuilder.DropTable(
                name: "BoardColumns");

            migrationBuilder.DropIndex(
                name: "IX_AppTasks_BoardColumnId",
                table: "AppTasks");

            migrationBuilder.DropColumn(
                name: "BoardColumnId",
                table: "AppTasks");

            migrationBuilder.DropColumn(
                name: "IsCompleted",
                table: "AppTasks");

            migrationBuilder.DropColumn(
                name: "Order",
                table: "AppTasks");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "AppTasks");
        }
    }
}
