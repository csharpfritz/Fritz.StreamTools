using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Fritz.Chatbot.QnA.Migrations
{
    public partial class Addwronganswers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AnswerPct",
                table: "UnansweredQuestions",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AnswerTextProvided",
                table: "UnansweredQuestions",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewDate",
                table: "UnansweredQuestions",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnswerPct",
                table: "UnansweredQuestions");

            migrationBuilder.DropColumn(
                name: "AnswerTextProvided",
                table: "UnansweredQuestions");

            migrationBuilder.DropColumn(
                name: "ReviewDate",
                table: "UnansweredQuestions");
        }
    }
}
