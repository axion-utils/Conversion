# Introduction

[Nuget: Axion.Conversion](https://www.nuget.org/packages/Axion.Conversion/)

This package is intended to replace System.Convert. Some key differences include:
1. Custom conversion is supported through inheritance. Only null and typeof(object) are treated as special cases. 
See [TypeConvertEx](https://github.com/axion-utils/Conversion/blob/master/TypeConvert/TypeConvertEx.cs) for an example of how to do this.
2. Exceptions can be prevented without using a try catch.
3. Functions can be reused when converting lists of objects.
4. TypeConvert.CanConvertTo is more accurate than System.ComponentModel.TypeConverter.CanConvertTo.

## Example

```csharp
using System;
using Axion.Conversion;

namespace Example
{
public static class Program
{
public static void Main()
{
	// Equivalent to TypeConvert.Default.ChangeType
	object value = TypeConvert.ChangeType("1.5", typeof(double));
	Console.WriteLine(value + " " + value.GetType().Name); // 1.5 Double

	// Functions can be reused
	Func<object, object> converter = TypeConvert.GetConverter(typeof(string), typeof(char));
	value = converter("c");
	Console.WriteLine(value + " " + value.GetType().Name); // c char
	value = converter("5");
	Console.WriteLine(value + " " + value.GetType().Name); // 5 char

	// throws an exception for numeric overflow
	value = ((long)int.MaxValue) + 1;
	bool success = TypeConvert.Default.TryChangeType(value, typeof(int), out object result);
	Console.WriteLine(success + " " + (result ?? "null")); // False null

	// allows numeric overflow
	success = TypeConvert.Safe.TryChangeType(value, typeof(int), out result);
	Console.WriteLine(success + " " + (result ?? "null")); // True -2147483648

	// Enum conversions are case sensitive
	value = TypeConvert.Default.ChangeType(DayOfWeek.Friday.ToString(), typeof(DayOfWeek));
	Console.WriteLine(value + " " + value.GetType().Name); // Friday DayOfWeek

	Console.ReadKey();
}
}
}

```

# License

*The Apache 2.0 License*

Copyright (c) 2020 Wesley Hamilton 

Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at 

http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License.