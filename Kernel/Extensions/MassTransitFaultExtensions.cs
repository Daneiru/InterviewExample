using MassTransit;
using System.Text;

namespace Kernel.Extensions;

    public static class MassTransitFaultExtensions
    {
	/// <summary>
	/// Creates a standardized exception string for Consumer faults
	/// </summary>
	public static string ToExceptionString(this Fault fault)
	{
		var exception = new StringBuilder();
		exception.Append($"{fault.Exceptions[0].ExceptionType}: {fault.Exceptions[0].Message}\n{fault.Exceptions[0].StackTrace}");

		foreach (var ex in fault.Exceptions.Skip(1))
		{
			exception.Append("--- End of stack trace from previous location where exception was thrown ---");
			exception.Append(ex.StackTrace);
		}

		return exception.ToString();
	}
}
