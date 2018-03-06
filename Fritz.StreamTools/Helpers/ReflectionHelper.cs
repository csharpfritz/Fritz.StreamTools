using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Fritz.StreamTools.Helpers
{
	public static class ReflectionHelper
	{
		//
		// Uses reflection to find the named event and calls DynamicInvoke on it
		//
		public static void RaiseEvent(object instance, string name, EventArgs args)
		{
			if (instance == null)
				throw new ArgumentNullException(nameof(instance));
			if (string.IsNullOrEmpty(name))
				throw new ArgumentException("message", nameof(name));

			var fieldInfo = instance.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
			if (fieldInfo == null)
				return;
			var multicastDelegate = fieldInfo.GetValue(instance) as MulticastDelegate;

			// NOTE: Using DynamicInvoke so tests work!
			multicastDelegate?.DynamicInvoke(new object[] { instance, args });
		}
	}
}
