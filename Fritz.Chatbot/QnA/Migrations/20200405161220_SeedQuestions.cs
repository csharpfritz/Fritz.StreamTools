using Microsoft.EntityFrameworkCore.Migrations;

namespace Fritz.Chatbot.QnA.Migrations
{
    public partial class SeedQuestions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "QnAPairs",
                columns: new[] { "Id", "AnswerText", "QuestionText" },
                values: new object[,]
                {
                    { 1, "Jeff speaks English, with a Mid-Atlantic / Philadelphia accent.", "What language is Jeff speaking?" },
                    { 19, "The Live Coders is a Twitch stream team that Jeff founded and comprised of folks that write code and answer questions about technology.You can learn more about them at livecoders.dev", "Who are the Live Coders?" },
                    { 18, "Madrinas is a sponsor of the Fritz and Friends channel.They make organic, free trade coffee that you can get from madrinascoffee.com.Use the coupon code 'FRITZ' for 20 % off your order.", "What is Madrinas Coffee?" },
                    { 17, "That is Carnac from the Code52 project: Code52 / carnac", "What tool displays your keystrokes?" },
                    { 16, "The architecture workshop is available as a playlist on YouTube at: youtube.com/watch?v=k8cZUW4MS3I&list=PLVMqA0_8O85x-aurj1KphxUeWTeTlYkGM", "Where can I watch the Architecture workshop?" },
                    { 15, "The C# workshop is available as a playlist on YouTube at: youtube.com/watch?v=9ZmZuUSqQUM&list=PLVMqA0_8O85zIiU-T5h6rn8ortqEUNCeK", "Where can I watch the C# workshop?" },
                    { 14, "Jeff streams regularly on Tuesday, Wednesday, Thursday, Friday, and Sunday at 10am ET.", "When does Jeff stream?" },
                    { 13, "Jeff broadcasts with a Dell Precision Tower 3620 that has a Geforce GTX 1060 video card", "What is the machine you are using?" },
                    { 12, "The workshop is at youtube.com/watch?v=--lYHxrsLsc", "Where can I watch the 8 - hour ASP.NET Core workshop?" },
                    { 20, "The first Live Coders Conference is April 9, 2020 starting at 9a ET / 6a PT / 1300 UTC.You can learn more at conf.livecoders.dev", "When is the Live Coders Conference ?" },
                    { 11, "All of Jeff's live stream videos are archived on YouTube at: youtube.com/csharpfritz", "Where can I catch Fritz videos?" },
                    { 9, "You can find the source code shared on stream at @csharpfritz", "Where is Jeff's GitHub?" },
                    { 8, "Jeff blogs at: www.jeffreyfritz.com", "Where can I find Jeff's blog?" },
                    { 7, "No one knows the real answer to this question, because his wife keeps discarding a different one each month and doesn't tell him. Just ask about his Philly.NET hat...", "How many hats does Jeff own?" },
                    { 6, "Powershell with the posh-git plugin from dahlbyk / posh - git gives extra insight into Git repositories. Information on the prompt and tab-completion of git commands are just some of the cool features of posh-git.", "Why does Jeff use Powershell to work with Git?" },
                    { 5, "Jeff typically writes code in C# with ASP.NET Core. You will also find him regularly writing JavaScript, TypeScript, CSS, and HTML.", "What language is Jeff coding in?" },
                    { 4, "The music comes from Carl Franklin's Music to Code By at http://mtcb.pwop.com and can also be found on the mobile app Music To Flow By that you can get at http://musictoflowby.com", "What music is playing?" },
                    { 3, "Jeff uses the Visual Studio Enterprise Edition, in preview mode. The preview is ALWAYS free to try: www.visualstudio.com / vs / preview /", "Which VS version do you use?" },
                    { 2, "Jeff typically uses Visual Studio 2019 Enterprise edition available at visualstudio.com, and sometimes uses Visual Studio Code from code.visualstudio.com", "What editor does Jeff use?" },
                    { 10, "Jeff has videos on WintellectNow -http://wintellectnow.com", "Where can I find training videos from Jeff?" },
                    { 21, "Jeff uses a Vortex Race 3 with Cherry MX Blue switches, details on his blog at: jeffreyfritz.com/2018/07/mechanical-keyboards-i-just-got-one-and-why-you-need-one-too", "What keyboard are you using?" }
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "QnAPairs",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "QnAPairs",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "QnAPairs",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "QnAPairs",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "QnAPairs",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "QnAPairs",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "QnAPairs",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "QnAPairs",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "QnAPairs",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "QnAPairs",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "QnAPairs",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "QnAPairs",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "QnAPairs",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "QnAPairs",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "QnAPairs",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "QnAPairs",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "QnAPairs",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "QnAPairs",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "QnAPairs",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "QnAPairs",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "QnAPairs",
                keyColumn: "Id",
                keyValue: 21);
        }
    }
}
