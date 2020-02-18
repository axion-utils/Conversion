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

namespace Axion.Conversion
{
	public class TypeConvertBase
	{
		/// <summary>
		/// Constructs a <see cref="TypeConvertBase"/> and copies the conversions from another <see cref="TypeConvertBase"/>.
		/// </summary>
		/// <param name="threadSafe">Determines if custom conversions use a <see cref="ConcurrentDictionary{TKey, TValue}"/>.</param>
		/// <param name="tryParseEnum">Determines if Enum.Parse() or Enum.TryParse() is used.</param>
		/// <param name="copyFrom">The <see cref="TypeConvertBase"/> to copy converters from.</param>
		public TypeConvertBase(bool threadSafe, bool tryParseEnum, TypeConvertBase copyFrom) : this(threadSafe, tryParseEnum)
		{
			if (copyFrom != null) {
				foreach (var kv in copyFrom.LookupCache) {
					LookupCache.Add(kv.Key, kv.Value);
				}
				for (int i = 2; i < 19; i++) {
					Func<object, object>[] arr = converterArray[i];
					Func<object, object>[] blArr = copyFrom.converterArray[i];
					for (int j = 0; j < 19; j++) {
						arr[j] = blArr[j];
					}
				}
			}
			else {
				for (int i = 2; i < 19; i++) {
					Func<object, object>[] arr = converterArray[i];
					for (int j = 0; j < 19; j++) {
						arr[j] = InvalidConversion;
					}
					arr[i] = Conversions.None;
				}
				converterArray[17][17] = InvalidConversion;
			}
		}

		/// <summary>
		/// Constructs an empty <see cref="TypeConvertBase"/>.
		/// </summary>
		/// <param name="threadSafe">Determines if custom conversions use a <see cref="ConcurrentDictionary{TKey, TValue}"/>.</param>
		/// <param name="tryParseEnum">Determines if Enum.Parse() or Enum.TryParse() is used.</param>
		public TypeConvertBase(bool threadSafe, bool tryParseEnum)
		{
			LookupCache = threadSafe
				? new ConcurrentDictionary<Tuple<Type, Type>, Func<object, object>>()
				: (IDictionary<Tuple<Type, Type>, Func<object, object>>)new Dictionary<Tuple<Type, Type>, Func<object, object>>();

			TryParseEnum = tryParseEnum;

			//outputConverters[(int)TypeCode.Empty] = null;
			//outputConverters[(int)TypeCode.Object] = null;
			outputConverters[(int)TypeCode.DBNull] = CreateConverterFromType(typeof(DBNull));
			outputConverters[(int)TypeCode.Boolean] = CreateConverterFromType(typeof(bool));
			outputConverters[(int)TypeCode.Char] = CreateConverterFromType(typeof(char));
			outputConverters[(int)TypeCode.SByte] = CreateConverterFromType(typeof(sbyte));
			outputConverters[(int)TypeCode.Byte] = CreateConverterFromType(typeof(byte));
			outputConverters[(int)TypeCode.Int16] = CreateConverterFromType(typeof(short));
			outputConverters[(int)TypeCode.UInt16] = CreateConverterFromType(typeof(ushort));
			outputConverters[(int)TypeCode.Int32] = CreateConverterFromType(typeof(int));
			outputConverters[(int)TypeCode.UInt32] = CreateConverterFromType(typeof(uint));
			outputConverters[(int)TypeCode.Int64] = CreateConverterFromType(typeof(long));
			outputConverters[(int)TypeCode.UInt64] = CreateConverterFromType(typeof(ulong));
			outputConverters[(int)TypeCode.Single] = CreateConverterFromType(typeof(float));
			outputConverters[(int)TypeCode.Double] = CreateConverterFromType(typeof(double));
			outputConverters[(int)TypeCode.Decimal] = CreateConverterFromType(typeof(decimal));
			outputConverters[(int)TypeCode.DateTime] = CreateConverterFromType(typeof(DateTime));
			outputConverters[17] = null;
			outputConverters[(int)TypeCode.String] = ToString;

			//converterArray[(int)TypeCode.Empty] = invalidConverters; // Empty = 0
			//converterArray[(int)TypeCode.Object] = invalidConverters; // Object = 1
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
		}

		/// <summary>
		/// Unused functions which are only used to prevent exceptions.
		/// </summary>
		protected readonly Func<object, object>[] invalidConverters = new Func<object, object>[19];
		/// <summary>
		/// Functions which convert from input (int)<see cref="TypeCode"/> to <see cref="DBNull"/>.
		/// </summary>
		protected readonly Func<object, object>[] dbNullConverters = new Func<object, object>[19];
		/// <summary>
		/// Functions which convert from input (int)<see cref="TypeCode"/> to <see cref="bool"/>.
		/// </summary>
		protected readonly Func<object, object>[] boolConverters = new Func<object, object>[19];
		/// <summary>
		/// Functions which convert from input (int)<see cref="TypeCode"/> to <see cref="char"/>.
		/// </summary>
		protected readonly Func<object, object>[] charConverters = new Func<object, object>[19];
		/// <summary>
		/// Functions which convert from input (int)<see cref="TypeCode"/> to <see cref="sbyte"/>.
		/// </summary>
		protected readonly Func<object, object>[] sbyteConverters = new Func<object, object>[19];
		/// <summary>
		/// Functions which convert from input (int)<see cref="TypeCode"/> to <see cref="byte"/>.
		/// </summary>
		protected readonly Func<object, object>[] byteConverters = new Func<object, object>[19];
		/// <summary>
		/// Functions which convert from input (int)<see cref="TypeCode"/> to <see cref="short"/>.
		/// </summary>
		protected readonly Func<object, object>[] int16Converters = new Func<object, object>[19];
		/// <summary>
		/// Functions which convert from input (int)<see cref="TypeCode"/> to <see cref="ushort"/>.
		/// </summary>
		protected readonly Func<object, object>[] uint16Converters = new Func<object, object>[19];
		/// <summary>
		/// Functions which convert from input (int)<see cref="TypeCode"/> to <see cref="int"/>.
		/// </summary>
		protected readonly Func<object, object>[] int32Converters = new Func<object, object>[19];
		/// <summary>
		/// Functions which convert from input (int)<see cref="TypeCode"/> to <see cref="uint"/>.
		/// </summary>
		protected readonly Func<object, object>[] uint32Converters = new Func<object, object>[19];
		/// <summary>
		/// Functions which convert from input (int)<see cref="TypeCode"/> to <see cref="long"/>.
		/// </summary>
		protected readonly Func<object, object>[] int64Converters = new Func<object, object>[19];
		/// <summary>
		/// Functions which convert from input (int)<see cref="TypeCode"/> to <see cref="ulong"/>.
		/// </summary>
		protected readonly Func<object, object>[] uint64Converters = new Func<object, object>[19];
		/// <summary>
		/// Functions which convert from input (int)<see cref="TypeCode"/> to <see cref="float"/>.
		/// </summary>
		protected readonly Func<object, object>[] singleConverters = new Func<object, object>[19];
		/// <summary>
		/// Functions which convert from input (int)<see cref="TypeCode"/> to <see cref="double"/>.
		/// </summary>
		protected readonly Func<object, object>[] doubleConverters = new Func<object, object>[19];
		/// <summary>
		/// Functions which convert from input (int)<see cref="TypeCode"/> to <see cref="decimal"/>.
		/// </summary>
		protected readonly Func<object, object>[] decimalConverters = new Func<object, object>[19];
		/// <summary>
		/// Functions which convert from input (int)<see cref="TypeCode"/> to <see cref="string"/>.
		/// </summary>
		protected readonly Func<object, object>[] dateTimeConverters = new Func<object, object>[19];
		/// <summary>
		/// Functions which convert from input (int)<see cref="TypeCode"/> to <see cref="string"/>.
		/// </summary>
		protected readonly Func<object, object>[] stringConverters = new Func<object, object>[19];

		/// <summary>
		/// The function returned by <see cref="this[Type]"/> and called by methods such as <see cref="ToBoolean(object)"/>. It should not
		/// support custom conversions of <see cref="TypeCode.Object"/> or enums.
		/// </summary>
		internal readonly Func<object, object>[] outputConverters = new Func<object, object>[19];

		/// <summary>
		/// An array of converters which can be accessed using Typecodes via [output][input].
		/// </summary>
		internal readonly Func<object, object>[][] converterArray = new Func<object, object>[19][];

		/// <summary>
		/// The cache for custom conversions stored as Tuple.Create(input, output). This will include enum parsing, implicit/explicit casts
		/// for non-primitive types, and <see cref="System.ComponentModel.TypeConverter"/>.
		/// </summary>
		protected readonly IDictionary<Tuple<Type, Type>, Func<object, object>> LookupCache;

		/// <summary>
		/// The converter that is returned when an input <see cref="Type"/> cannot be converted to the output <see cref="Type"/>.
		/// </summary>
		protected Func<object, object> InvalidConversion => Conversions.AsNull;

		/// <summary>
		/// Determines whether the converter uses a <see cref="ConcurrentDictionary{TKey, TValue}"/> to store custom converters.
		/// </summary>
		protected bool IsThreadSafe => LookupCache is ConcurrentDictionary<Tuple<Type, Type>, Func<object, object>>;

		/// <summary>
		/// Determines if Enum.TryParse() or Enum.Parse() will be used.
		/// </summary>
		protected bool TryParseEnum { get; }

		/// <summary>
		/// The types with a <see cref="TypeCode"/>.
		/// </summary>
		private static readonly Type[] TypeCodeTypes = new Type[] {
			typeof(bool), typeof(char), typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(int), typeof(uint),
			typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal), typeof(DateTime), typeof(string),
		};

		/// <summary>
		/// The types that have stored conversions. This is not every possible conversion.
		/// </summary>
		public IEnumerable<Type> OutputTypes => TypeCodeTypes.Concat(LookupCache.Keys.Select(k => k.Item2)).Distinct();


		/// <summary>
		/// Creates a converter from a given type. This is used to populate <see cref="outputConverters"/>. <see cref="TypeCode.Object"/> is not supported.
		/// </summary>
		/// <param name="output">The type to create a converter for.</param>
		protected Func<object, object> CreateConverterFromType(Type output)
		{
			TypeCode outputTypeCode = Type.GetTypeCode(output);
			if (outputTypeCode <= TypeCode.Object)
				throw new NotSupportedException("CreateConverterFromType does not support TypeCode: " + outputTypeCode.ToString());
			return (object value) => {
				Type input;
				if (value == null)
					input = null;
				else {
					input = value.GetType();
					TypeCode inputTypeCode = Type.GetTypeCode(input);
					Func<object, object> converter;
					if (inputTypeCode == TypeCode.Object)
						converter = Lookup(input, output);
					else if (input.IsEnum)
						converter = LookupEnum(input, inputTypeCode, input, inputTypeCode);
					else
						converter = this[inputTypeCode, outputTypeCode];
					object result = converter(value);
					if (result != null)
						return result;
				}
				return OnFail(value, input, output);
			};
		}

		/// <summary>
		/// Converts any <see cref="object"/> to a <see cref="DBNull"/>.
		/// </summary>
		/// <param name="value">The <see cref="object"/> to convert.</param>
		protected object ToDBNull(object value) => outputConverters[(int)TypeCode.DBNull](value);

		/// <summary>
		/// Converts any <see cref="object"/> to a <see cref="bool"/>.
		/// </summary>
		/// <param name="value">The <see cref="object"/> to convert.</param>
		public object ToBoolean(object value) => outputConverters[(int)TypeCode.Boolean](value);

		/// <summary>
		/// Converts any <see cref="object"/> to a <see cref="char"/>.
		/// </summary>
		/// <param name="value">The <see cref="object"/> to convert.</param>
		public object ToChar(object value) => outputConverters[(int)TypeCode.Char](value);

		/// <summary>
		/// Converts any <see cref="object"/> to an <see cref="sbyte"/>.
		/// </summary>
		/// <param name="value">The <see cref="object"/> to convert.</param>
		public object ToSByte(object value) => outputConverters[(int)TypeCode.SByte](value);

		/// <summary>
		/// Converts any <see cref="object"/> to a <see cref="byte"/>.
		/// </summary>
		/// <param name="value">The <see cref="object"/> to convert.</param>
		public object ToByte(object value) => outputConverters[(int)TypeCode.Byte](value);

		/// <summary>
		/// Converts any <see cref="object"/> to a <see cref="short"/>.
		/// </summary>
		/// <param name="value">The <see cref="object"/> to convert.</param>
		public object ToInt16(object value) => outputConverters[(int)TypeCode.Int16](value);

		/// <summary>
		/// Converts any <see cref="object"/> to a <see cref="ushort"/>.
		/// </summary>
		/// <param name="value">The <see cref="object"/> to convert.</param>
		public object ToUInt16(object value) => outputConverters[(int)TypeCode.UInt16](value);

		/// <summary>
		/// Converts any <see cref="object"/> to an <see cref="int"/>.
		/// </summary>
		/// <param name="value">The <see cref="object"/> to convert.</param>
		public object ToInt32(object value) => outputConverters[(int)TypeCode.Int32](value);

		/// <summary>
		/// Converts any <see cref="object"/> to a <see cref="uint"/>.
		/// </summary>
		/// <param name="value">The <see cref="object"/> to convert.</param>
		public object ToUInt32(object value) => outputConverters[(int)TypeCode.UInt32](value);

		/// <summary>
		/// Converts any <see cref="object"/> to a <see cref="long"/>.
		/// </summary>
		/// <param name="value">The <see cref="object"/> to convert.</param>
		public object ToInt64(object value) => outputConverters[(int)TypeCode.Int64](value);

		/// <summary>
		/// Converts any <see cref="object"/> to a <see cref="ulong"/>.
		/// </summary>
		/// <param name="value">The <see cref="object"/> to convert.</param>
		public object ToUInt64(object value) => outputConverters[(int)TypeCode.UInt64](value);

		/// <summary>
		/// Converts any <see cref="object"/> to a <see cref="float"/>.
		/// </summary>
		/// <param name="value">The <see cref="object"/> to convert.</param>
		public object ToSingle(object value) => outputConverters[(int)TypeCode.Single](value);

		/// <summary>
		/// Converts any <see cref="object"/> to a <see cref="double"/>.
		/// </summary>
		/// <param name="value">The <see cref="object"/> to convert.</param>
		public object ToDouble(object value) => outputConverters[(int)TypeCode.Double](value);

		/// <summary>
		/// Converts any <see cref="object"/> to a <see cref="decimal"/>.
		/// </summary>
		/// <param name="value">The <see cref="object"/> to convert.</param>
		public object ToDecimal(object value) => outputConverters[(int)TypeCode.Decimal](value);

		/// <summary>
		/// Converts any <see cref="object"/> to a <see cref="DateTime"/>.
		/// </summary>
		/// <param name="value">The <see cref="object"/> to convert.</param>
		public object ToDateTime(object value) => outputConverters[(int)TypeCode.DateTime](value);

		/// <summary>
		/// Converts any <see cref="object"/> to a <see cref="string"/>.
		/// </summary>
		/// <param name="value">The <see cref="object"/> to convert.</param>
		public object ToString(object value) => outputConverters[(int)TypeCode.String](value);

		private object ToEnum(object value, Type output, TypeCode outputTypeCode)
		{
			Type input;
			if (value == null)
				input = null;
			else {
				input = value.GetType();
				TypeCode inputTypeCode = Type.GetTypeCode(input);
				Func<object, object> converter = LookupEnum(input, inputTypeCode, output, outputTypeCode);
				object result = converter(value);
				if (result != null)
					return result;
			}
			return OnFail(value, input, output);
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
					converter = TryParseEnum ? Conversions.TryParseEnum(output) : Conversions.ParseEnum(output);
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
			Type input;
			if (value == null)
				input = null;
			else {
				input = value.GetType();
				Func<object, object> converter = input.IsEnum ? LookupEnum(input, Type.GetTypeCode(input), output, TypeCode.Object) : Lookup(input, output);
				object result = converter(value);
				if (result != null)
					return result;
			}
			return OnFail(value, input, output);
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
			Type input;
			if (value == null)
				input = null;
			else {
				input = value.GetType();
				Func<object, object> converter = this[input, output];
				object result = converter(value);
				if (result != null)
					return result;
			}
			return OnFail(value, input, output);
		}

		/// <summary>
		/// Called when a conversion fails except by <see cref="TryChangeType(object, Type, out object)"/>.
		/// This should throw an exception or return a value.
		/// </summary>
		/// <param name="value">The <see cref="object"/> to convert.</param>
		/// <param name="input">The <see cref="Type"/> to convert from.</param>
		/// <param name="output">The <see cref="Type"/> to convert to.</param>
		protected virtual object OnFail(object value, Type input, Type output)
		{
			return null;
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
					Type input = value.GetType();
					Func<object, object> converter = this[input, output];
					result = converter(value);
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
				if (input == typeof(object))
					return this[output];
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
					else
						return outputConverters[(int)typeCode];
				}
				return (value) => ConvertCustom(value, output);
			}
		}
	}
}
