namespace Kernel.Extensions;

public static class EnumExtensions
{
	/// <summary>
	/// Try to convert string into enumeration type
	/// </summary>
	/// <typeparam name="T">Enumeration type</typeparam>
	/// <param name="src">String to Convert</param>
	/// <returns>Enum type or null if the conversion fails</returns>
	public static T? ConvertToEnum<T>(this string? src) where T : struct
	{
		if (Enum.TryParse(src, true, out T result) && Enum.IsDefined(typeof(T), result))		
			return result;
		
		return null;
	}
}