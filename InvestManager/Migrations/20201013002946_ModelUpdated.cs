using Microsoft.EntityFrameworkCore.Migrations;

namespace InvestManager.Migrations
{
    public partial class ModelUpdated : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Share_Operation_OperationId",
                table: "Share");

            migrationBuilder.DropIndex(
                name: "IX_Share_OperationId",
                table: "Share");

            migrationBuilder.DropColumn(
                name: "OperationId",
                table: "Share");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OperationId",
                table: "Share",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Share_OperationId",
                table: "Share",
                column: "OperationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Share_Operation_OperationId",
                table: "Share",
                column: "OperationId",
                principalTable: "Operation",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
