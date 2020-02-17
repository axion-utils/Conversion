#region License
// Copyright © 2020 Wesley Hamilton
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of 
// this software and associated documentation files (the "Software"), to deal in the 
// Software without restriction, including without limitation the rights to use, 
// copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the
// Software, and to permit persons to whom the Software is furnished to do so, 
// subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all 
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR 
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN
// AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
#endregion

using System;
using System.Numerics;

namespace Axion.Conversion
{
	/// <summary>
	/// TypeConvert which has been optimized for standard lookups.
	/// </summary>
	internal class TypeConvertDefault : TypeConvert
	{
		public TypeConvertDefault() : this(true, false)
		{
			Func<object, object>[][] numericConversions = Conversions.CheckedConversions;
			for (int i = 3; i <= 15; i++) {
				Func<object, object>[] arr = converterArray[i];
				Func<object, object>[] numericArr = numericConversions[i];
				// bool to decimal
				for (int j = 3; j <= 15; j++) {
					arr[j] = numericArr[j] ?? InvalidConversion;
				}
			}
			converterArray[(int)TypeCode.Boolean][(int)TypeCode.String] = Conversions.ParseBooleanEx;
			LookupCache[Tuple.Create(typeof(string), typeof(BigInteger))] = Conversions.parseBigInteger;
			LookupCache[Tuple.Create(typeof(string), typeof(DateTimeOffset))] = Conversions.parseDateTimeOffset;
			LookupCache[Tuple.Create(typeof(string), typeof(Guid))] = Conversions.ParseGuid;
			LookupCache[Tuple.Create(typeof(string), typeof(TimeSpan))] = Conversions.parseTimeSpan;
		}

		public TypeConvertDefault(bool threadSafe = true, bool tryParseEnum = false) : base(threadSafe, tryParseEnum, null)
		{
			for (int i = 0; i < 19; i++) {
				stringConverters[i] = Conversions.ObjectToString;
				dateTimeConverters[i] = InvalidConversion;
				dbNullConverters[i] = InvalidConversion;
			}
			Func<object, object>[] parseConversions = Conversions.ParseConversions;
			for (int i = 3; i < 17; i++) {
				Func<object, object>[] arr = converterArray[i];
				arr[(int)TypeCode.Empty] = InvalidConversion;
				arr[(int)TypeCode.Object] = InvalidConversion;
				arr[(int)TypeCode.DBNull] = InvalidConversion;
				arr[(int)TypeCode.DateTime] = InvalidConversion;
				arr[17] = InvalidConversion;
				arr[(int)TypeCode.String] = parseConversions[i] ?? InvalidConversion;
				arr[i] = Conversions.None;
			}
			dbNullConverters[(int)TypeCode.String] = Conversions.ObjectToString;
			outputConverters[(int)TypeCode.String] = (object value) => value?.ToString();
		}

		protected override Func<object, object> Lookup(Type input, Type output)
		{
			if (output == typeof(string))
				return Conversions.ObjectToString;
			if (output.IsAssignableFrom(input))
				return Conversions.None;
			Tuple<Type, Type> inout = Tuple.Create(input, output);
			if (!LookupCache.TryGetValue(inout, out Func<object, object> converter)) {
				converter = Conversions.ImplicitCast(input, output)
					?? Conversions.ExplicitCast(input, output)
					?? Conversions.TypeConverter(input, output);
				if (converter != null)
					LookupCache[inout] = converter;
				else
					converter = InvalidConversion;
			}
			return converter;
		}

		protected override Func<object, object> LookupEnum(Type input, TypeCode inputTypeCode, Type output, TypeCode outputTypeCode)
		{
			if (output == typeof(string))
				return Conversions.ObjectToString;
			if (input == typeof(string)) {
				Tuple<Type, Type> inout = Tuple.Create(input, output);
				if (!LookupCache.TryGetValue(inout, out Func<object, object> converter)) {
					converter = TryParseEnum ? Conversions.TryParseEnum(output) : Conversions.ParseEnum(output);
					LookupCache[inout] = converter;
				}
				return converter;
			}
			return this[inputTypeCode, outputTypeCode];
		}
	}
}
