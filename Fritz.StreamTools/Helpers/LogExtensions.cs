using System;
using Microsoft.Extensions.Logging;

namespace Fritz.ChatBot.Helpers
{
  public static class LogExtensions
  {
	public static bool LogAndSwallow(this ILogger logger, string action, Exception e)
	{
	  logger.LogWarning($"Exception while {action}: '{e.Message}'");
	  return true;

	}

	public static bool LogAndRethrow(ILogger logger, string action, Exception e)
	{
	  logger.LogWarning($"Exception while {action}: '{e.Message}'");
	  return false;

	}

  }
}
