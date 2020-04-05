using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Fritz.Chatbot.QnA.Migrations
{
    public partial class Initial_QnA : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QnAPairs",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    QuestionText = table.Column<string>(maxLength: 280, nullable: false),
                    AnswerText = table.Column<string>(maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QnAPairs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UnansweredQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AskedDateStamp = table.Column<DateTime>(nullable: false),
                    QuestionText = table.Column<string>(maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnansweredQuestions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AlternateQuestion",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    QuestionId = table.Column<int>(nullable: false),
                    QuestionText = table.Column<string>(maxLength: 280, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlternateQuestion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlternateQuestion_QnAPairs_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "QnAPairs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlternateQuestion_QuestionId",
                table: "AlternateQuestion",
                column: "QuestionId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlternateQuestion");

            migrationBuilder.DropTable(
                name: "UnansweredQuestions");

            migrationBuilder.DropTable(
                name: "QnAPairs");
        }
    }
}
