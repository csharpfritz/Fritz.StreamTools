using Microsoft.EntityFrameworkCore;
using System;
using System.Net.Sockets;

namespace Fritz.Chatbot.QnA.Data
{
	public class QnADbContext : DbContext
	{

		public QnADbContext(DbContextOptions<QnADbContext> options) : base(options) { }

		public DbSet<QnAPair> QnAPairs { get; set; }

		public DbSet<UnansweredQuestion> UnansweredQuestions { get; set; } 

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{

			modelBuilder.Entity<QnAPair>()
				.HasMany<AlternateQuestion>(q => q.AlternateQuestions)
				.WithOne(a => a.MainQuestion)
				.HasForeignKey(a => a.QuestionId);

			LoadSeedData(modelBuilder);

			base.OnModelCreating(modelBuilder);

		}

		private void LoadSeedData(ModelBuilder modelBuilder)
		{

			modelBuilder.Entity<QnAPair>().HasData(new QnAPair[] {

				new QnAPair() {Id=1,
					QuestionText="What language is Jeff speaking?",
					AnswerText="Jeff speaks English, with a Mid-Atlantic / Philadelphia accent."
				},
				new QnAPair() {Id=2,
					QuestionText="What editor does Jeff use?",
					AnswerText="Jeff typically uses Visual Studio 2019 Enterprise edition available at visualstudio.com, and sometimes uses Visual Studio Code from code.visualstudio.com"
				},new QnAPair() {Id=3,
					QuestionText="Which VS version do you use?",
					AnswerText="Jeff uses the Visual Studio Enterprise Edition, in preview mode. The preview is ALWAYS free to try: www.visualstudio.com / vs / preview /"
				},new QnAPair() {Id=4,
					QuestionText="What music is playing?",
					AnswerText="The music comes from Carl Franklin's Music to Code By at http://mtcb.pwop.com and can also be found on the mobile app Music To Flow By that you can get at http://musictoflowby.com"
				},new QnAPair() {Id=5,
					QuestionText="What language is Jeff coding in?",
					AnswerText="Jeff typically writes code in C# with ASP.NET Core. You will also find him regularly writing JavaScript, TypeScript, CSS, and HTML."
				},new QnAPair() {Id=6,
					QuestionText="Why does Jeff use Powershell to work with Git?",
					AnswerText="Powershell with the posh-git plugin from dahlbyk / posh - git gives extra insight into Git repositories. Information on the prompt and tab-completion of git commands are just some of the cool features of posh-git."
				},new QnAPair() {Id=7,
					QuestionText="How many hats does Jeff own?",
					AnswerText="No one knows the real answer to this question, because his wife keeps discarding a different one each month and doesn't tell him. Just ask about his Philly.NET hat..."
				},new QnAPair() {Id=8,
					QuestionText="Where can I find Jeff's blog?",
					AnswerText="Jeff blogs at: www.jeffreyfritz.com"
				},new QnAPair() {Id=9,
					QuestionText="Where is Jeff's GitHub?",
					AnswerText="You can find the source code shared on stream at @csharpfritz"
				},new QnAPair() {Id=10,
					QuestionText="Where can I find training videos from Jeff?",
					AnswerText="Jeff has videos on WintellectNow -http://wintellectnow.com"
				},new QnAPair() {Id=11,
					QuestionText="Where can I catch Fritz videos?",
					AnswerText="All of Jeff's live stream videos are archived on YouTube at: youtube.com/csharpfritz"
				},new QnAPair() {Id=12,
					QuestionText="Where can I watch the 8 - hour ASP.NET Core workshop?",
					AnswerText="The workshop is at youtube.com/watch?v=--lYHxrsLsc"
				},new QnAPair() {Id=13,
					QuestionText="What is the machine you are using?",
					AnswerText="Jeff broadcasts with a Dell Precision Tower 3620 that has a Geforce GTX 1060 video card"
				},new QnAPair() {Id=14,
					QuestionText="When does Jeff stream?",
					AnswerText="Jeff streams regularly on Tuesday, Wednesday, Thursday, Friday, and Sunday at 10am ET."
				},new QnAPair() {Id=15,
					QuestionText="Where can I watch the C# workshop?",
					AnswerText="The C# workshop is available as a playlist on YouTube at: youtube.com/watch?v=9ZmZuUSqQUM&list=PLVMqA0_8O85zIiU-T5h6rn8ortqEUNCeK"
				},new QnAPair() {Id=16,
					QuestionText="Where can I watch the Architecture workshop?",
					AnswerText="The architecture workshop is available as a playlist on YouTube at: youtube.com/watch?v=k8cZUW4MS3I&list=PLVMqA0_8O85x-aurj1KphxUeWTeTlYkGM"
				},new QnAPair() {Id=17,
					QuestionText="What tool displays your keystrokes?",
					AnswerText="That is Carnac from the Code52 project: Code52 / carnac"
				},new QnAPair() {Id=18,
					QuestionText="What is Madrinas Coffee?",
					AnswerText="Madrinas is a sponsor of the Fritz and Friends channel.They make organic, free trade coffee that you can get from madrinascoffee.com.Use the coupon code 'FRITZ' for 20 % off your order."
				},new QnAPair() {Id=19,
					QuestionText="Who are the Live Coders?",
					AnswerText="The Live Coders is a Twitch stream team that Jeff founded and comprised of folks that write code and answer questions about technology.You can learn more about them at livecoders.dev"
				}, new QnAPair() {Id=20,
					QuestionText="When is the Live Coders Conference ?",
					AnswerText="The first Live Coders Conference is April 9, 2020 starting at 9a ET / 6a PT / 1300 UTC.You can learn more at conf.livecoders.dev"
				},new QnAPair() {Id=21,
					QuestionText="What keyboard are you using?",
					AnswerText="Jeff uses a Vortex Race 3 with Cherry MX Blue switches, details on his blog at: jeffreyfritz.com/2018/07/mechanical-keyboards-i-just-got-one-and-why-you-need-one-too"
				}
			});


		}
	}


}
