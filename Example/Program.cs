using System;
using Axion.Conversion;

namespace Axion
{
public static class Program
{
public static void Main()
{
	// equivalent to TypeConvert.Default.ChangeType
	object value = TypeConvert.ChangeType("1.5", typeof(double));
	Console.WriteLine(value + " " + value.GetType().Name); // 1.5 Double

	Func<object, object> converter = TypeConvert.GetConverter(typeof(string), typeof(char));
	value = converter("c");
	Console.WriteLine(value + " " + value.GetType().Name); // c char
	value = converter("5");
	Console.WriteLine(value + " " + value.GetType().Name); // 5 char

	// prevents numeric overflow
	value = ((long)int.MaxValue) + 1;
	bool success = TypeConvert.Default.TryChangeType(value, typeof(int), out object result);
	Console.WriteLine(success + " " + (result ?? "null")); // False null

	// Enum conversions are case sensitive
	value = TypeConvert.Default.ChangeType(DayOfWeek.Friday.ToString(), typeof(DayOfWeek));
	Console.WriteLine(value + " " + value.GetType().Name); // Friday DayOfWeek

	Console.ReadKey();
}
}
}
