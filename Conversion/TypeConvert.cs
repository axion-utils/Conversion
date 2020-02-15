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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Axion.Conversion
{
	/// <summary>
	/// An alternative to <see cref="System.Convert"/> and <see cref="System.ComponentModel.TypeConverter"/>. 
	/// Creates functions that can convert any <see cref="object"/> to any <see cref="Type"/>.
	/// </summary>
	public class TypeConvert
	{
		/// <summary>
		/// The default <see cref="TypeConvert"/> that throws exceptions for numeric overflow and failed string parsing.
		/// </summary>
		public static readonly TypeConvert Default = new TypeConvertEx(false, true);

		/// <summary>
		/// The default <see cref="TypeConvert"/> that does not throw exceptions and allows numeric overflow.
		/// </summary>
		public static readonly TypeConvert Safe = new TypeConvertEx(true, true);

		/// <summary>
		/// Converts an <see cref="object"/> to the specified <see cref="Type"/>.
		/// </summary>
		/// <param name="value">The <see cref="object"/> to convert.</param>
		/// <param name="output">The <see cref="Type"/> to convert to.</param>
		/// <param name="safe">Determines if most exceptions should be prevented.</param>
		/// <returns>The result of the conversion or <see langword="null"/> on failure.</returns>
		public static object ChangeType(object value, Type output, bool safe = false)
		{
			return safe ? Safe.ChangeType(value, output) : Default.ChangeType(value, output);
		}

		/// <summary>
		/// Attempts to convert an <see cref="object"/> to the specified <see cref="Type"/> and catches all exceptions.
		/// </summary>
		/// <param name="value">The <see cref="object"/> to convert.</param>
		/// <param name="output">The <see cref="Type"/> to convert to.</param>
		/// <param name="result">The converted <see cref="object"/> or <see langword="null"/> on failure.</param>
		/// <param name="safe">Determines if overflow and parse exceptions should be prevented.</param>
		/// <returns>True if the <see cref="object"/> was converted successfully; false otherwise.</returns>
		public static bool TryChangeType(object value, Type output, out object result, bool safe = true)
		{
			return safe ? Safe.TryChangeType(value, output, out result) : Default.TryChangeType(value, output, out result);
		}

		/// <summary>
		/// Gets or sets the function that converts the input <see cref="Type"/> to the output <see cref="Type"/>. 
		/// <see cref="Conversions.AsNull"/> will be returned for invalid conversions.
		/// </summary>
		/// <param name="input">The <see cref="Type"/> to convert from.</param>
		/// <param name="output">The <see cref="Type"/> to convert to.</param>
		/// <param name="safe">Determines if most exceptions should be prevented.</param>
		public static Func<object, object> GetConverter(Type input, Type output, bool safe = false)
		{
			return safe ? Safe[input, output] : Default[input, output];
		}

		/// <summary>
		/// Gets the function that converts any <see cref="object"/> to the specified <see cref="Type"/>.
		/// </summary>
		/// <param name="output">The <see cref="Type"/> to convert to.</param>
		/// <param name="safe">Determines if most exceptions should be prevented.</param>
		public static Func<object, object> GetConverter(Type output, bool safe = false)
		{
			return safe ? Safe[output] : Default[output];
		}

		/// <summary>
		/// Returns whether the input <see cref="Type"/> can be converted to the output <see cref="Type"/>.
		/// </summary>
		/// <param name="input">The <see cref="Type"/> to convert from.</param>
		/// <param name="output">The <see cref="Type"/> to convert to.</param>
		/// <returns>True if the input <see cref="Type"/> can be converted to the output <see cref="Type"/>.</returns>
		public static bool CanConvertTo(Type input, Type output)
		{
			return Default.CanConvert(input, output);
		}

		private static readonly Func<object, object>[] invalidConverters = new Func<object, object>[19];
		private readonly Func<object, object>[] dbNullConverters = new Func<object, object>[19];
		private readonly Func<object, object>[] boolConverters = new Func<object, object>[19];
		private readonly Func<object, object>[] charConverters = new Func<object, object>[19];
		private readonly Func<object, object>[] sbyteConverters = new Func<object, object>[19];
		private readonly Func<object, object>[] byteConverters = new Func<object, object>[19];
		private readonly Func<object, object>[] int16Converters = new Func<object, object>[19];
		private readonly Func<object, object>[] uint16Converters = new Func<object, object>[19];
		private readonly Func<object, object>[] int32Converters = new Func<object, object>[19];
		private readonly Func<object, object>[] uint32Converters = new Func<object, object>[19];
		private readonly Func<object, object>[] int64Converters = new Func<object, object>[19];
		private readonly Func<object, object>[] uint64Converters = new Func<object, object>[19];
		private readonly Func<object, object>[] singleConverters = new Func<object, object>[19];
		private readonly Func<object, object>[] doubleConverters = new Func<object, object>[19];
		private readonly Func<object, object>[] decimalConverters = new Func<object, object>[19];
		private readonly Func<object, object>[] dateTimeConverters = new Func<object, object>[19];
		private readonly Func<object, object>[] stringConverters = new Func<object, object>[19];
		private readonly Func<object, object>[][] converterArray = new Func<object, object>[19][];
		private readonly Func<object, object>[] outputConverters = new Func<object, object>[19];

		static TypeConvert()
		{
			for(int i = 0; i < invalidConverters.Length; i++) {
				invalidConverters[i] = Conversions.AsNull;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		protected readonly IDictionary<Tuple<Type, Type>, Func<object, object>> LookupCache; // (INPUT, OUTPUT) unlike everything else

		/// <summary>
		/// The converter that is returned when an input <see cref="Type"/> cannot be converted to the output <see cref="Type"/>.
		/// </summary>
		protected Func<object, object> InvalidConversion => Conversions.AsNull;

		/// <summary>
		/// Determines if exceptions should be prevented when possible.
		/// </summary>
		protected readonly bool IsExceptionSafe;

		/// <summary>
		/// Determines whether the converter uses a <see cref="ConcurrentDictionary{TKey, TValue}"/> to store custom converters.
		/// </summary>
		protected bool IsThreadSafe => LookupCache is ConcurrentDictionary<Tuple<Type, Type>, Func<object, object>>;

		private static readonly Type[] BasicTypes = new Type[] {
			typeof(bool), typeof(char), typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(int), typeof(uint),
			typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal), typeof(DateTime), typeof(string),
		};

		/// <summary>
		/// The types that have stored conversions. This is not every possible conversion.
		/// </summary>
		public IEnumerable<Type> OutputTypes => BasicTypes.Concat(LookupCache.Keys.Select(k => k.Item2)).Distinct();
		
		/// <summary>
		/// Constructs a <see cref="TypeConvert"/> that can convert objects between different types.
		/// </summary>
		/// <param name="exceptionSafe">Determines if exceptions should be prevented when possible. This determines if
		/// numeric conversions should be checked for overflow and if string conversions should use the TryParse or
		/// Parse methods.</param>
		/// <param name="threadSafe">Determines if custom conversions should be stored in a thread-safe dictionary.</param>
		public TypeConvert(bool exceptionSafe = false, bool threadSafe = false)
		{
			IsExceptionSafe = exceptionSafe;
			LookupCache = threadSafe
				? new ConcurrentDictionary<Tuple<Type, Type>, Func<object, object>>()
				: (IDictionary<Tuple<Type, Type>, Func<object, object>>)new Dictionary<Tuple<Type, Type>, Func<object, object>>();

			// Typecode to X
			outputConverters[(int)TypeCode.Empty] = InvalidConversion;
			outputConverters[(int)TypeCode.Object] = null;
			outputConverters[(int)TypeCode.DBNull] = ToDBNull;
			outputConverters[(int)TypeCode.Boolean] = ToBoolean;
			outputConverters[(int)TypeCode.Char] = ToChar;
			outputConverters[(int)TypeCode.SByte] = ToSByte;
			outputConverters[(int)TypeCode.Byte] = ToByte;
			outputConverters[(int)TypeCode.Int16] = ToInt16;
			outputConverters[(int)TypeCode.UInt16] = ToUInt16;
			outputConverters[(int)TypeCode.Int32] = ToInt32;
			outputConverters[(int)TypeCode.UInt32] = ToUInt32;
			outputConverters[(int)TypeCode.Int64] = ToInt64;
			outputConverters[(int)TypeCode.UInt64] = ToUInt64;
			outputConverters[(int)TypeCode.Single] = ToSingle;
			outputConverters[(int)TypeCode.Double] = ToDouble;
			outputConverters[(int)TypeCode.Decimal] = ToDecimal;
			outputConverters[(int)TypeCode.DateTime] = ToDateTime;
			outputConverters[(int)TypeCode.String] = ToString;

			converterArray[(int)TypeCode.Empty] = invalidConverters; // Empty = 0
			converterArray[(int)TypeCode.Object] = invalidConverters; // Object = 1
			converterArray[(int)TypeCode.DBNull] = dbNullConverters; // DBNull = 2
			converterArray[(int)TypeCode.Boolean] = boolConverters; // Boolean = 3
			converterArray[(int)TypeCode.Char] = charConverters; // Char = 4
			converterArray[(int)TypeCode.SByte] = sbyteConverters; // SByte = 5
			converterArray[(int)TypeCode.Byte] = byteConverters; // Byte = 6
			converterArray[(int)TypeCode.Int16] = int16Converters; // Int16 = 7
			converterArray[(int)TypeCode.UInt16] = uint16Converters; // UInt16 = 8
			converterArray[(int)TypeCode.Int32] = int32Converters; // Int32 = 9
			converterArray[(int)TypeCode.UInt32] = uint32Converters; // UInt32 = 10
			converterArray[(int)TypeCode.Int64] = int64Converters; // Int64 = 11
			converterArray[(int)TypeCode.UInt64] = uint64Converters; // UInt64 = 12
			converterArray[(int)TypeCode.Single] = singleConverters; // Single = 13
			converterArray[(int)TypeCode.Double] = doubleConverters; // Double = 14
			converterArray[(int)TypeCode.Decimal] = decimalConverters; // Decimal = 15
			converterArray[(int)TypeCode.DateTime] = dateTimeConverters; // DateTime = 16
			converterArray[17] = invalidConverters;
			converterArray[(int)TypeCode.String] = stringConverters; // String = 18

			Func<object, object>[][] numericConversions = exceptionSafe ? Conversions.UncheckedConversions : Conversions.CheckedConversions;
			for (int i = 3; i <= 15; i++) {
				var arr = converterArray[i];
				var numericArr = numericConversions[i];
				// bool to decimal
				for (int j = 3; j <= 15; j++) {
					arr[j] = numericArr[j] ?? InvalidConversion;
				}
			}
			for (int i = 0; i < 19; i++) {
				stringConverters[i] = Conversions.ObjectToString;
				dateTimeConverters[i] = InvalidConversion;
				dbNullConverters[i] = InvalidConversion;
			}
			Func<object, object>[] parseConversions = exceptionSafe ? Conversions.TryParseConversions : Conversions.ParseConversions;
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
			converterArray[(int)TypeCode.Boolean][(int)TypeCode.String] = IsExceptionSafe ? Conversions.TryParseBooleanEx : Conversions.ParseBooleanEx;
			if (exceptionSafe) {
				LookupCache[Tuple.Create(typeof(string), typeof(BigInteger))] = Conversions.tryParseBigInteger;
				LookupCache[Tuple.Create(typeof(string), typeof(DateTimeOffset))] = Conversions.tryParseDateTimeOffset;
				LookupCache[Tuple.Create(typeof(string), typeof(Guid))] = Conversions.TryParseGuid;
				LookupCache[Tuple.Create(typeof(string), typeof(TimeSpan))] = Conversions.tryParseTimeSpan;
			}
			else {
				LookupCache[Tuple.Create(typeof(string), typeof(BigInteger))] = Conversions.parseBigInteger;
				LookupCache[Tuple.Create(typeof(string), typeof(DateTimeOffset))] = Conversions.parseDateTimeOffset;
				LookupCache[Tuple.Create(typeof(string), typeof(Guid))] = Conversions.ParseGuid;
				LookupCache[Tuple.Create(typeof(string), typeof(TimeSpan))] = Conversions.parseTimeSpan;
			}
		}

		/// <summary>
		/// Enables non-standard numeric <see cref="bool"/> conversions. These are not enabled by default.
		/// </summary>
		protected void SetBooleanConversions()
		{
			charConverters[(int)TypeCode.Boolean] = Conversions.BooleanToChar;
			boolConverters[(int)TypeCode.Char] = Conversions.CharToBoolean;
			// X to Boolean
			boolConverters[(int)TypeCode.SByte] = Conversions.SByteToBoolean;
			boolConverters[(int)TypeCode.Byte] = Conversions.ByteToBoolean;
			boolConverters[(int)TypeCode.Int16] = Conversions.Int16ToBoolean;
			boolConverters[(int)TypeCode.UInt16] = Conversions.UInt16ToBoolean;
			boolConverters[(int)TypeCode.Int32] = Conversions.Int32ToBoolean;
			boolConverters[(int)TypeCode.UInt32] = Conversions.UInt32ToBoolean;
			boolConverters[(int)TypeCode.Int64] = Conversions.Int64ToBoolean;
			boolConverters[(int)TypeCode.UInt64] = Conversions.UInt64ToBoolean;
			boolConverters[(int)TypeCode.Single] = Conversions.SingleToBoolean;
			boolConverters[(int)TypeCode.Double] = Conversions.DoubleToBoolean;
			boolConverters[(int)TypeCode.Decimal] = Conversions.DecimalToBoolean;
			// Boolean to X
			sbyteConverters[(int)TypeCode.Boolean] = Conversions.BooleanToSByte;
			byteConverters[(int)TypeCode.Boolean] = Conversions.BooleanToByte;
			int16Converters[(int)TypeCode.Boolean] = Conversions.BooleanToInt16;
			uint16Converters[(int)TypeCode.Boolean] = Conversions.BooleanToUInt16;
			int32Converters[(int)TypeCode.Boolean] = Conversions.BooleanToInt32;
			uint32Converters[(int)TypeCode.Boolean] = Conversions.BooleanToUInt32;
			int64Converters[(int)TypeCode.Boolean] = Conversions.BooleanToInt64;
			uint64Converters[(int)TypeCode.Boolean] = Conversions.BooleanToUInt64;
			singleConverters[(int)TypeCode.Boolean] = Conversions.BooleanToSingle;
			doubleConverters[(int)TypeCode.Boolean] = Conversions.BooleanToDouble;
			decimalConverters[(int)TypeCode.Boolean] = Conversions.BooleanToDecimal;
		}

		/// <summary>
		/// Converts any <see cref="object"/> to a <see cref="DBNull"/>.
		/// </summary>
		/// <param name="value">The <see cref="object"/> to convert.</param>
		protected virtual object ToDBNull(object value)
		{
			if (value == null)
				return null;
			Type input = value.GetType();
			TypeCode typeCode = Type.GetTypeCode(input);
			Func<object, object> converter = typeCode == TypeCode.Object ? Lookup(input, typeof(DBNull)) : dbNullConverters[(int)typeCode];
			return converter(value);
		}

		/// <summary>
		/// Converts any <see cref="object"/> to a <see cref="bool"/>.
		/// </summary>
		/// <param name="value">The <see cref="object"/> to convert.</param>
		public object ToBoolean(object value)
		{
			if (value == null)
				return null;
			Type input = value.GetType();
			TypeCode typeCode = Type.GetTypeCode(input);
			Func<object, object> converter = typeCode == TypeCode.Object ? Lookup(input, typeof(bool)) : boolConverters[(int)typeCode];
			return converter(value);
		}

		/// <summary>
		/// Converts any <see cref="object"/> to a <see cref="char"/>.
		/// </summary>
		/// <param name="value">The <see cref="object"/> to convert.</param>
		public object ToChar(object value)
		{
			if (value == null)
				return null;
			Type input = value.GetType();
			TypeCode typeCode = Type.GetTypeCode(input);
			Func<object, object> converter = typeCode == TypeCode.Object ? Lookup(input, typeof(char)) : charConverters[(int)typeCode];
			return converter(value);
		}

		/// <summary>
		/// Converts any <see cref="object"/> to an <see cref="sbyte"/>.
		/// </summary>
		/// <param name="value">The <see cref="object"/> to convert.</param>
		public object ToSByte(object value)
		{
			if (value == null)
				return null;
			Type input = value.GetType();
			TypeCode typeCode = Type.GetTypeCode(input);
			Func<object, object> converter = typeCode == TypeCode.Object ? Lookup(input, typeof(sbyte)) : sbyteConverters[(int)typeCode];
			return converter(value);
		}

		/// <summary>
		/// Converts any <see cref="object"/> to a <see cref="byte"/>.
		/// </summary>
		/// <param name="value">The <see cref="object"/> to convert.</param>
		public object ToByte(object value)
		{
			if (value == null)
				return null;
			Type input = value.GetType();
			TypeCode typeCode = Type.GetTypeCode(input);
			Func<object, object> converter = typeCode == TypeCode.Object ? Lookup(input, typeof(byte)) : byteConverters[(int)typeCode];
			return converter(value);
		}

		/// <summary>
		/// Converts any <see cref="object"/> to a <see cref="short"/>.
		/// </summary>
		/// <param name="value">The <see cref="object"/> to convert.</param>
		public object ToInt16(object value)
		{
			if (value == null)
				return null;
			Type input = value.GetType();
			TypeCode typeCode = Type.GetTypeCode(input);
			Func<object, object> converter = typeCode == TypeCode.Object ? Lookup(input, typeof(short)) : int16Converters[(int)typeCode];
			return converter(value);
		}

		/// <summary>
		/// Converts any <see cref="object"/> to a <see cref="ushort"/>.
		/// </summary>
		/// <param name="value">The <see cref="object"/> to convert.</param>
		public object ToUInt16(object value)
		{
			if (value == null)
				return null;
			Type input = value.GetType();
			TypeCode typeCode = Type.GetTypeCode(input);
			Func<object, object> converter = typeCode == TypeCode.Object ? Lookup(input, typeof(ushort)) : uint16Converters[(int)typeCode];
			return converter(value);
		}

		/// <summary>
		/// Converts any <see cref="object"/> to an <see cref="int"/>.
		/// </summary>
		/// <param name="value">The <see cref="object"/> to convert.</param>
		public object ToInt32(object value)
		{
			if (value == null)
				return null;
			Type input = value.GetType();
			TypeCode typeCode = Type.GetTypeCode(input);
			Func<object, object> converter = typeCode == TypeCode.Object ? Lookup(input, typeof(int)) : int32Converters[(int)typeCode];
			return converter(value);
		}

		/// <summary>
		/// Converts any <see cref="object"/> to a <see cref="uint"/>.
		/// </summary>
		/// <param name="value">The <see cref="object"/> to convert.</param>
		public object ToUInt32(object value)
		{
			if (value == null)
				return null;
			Type input = value.GetType();
			TypeCode typeCode = Type.GetTypeCode(input);
			Func<object, object> converter = typeCode == TypeCode.Object ? Lookup(input, typeof(uint)) : uint32Converters[(int)typeCode];
			return converter(value);
		}

		/// <summary>
		/// Converts any <see cref="object"/> to a <see cref="long"/>.
		/// </summary>
		/// <param name="value">The <see cref="object"/> to convert.</param>
		public object ToInt64(object value)
		{
			if (value == null)
				return null;
			Type input = value.GetType();
			TypeCode typeCode = Type.GetTypeCode(input);
			Func<object, object> converter = typeCode == TypeCode.Object ? Lookup(input, typeof(long)) : int64Converters[(int)typeCode];
			return converter(value);
		}

		/// <summary>
		/// Converts any <see cref="object"/> to a <see cref="ulong"/>.
		/// </summary>
		/// <param name="value">The <see cref="object"/> to convert.</param>
		public object ToUInt64(object value)
		{
			if (value == null)
				return null;
			Type input = value.GetType();
			TypeCode typeCode = Type.GetTypeCode(input);
			Func<object, object> converter = typeCode == TypeCode.Object ? Lookup(input, typeof(ulong)) : uint64Converters[(int)typeCode];
			return converter(value);
		}

		/// <summary>
		/// Converts any <see cref="object"/> to a <see cref="float"/>.
		/// </summary>
		/// <param name="value">The <see cref="object"/> to convert.</param>
		public object ToSingle(object value)
		{
			if (value == null)
				return null;
			Type input = value.GetType();
			TypeCode typeCode = Type.GetTypeCode(input);
			Func<object, object> converter = typeCode == TypeCode.Object ? Lookup(input, typeof(float)) : singleConverters[(int)typeCode];
			return converter(value);
		}

		/// <summary>
		/// Converts any <see cref="object"/> to a <see cref="double"/>.
		/// </summary>
		/// <param name="value">The <see cref="object"/> to convert.</param>
		public object ToDouble(object value)
		{
			if (value == null)
				return null;
			Type input = value.GetType();
			TypeCode typeCode = Type.GetTypeCode(input);
			Func<object, object> converter = typeCode == TypeCode.Object ? Lookup(input, typeof(double)) : doubleConverters[(int)typeCode];
			return converter(value);
		}

		/// <summary>
		/// Converts any <see cref="object"/> to a <see cref="decimal"/>.
		/// </summary>
		/// <param name="value">The <see cref="object"/> to convert.</param>
		public object ToDecimal(object value)
		{
			if (value == null)
				return null;
			Type input = value.GetType();
			TypeCode typeCode = Type.GetTypeCode(input);
			Func<object, object> converter = typeCode == TypeCode.Object ? Lookup(input, typeof(decimal)) : decimalConverters[(int)typeCode];
			return converter(value);
		}

		/// <summary>
		/// Converts any <see cref="object"/> to a <see cref="DateTime"/>.
		/// </summary>
		/// <param name="value">The <see cref="object"/> to convert.</param>
		public object ToDateTime(object value)
		{
			if (value == null)
				return null;
			Type input = value.GetType();
			TypeCode typeCode = Type.GetTypeCode(input);
			Func<object, object> converter = typeCode == TypeCode.Object ? Lookup(input, typeof(DateTime)) : dateTimeConverters[(int)typeCode];
			return converter(value);
		}

		/// <summary>
		/// Converts any <see cref="object"/> to a <see cref="string"/>.
		/// </summary>
		/// <param name="value">The <see cref="object"/> to convert.</param>
		public virtual object ToString(object value)
		{
			if (value == null)
				return null;
			Type input = value.GetType();
			TypeCode typeCode = Type.GetTypeCode(input);
			Func<object, object> converter = typeCode == TypeCode.Object ? Lookup(input, typeof(string)) : stringConverters[(int)typeCode];
			return converter(value);
		}

		private object ToEnum(object value, Type output, TypeCode outputTypeCode)
		{
			if (value == null)
				return null;
			Type input = value.GetType();
			TypeCode inputTypeCode = Type.GetTypeCode(input);
			Func<object, object> converter = LookupEnum(input, inputTypeCode, output, outputTypeCode);
			return converter(value);
		}

		/// <summary>
		/// Creates a function for the given conversion which includes at least one <see cref="Enum"/>.
		/// </summary>
		/// <param name="input">The <see cref="Type"/> to convert from.</param>
		/// <param name="inputTypeCode">The <see cref="TypeCode"/> of <paramref name="input"/>.</param>
		/// <param name="output">The <see cref="Type"/> to convert to.</param>
		/// <param name="outputTypeCode">The <see cref="TypeCode"/> of <paramref name="output"/>.</param>
		protected virtual Func<object, object> LookupEnum(Type input, TypeCode inputTypeCode, Type output, TypeCode outputTypeCode)
		{
			Tuple<Type, Type> inout = Tuple.Create(input, output);
			if (!LookupCache.TryGetValue(inout, out Func<object, object> converter)) {
				if (output == typeof(string))
					return Conversions.ObjectToString;
				if (input == typeof(string)) {
					converter = IsExceptionSafe ? Conversions.TryParseEnum(output) : Conversions.ParseEnum(output);
					LookupCache[inout] = converter;
				}
				else
					converter = converterArray[(int)outputTypeCode][(int)inputTypeCode];
			}
			return converter;
		}

		/// <summary>
		/// Gets the function for the specified conversion.
		/// </summary>
		/// <param name="output">The <see cref="TypeCode"/> of the input.</param>
		/// <param name="input">The <see cref="TypeCode"/> of the output</param>
		protected Func<object, object> this[TypeCode input, TypeCode output] {
			get => converterArray[(int)output][(int)input];
			set => converterArray[(int)output][(int)input] = value ?? InvalidConversion;
		}

		/// <summary>
		/// Creates a function for the given conversion where neither input and output are an <see cref="Enum"/>.
		/// </summary>
		/// <param name="input">The <see cref="Type"/> to convert from.</param>
		/// <param name="output">The <see cref="Type"/> to convert to.</param>
		/// <returns>The function that converts an object of the given <see cref="Type"/> to the specified <see cref="Type"/>.</returns>
		protected virtual Func<object, object> Lookup(Type input, Type output)
		{
			Tuple<Type, Type> inout = Tuple.Create(input, output);
			if (!LookupCache.TryGetValue(inout, out Func<object, object> converter)) {
				if (output == typeof(string))
					return Conversions.ObjectToString;
				if (output.IsAssignableFrom(input))
					return Conversions.None;
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

		/// <summary>
		/// The output converter for types without a <see cref="TypeCode"/>.
		/// </summary>
		private object ConvertCustom(object value, Type output)
		{
			if (value == null)
				return null;
			Type input = value.GetType();
			Func<object, object> converter = Lookup(input, output);
			return converter(value);
		}

		/// <summary>
		/// Returns whether the input <see cref="Type"/> can be converted to the output <see cref="Type"/>.
		/// </summary>
		/// <param name="input">The <see cref="Type"/> to convert from.</param>
		/// <param name="output">The <see cref="Type"/> to convert to.</param>
		/// <returns>True if the input <see cref="Type"/> can be converted to the output <see cref="Type"/>.</returns>
		public bool CanConvert(Type input, Type output)
		{
			Func<object, object> converter = this[input, output];
			return converter != InvalidConversion;
		}

		/// <summary>
		/// Converts an <see cref="object"/> to the specified <see cref="Type"/>.
		/// </summary>
		/// <param name="value">The <see cref="object"/> to convert.</param>
		/// <param name="output">The <see cref="Type"/> to convert to.</param>
		/// <returns>The result of the conversion or <see langword="null"/> on failure.</returns>
		public object ChangeType(object value, Type output)
		{
			if (value == null)
				return value;
			Type input = value.GetType();
			Func<object, object> converter = this[input, output];
			return converter(value);
		}

		/// <summary>
		/// Attempts to convert an <see cref="object"/> to the specified <see cref="Type"/> and catches all exceptions.
		/// </summary>
		/// <param name="value">The <see cref="object"/> to convert.</param>
		/// <param name="output">The <see cref="Type"/> to convert to.</param>
		/// <param name="result">The converted <see cref="object"/> or <see langword="null"/> on failure.</param>
		/// <returns>True if the <see cref="object"/> was converted successfully; false otherwise.</returns>
		public bool TryChangeType(object value, Type output, out object result)
		{
			if (value != null) {
				try {
					result = ChangeType(value, output);
					return result != null;
				}
				catch {
					// ignore
				}
			}
			result = null;
			return false;
		}

		/// <summary>
		/// Gets or sets the function that converts an <see cref="Object"/> of the given <see cref="Type"/> to the specified output <see cref="Type"/>. 
		/// <see cref="Conversions.AsNull"/> will be returned for invalid conversions. A <see langword="null"/>
		/// value will remove the conversion or set it to <see cref="Conversions.AsNull"/>.
		/// </summary>
		/// <param name="input">The <see cref="Type"/> to convert from.</param>
		/// <param name="output">The <see cref="Type"/> to convert to.</param>
		/// <returns>The function that converts an <see cref="Object"/> of the given <see cref="Type"/> to the specified output <see cref="Type"/>.</returns>
		public Func<object, object> this[Type input, Type output] {
			get {
				if (output == typeof(object))
					return Conversions.None;
				TypeCode outputTypeCode = Type.GetTypeCode(output);
				if (outputTypeCode > TypeCode.Object) {
					TypeCode inputTypeCode = Type.GetTypeCode(input);
					if (output.IsEnum || input.IsEnum)
						return LookupEnum(input, inputTypeCode, output, outputTypeCode);
					if (inputTypeCode > TypeCode.Object)
						return converterArray[(int)outputTypeCode][(int)inputTypeCode];
				}
				else if (input.IsEnum)
					return LookupEnum(input, Type.GetTypeCode(input), output, outputTypeCode);
				return Lookup(input, output);
			}
			protected set {
				TypeCode outputTypeCode = Type.GetTypeCode(output);
				if (outputTypeCode > TypeCode.Object) {
					TypeCode inputTypeCode = Type.GetTypeCode(input);
					if (inputTypeCode > TypeCode.Object && !input.IsEnum && !output.IsEnum) {
						converterArray[(int)outputTypeCode][(int)inputTypeCode] = value ?? InvalidConversion;
					}
				}
				else if (output == typeof(object))
					throw new InvalidOperationException("Output type Object cannot be set.");
				Tuple<Type, Type> inout = Tuple.Create(input, output);
				if (value == null)
					LookupCache.Remove(inout);
				else
					LookupCache[inout] = value;
			}
		}

		/// <summary>
		/// Gets the function that converts any <see cref="object"/> to the specified <see cref="Type"/>.
		/// </summary>
		/// <param name="output">The <see cref="Type"/> to convert to.</param>
		/// <returns>The function that converts any <see cref="object"/> to the specified <see cref="Type"/>.</returns>
		public Func<object, object> this[Type output] {
			get {
				if (output == typeof(object))
					return Conversions.None;
				TypeCode typeCode = Type.GetTypeCode(output);
				if (typeCode > TypeCode.Object) {
					if (output.IsEnum)
						return (value) => ToEnum(value, output, typeCode);
					return outputConverters[(int)typeCode];
				}
				return (value) => ConvertCustom(value, output);
			}
		}
	}
}
