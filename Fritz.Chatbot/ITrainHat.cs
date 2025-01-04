using System.Threading.Tasks;

namespace Fritz.Chatbot
{

	public interface ITrainHat
	{

		void StartTraining(int? count);

		Task AddScreenshot();

	}

}
