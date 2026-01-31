using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeeklyHabitTracker.Migrations;

public partial class InitialMotivationalQuotes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "MotivationalQuotes",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Quote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                Author = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MotivationalQuotes", x => x.Id);
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "MotivationalQuotes");
    }
}
