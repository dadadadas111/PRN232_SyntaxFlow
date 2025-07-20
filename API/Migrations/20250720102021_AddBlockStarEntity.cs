using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class AddBlockStarEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BlockStars",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BlockId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlockStars", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BlockStars_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BlockStars_Blocks_BlockId",
                        column: x => x.BlockId,
                        principalTable: "Blocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BlockStars_BlockId",
                table: "BlockStars",
                column: "BlockId");

            migrationBuilder.CreateIndex(
                name: "IX_BlockStars_BlockId_UserId",
                table: "BlockStars",
                columns: new[] { "BlockId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BlockStars_CreatedAt",
                table: "BlockStars",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_BlockStars_UserId",
                table: "BlockStars",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BlockStars");
        }
    }
}
