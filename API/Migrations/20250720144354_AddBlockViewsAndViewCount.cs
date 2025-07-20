using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class AddBlockViewsAndViewCount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ViewCount",
                table: "Blocks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "BlockViews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BlockId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ViewedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlockViews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BlockViews_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BlockViews_Blocks_BlockId",
                        column: x => x.BlockId,
                        principalTable: "Blocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BlockViews_BlockId",
                table: "BlockViews",
                column: "BlockId");

            migrationBuilder.CreateIndex(
                name: "IX_BlockViews_IpAddress",
                table: "BlockViews",
                column: "IpAddress");

            migrationBuilder.CreateIndex(
                name: "IX_BlockViews_UserId",
                table: "BlockViews",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_BlockViews_ViewedAt",
                table: "BlockViews",
                column: "ViewedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BlockViews");

            migrationBuilder.DropColumn(
                name: "ViewCount",
                table: "Blocks");
        }
    }
}
