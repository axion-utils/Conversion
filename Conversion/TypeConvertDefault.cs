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

namespace Axion.Conversion
{
	/// <summary>
	/// <see cref="TypeConvertBase"/> which is nearly identical to <see cref="System.Convert"/>. Lookups have been optimized to be faster.
	/// </summary>
	public sealed class TypeConvertDefault : TypeConvertBase
	{
		/// <summary>
		/// Constructs a <see cref="TypeConvertDefault"/> and populates the conversion table with the default values.
		/// </summary>
		/// <param name="threadSafe">Determines if custom conversions use a <see cref="ConcurrentDictionary{TKey, TValue}"/>.</param>
		public TypeConvertDefault(bool threadSafe = true) : base(threadSafe)
		{
			// Output converters
			this[TypeCode.String] = Conversions.NullableToString;

			for (int i = 0; i < 19; i++) {
				this[(TypeCode)i, TypeCode.DBNull] = InvalidConversion;
				this[(TypeCode)i, TypeCode.DateTime] = InvalidConversion;
				this[(TypeCode)i, TypeCode.String] = Conversions.ObjectToString;
				this[(TypeCode)i, (TypeCode)17] = InvalidConversion;
			}
			for (int i = 3; i < 17; i++) {
				this[TypeCode.Empty, (TypeCode)i] = InvalidConversion;
				this[TypeCode.Object, (TypeCode)i] = InvalidConversion;
				this[TypeCode.DBNull, (TypeCode)i] = InvalidConversion;
				this[TypeCode.DateTime, (TypeCode)i] = InvalidConversion;
				this[(TypeCode)17, (TypeCode)i] = InvalidConversion;
				this[(TypeCode)i, (TypeCode)i] = Conversions.None;
			}

			// DBNull
			this[TypeCode.DBNull, TypeCode.DBNull] = Conversions.None;

			// Boolean
			//this[TypeCode.Boolean, TypeCode.Boolean] = Conversions.None;
			this[TypeCode.Char, TypeCode.Boolean] = InvalidConversion;
			this[TypeCode.SByte, TypeCode.Boolean] = Conversions.SByteToBoolean;
			this[TypeCode.Byte, TypeCode.Boolean] = Conversions.ByteToBoolean;
			this[TypeCode.Int16, TypeCode.Boolean] = Conversions.Int16ToBoolean;
			this[TypeCode.UInt16, TypeCode.Boolean] = Conversions.UInt16ToBoolean;
			this[TypeCode.Int32, TypeCode.Boolean] = Conversions.Int32ToBoolean;
			this[TypeCode.UInt32, TypeCode.Boolean] = Conversions.UInt32ToBoolean;
			this[TypeCode.Int64, TypeCode.Boolean] = Conversions.Int64ToBoolean;
			this[TypeCode.UInt64, TypeCode.Boolean] = Conversions.UInt64ToBoolean;
			this[TypeCode.Single, TypeCode.Boolean] = Conversions.SingleToBoolean;
			this[TypeCode.Double, TypeCode.Boolean] = Conversions.DoubleToBoolean;
			this[TypeCode.Decimal, TypeCode.Boolean] = Conversions.DecimalToBoolean;
			this[TypeCode.String, TypeCode.Boolean] = Conversions.TryParseBooleanEx;

			// Char
			this[TypeCode.Boolean, TypeCode.Char] = InvalidConversion;
			//this[TypeCode.Char, TypeCode.Char] = Conversions.None;
			this[TypeCode.SByte, TypeCode.Char] = Conversions.SByteToChar;
			this[TypeCode.Byte, TypeCode.Char] = Conversions.ByteToChar;
			this[TypeCode.Int16, TypeCode.Char] = Conversions.Int16ToChar;
			this[TypeCode.UInt16, TypeCode.Char] = Conversions.UInt16ToChar;
			this[TypeCode.Int32, TypeCode.Char] = Conversions.Int32ToChar;
			this[TypeCode.UInt32, TypeCode.Char] = Conversions.UInt32ToChar;
			this[TypeCode.Int64, TypeCode.Char] = Conversions.Int64ToChar;
			this[TypeCode.UInt64, TypeCode.Char] = Conversions.UInt64ToChar;
			this[TypeCode.Single, TypeCode.Char] = InvalidConversion;
			this[TypeCode.Double, TypeCode.Char] = InvalidConversion;
			this[TypeCode.Decimal, TypeCode.Char] = InvalidConversion;
			this[TypeCode.String, TypeCode.Char] = Conversions.TryParseChar;

			// SByte
			this[TypeCode.Boolean, TypeCode.SByte] = Conversions.BooleanToSByte;
			this[TypeCode.Char, TypeCode.SByte] = Conversions.CharToSByte;
			//this[TypeCode.SByte, TypeCode.SByte] = Conversions.None;
			this[TypeCode.Byte, TypeCode.SByte] = Conversions.ByteToSByte;
			this[TypeCode.Int16, TypeCode.SByte] = Conversions.Int16ToSByte;
			this[TypeCode.UInt16, TypeCode.SByte] = Conversions.UInt16ToSByte;
			this[TypeCode.Int32, TypeCode.SByte] = Conversions.Int32ToSByte;
			this[TypeCode.UInt32, TypeCode.SByte] = Conversions.UInt32ToSByte;
			this[TypeCode.Int64, TypeCode.SByte] = Conversions.Int64ToSByte;
			this[TypeCode.UInt64, TypeCode.SByte] = Conversions.UInt64ToSByte;
			this[TypeCode.Single, TypeCode.SByte] = Conversions.SingleToSByte;
			this[TypeCode.Double, TypeCode.SByte] = Conversions.DoubleToSByte;
			this[TypeCode.Decimal, TypeCode.SByte] = Conversions.DecimalToSByte;
			this[TypeCode.String, TypeCode.SByte] = Conversions.tryParseSByte;

			// Byte
			this[TypeCode.Boolean, TypeCode.Byte] = Conversions.BooleanToByte;
			this[TypeCode.Char, TypeCode.Byte] = Conversions.CharToByte;
			this[TypeCode.SByte, TypeCode.Byte] = Conversions.SByteToByte;
			//this[TypeCode.Byte, TypeCode.Byte] = Conversions.None;
			this[TypeCode.Int16, TypeCode.Byte] = Conversions.Int16ToByte;
			this[TypeCode.UInt16, TypeCode.Byte] = Conversions.UInt16ToByte;
			this[TypeCode.Int32, TypeCode.Byte] = Conversions.Int32ToByte;
			this[TypeCode.UInt32, TypeCode.Byte] = Conversions.UInt32ToByte;
			this[TypeCode.Int64, TypeCode.Byte] = Conversions.Int64ToByte;
			this[TypeCode.UInt64, TypeCode.Byte] = Conversions.UInt64ToByte;
			this[TypeCode.Single, TypeCode.Byte] = Conversions.SingleToByte;
			this[TypeCode.Double, TypeCode.Byte] = Conversions.DoubleToByte;
			this[TypeCode.Decimal, TypeCode.Byte] = Conversions.DecimalToByte;
			this[TypeCode.String, TypeCode.Byte] = Conversions.tryParseByte;

			// Int16
			this[TypeCode.Boolean, TypeCode.Int16] = Conversions.BooleanToInt16;
			this[TypeCode.Char, TypeCode.Int16] = Conversions.CharToInt16;
			this[TypeCode.SByte, TypeCode.Int16] = Conversions.SByteToInt16;
			this[TypeCode.Byte, TypeCode.Int16] = Conversions.ByteToInt16;
			//this[TypeCode.Int16, TypeCode.Int16] = Conversions.None;
			this[TypeCode.UInt16, TypeCode.Int16] = Conversions.UInt16ToInt16;
			this[TypeCode.Int32, TypeCode.Int16] = Conversions.Int32ToInt16;
			this[TypeCode.UInt32, TypeCode.Int16] = Conversions.UInt32ToInt16;
			this[TypeCode.Int64, TypeCode.Int16] = Conversions.Int64ToInt16;
			this[TypeCode.UInt64, TypeCode.Int16] = Conversions.UInt64ToInt16;
			this[TypeCode.Single, TypeCode.Int16] = Conversions.SingleToInt16;
			this[TypeCode.Double, TypeCode.Int16] = Conversions.DoubleToInt16;
			this[TypeCode.Decimal, TypeCode.Int16] = Conversions.DecimalToInt16;
			this[TypeCode.String, TypeCode.Int16] = Conversions.tryParseInt16;

			// UInt16
			this[TypeCode.Boolean, TypeCode.UInt16] = Conversions.BooleanToUInt16;
			this[TypeCode.Char, TypeCode.UInt16] = Conversions.CharToUInt16;
			this[TypeCode.SByte, TypeCode.UInt16] = Conversions.SByteToUInt16;
			this[TypeCode.Byte, TypeCode.UInt16] = Conversions.ByteToUInt16;
			this[TypeCode.Int16, TypeCode.UInt16] = Conversions.Int16ToUInt16;
			//this[TypeCode.UInt16, TypeCode.UInt16] = Conversions.None;
			this[TypeCode.Int32, TypeCode.UInt16] = Conversions.Int32ToUInt16;
			this[TypeCode.UInt32, TypeCode.UInt16] = Conversions.UInt32ToUInt16;
			this[TypeCode.Int64, TypeCode.UInt16] = Conversions.Int64ToUInt16;
			this[TypeCode.UInt64, TypeCode.UInt16] = Conversions.UInt64ToUInt16;
			this[TypeCode.Single, TypeCode.UInt16] = Conversions.SingleToUInt16;
			this[TypeCode.Double, TypeCode.UInt16] = Conversions.DoubleToUInt16;
			this[TypeCode.Decimal, TypeCode.UInt16] = Conversions.DecimalToUInt16;
			this[TypeCode.String, TypeCode.UInt16] = Conversions.tryParseUInt16;

			// Int32
			this[TypeCode.Boolean, TypeCode.Int32] = Conversions.BooleanToInt32;
			this[TypeCode.Char, TypeCode.Int32] = Conversions.CharToInt32;
			this[TypeCode.SByte, TypeCode.Int32] = Conversions.SByteToInt32;
			this[TypeCode.Byte, TypeCode.Int32] = Conversions.ByteToInt32;
			this[TypeCode.Int16, TypeCode.Int32] = Conversions.Int16ToInt32;
			this[TypeCode.UInt16, TypeCode.Int32] = Conversions.UInt16ToInt32;
			//this[TypeCode.Int32, TypeCode.Int32] = Conversions.None;
			this[TypeCode.UInt32, TypeCode.Int32] = Conversions.UInt32ToInt32;
			this[TypeCode.Int64, TypeCode.Int32] = Conversions.Int64ToInt32;
			this[TypeCode.UInt64, TypeCode.Int32] = Conversions.UInt64ToInt32;
			this[TypeCode.Single, TypeCode.Int32] = Conversions.SingleToInt32;
			this[TypeCode.Double, TypeCode.Int32] = Conversions.DoubleToInt32;
			this[TypeCode.Decimal, TypeCode.Int32] = Conversions.DecimalToInt32;
			this[TypeCode.String, TypeCode.Int32] = Conversions.tryParseInt32;

			// UInt32
			this[TypeCode.Boolean, TypeCode.UInt32] = Conversions.BooleanToUInt32;
			this[TypeCode.Char, TypeCode.UInt32] = Conversions.CharToUInt32;
			this[TypeCode.SByte, TypeCode.UInt32] = Conversions.SByteToUInt32;
			this[TypeCode.Byte, TypeCode.UInt32] = Conversions.ByteToUInt32;
			this[TypeCode.Int16, TypeCode.UInt32] = Conversions.Int16ToUInt32;
			this[TypeCode.UInt16, TypeCode.UInt32] = Conversions.UInt16ToUInt32;
			this[TypeCode.Int32, TypeCode.UInt32] = Conversions.Int32ToUInt32;
			//this[TypeCode.UInt32, TypeCode.UInt32] = Conversions.None;
			this[TypeCode.Int64, TypeCode.UInt32] = Conversions.Int64ToUInt32;
			this[TypeCode.UInt64, TypeCode.UInt32] = Conversions.UInt64ToUInt32;
			this[TypeCode.Single, TypeCode.UInt32] = Conversions.SingleToUInt32;
			this[TypeCode.Double, TypeCode.UInt32] = Conversions.DoubleToUInt32;
			this[TypeCode.Decimal, TypeCode.UInt32] = Conversions.DecimalToUInt32;
			this[TypeCode.String, TypeCode.UInt32] = Conversions.tryParseUInt32;

			// Int64
			this[TypeCode.Boolean, TypeCode.Int64] = Conversions.BooleanToInt64;
			this[TypeCode.Char, TypeCode.Int64] = Conversions.CharToInt64;
			this[TypeCode.SByte, TypeCode.Int64] = Conversions.SByteToInt64;
			this[TypeCode.Byte, TypeCode.Int64] = Conversions.ByteToInt64;
			this[TypeCode.Int16, TypeCode.Int64] = Conversions.Int16ToInt64;
			this[TypeCode.UInt16, TypeCode.Int64] = Conversions.UInt16ToInt64;
			this[TypeCode.Int32, TypeCode.Int64] = Conversions.Int32ToInt64;
			this[TypeCode.UInt32, TypeCode.Int64] = Conversions.UInt32ToInt64;
			//this[TypeCode.Int64, TypeCode.Int64] = Conversions.None;
			this[TypeCode.UInt64, TypeCode.Int64] = Conversions.UInt64ToInt64;
			this[TypeCode.Single, TypeCode.Int64] = Conversions.SingleToInt64;
			this[TypeCode.Double, TypeCode.Int64] = Conversions.DoubleToInt64;
			this[TypeCode.Decimal, TypeCode.Int64] = Conversions.DecimalToInt64;
			this[TypeCode.String, TypeCode.Int64] = Conversions.tryParseInt64;

			// UInt64
			this[TypeCode.Boolean, TypeCode.UInt64] = Conversions.BooleanToUInt64;
			this[TypeCode.Char, TypeCode.UInt64] = Conversions.CharToUInt64;
			this[TypeCode.SByte, TypeCode.UInt64] = Conversions.SByteToUInt64;
			this[TypeCode.Byte, TypeCode.UInt64] = Conversions.ByteToUInt64;
			this[TypeCode.Int16, TypeCode.UInt64] = Conversions.Int16ToUInt64;
			this[TypeCode.UInt16, TypeCode.UInt64] = Conversions.UInt16ToUInt64;
			this[TypeCode.Int32, TypeCode.UInt64] = Conversions.Int32ToUInt64;
			this[TypeCode.UInt32, TypeCode.UInt64] = Conversions.UInt32ToUInt64;
			this[TypeCode.Int64, TypeCode.UInt64] = Conversions.Int64ToUInt64;
			//this[TypeCode.UInt64, TypeCode.UInt64] = Conversions.None;
			this[TypeCode.Single, TypeCode.UInt64] = Conversions.SingleToUInt64;
			this[TypeCode.Double, TypeCode.UInt64] = Conversions.DoubleToUInt64;
			this[TypeCode.Decimal, TypeCode.UInt64] = Conversions.DecimalToUInt64;
			this[TypeCode.String, TypeCode.UInt64] = Conversions.tryParseUInt64;

			// Single
			this[TypeCode.Boolean, TypeCode.Single] = Conversions.BooleanToSingle;
			this[TypeCode.Char, TypeCode.Single] = InvalidConversion;
			this[TypeCode.SByte, TypeCode.Single] = Conversions.SByteToSingle;
			this[TypeCode.Byte, TypeCode.Single] = Conversions.ByteToSingle;
			this[TypeCode.Int16, TypeCode.Single] = Conversions.Int16ToSingle;
			this[TypeCode.UInt16, TypeCode.Single] = Conversions.UInt16ToSingle;
			this[TypeCode.Int32, TypeCode.Single] = Conversions.Int32ToSingle;
			this[TypeCode.UInt32, TypeCode.Single] = Conversions.UInt32ToSingle;
			this[TypeCode.Int64, TypeCode.Single] = Conversions.Int64ToSingle;
			this[TypeCode.UInt64, TypeCode.Single] = Conversions.UInt64ToSingle;
			//this[TypeCode.Single, TypeCode.Single] = Conversions.None;
			this[TypeCode.Double, TypeCode.Single] = Conversions.DoubleToSingle_Unchecked;
			this[TypeCode.Decimal, TypeCode.Single] = Conversions.DecimalToSingle;
			this[TypeCode.String, TypeCode.Single] = Conversions.tryParseSingle;

			// Double
			this[TypeCode.Boolean, TypeCode.Double] = Conversions.BooleanToDouble;
			this[TypeCode.Char, TypeCode.Double] = InvalidConversion;
			this[TypeCode.SByte, TypeCode.Double] = Conversions.SByteToDouble;
			this[TypeCode.Byte, TypeCode.Double] = Conversions.ByteToDouble;
			this[TypeCode.Int16, TypeCode.Double] = Conversions.Int16ToDouble;
			this[TypeCode.UInt16, TypeCode.Double] = Conversions.UInt16ToDouble;
			this[TypeCode.Int32, TypeCode.Double] = Conversions.Int32ToDouble;
			this[TypeCode.UInt32, TypeCode.Double] = Conversions.UInt32ToDouble;
			this[TypeCode.Int64, TypeCode.Double] = Conversions.Int64ToDouble;
			this[TypeCode.UInt64, TypeCode.Double] = Conversions.UInt64ToDouble;
			this[TypeCode.Single, TypeCode.Double] = Conversions.SingleToDouble;
			//this[TypeCode.Double, TypeCode.Double] = Conversions.None;
			this[TypeCode.Decimal, TypeCode.Double] = Conversions.DecimalToDouble;
			this[TypeCode.String, TypeCode.Double] = Conversions.tryParseDouble;

			// Decimal
			this[TypeCode.Boolean, TypeCode.Decimal] = Conversions.BooleanToDecimal;
			this[TypeCode.Char, TypeCode.Decimal] = InvalidConversion;
			this[TypeCode.SByte, TypeCode.Decimal] = Conversions.SByteToDecimal;
			this[TypeCode.Byte, TypeCode.Decimal] = Conversions.ByteToDecimal;
			this[TypeCode.Int16, TypeCode.Decimal] = Conversions.Int16ToDecimal;
			this[TypeCode.UInt16, TypeCode.Decimal] = Conversions.UInt16ToDecimal;
			this[TypeCode.Int32, TypeCode.Decimal] = Conversions.Int32ToDecimal;
			this[TypeCode.UInt32, TypeCode.Decimal] = Conversions.UInt32ToDecimal;
			this[TypeCode.Int64, TypeCode.Decimal] = Conversions.Int64ToDecimal;
			this[TypeCode.UInt64, TypeCode.Decimal] = Conversions.UInt64ToDecimal;
			this[TypeCode.Single, TypeCode.Decimal] = Conversions.SingleToDecimal;
			this[TypeCode.Double, TypeCode.Decimal] = Conversions.DoubleToDecimal;
			//this[TypeCode.Decimal, TypeCode.Decimal] = Conversions.None;
			this[TypeCode.String, TypeCode.Decimal] = Conversions.tryParseDecimal;

			// DateTime
			this[TypeCode.String, TypeCode.DateTime] = Conversions.tryParseDateTime;
		}

		/// <summary>
		/// Creates a function for the given conversion where neither input and output are an <see cref="Enum"/>.
		/// </summary>
		/// <param name="input">The <see cref="Type"/> to convert from.</param>
		/// <param name="output">The <see cref="Type"/> to convert to.</param>
		/// <returns>The function that converts an object of the given <see cref="Type"/> to the specified <see cref="Type"/>.</returns>
		protected override Func<object, object> Lookup(Type input, Type output)
		{
			if (output == typeof(string))
				return Conversions.ObjectToString;
			if (output.IsAssignableFrom(input))
				return Conversions.None;
			Tuple<Type, Type> inout = Tuple.Create(input, output);
			if (!LookupCache.TryGetValue(inout, out Func<object, object> converter)) {
				converter = (input == typeof(string) ? Conversions.TryParse(output) : null)
					?? Conversions.ImplicitCast(input, output)
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
		/// Creates a function for the given conversion which includes at least one <see cref="Enum"/>.
		/// </summary>
		/// <param name="input">The <see cref="Type"/> to convert from.</param>
		/// <param name="inputTypeCode">The <see cref="TypeCode"/> of <paramref name="input"/>.</param>
		/// <param name="output">The <see cref="Type"/> to convert to.</param>
		/// <param name="outputTypeCode">The <see cref="TypeCode"/> of <paramref name="output"/>.</param>
		protected override Func<object, object> LookupEnum(Type input, TypeCode inputTypeCode, Type output, TypeCode outputTypeCode)
		{
			if (outputTypeCode == TypeCode.String)
				return Conversions.ObjectToString;
			if (inputTypeCode == TypeCode.String) {
				Tuple<Type, Type> inout = Tuple.Create(input, output);
				if (!LookupCache.TryGetValue(inout, out Func<object, object> converter)) {
					converter = Conversions.TryParseEnum(output);
					LookupCache[inout] = converter;
				}
				return converter;
			}
			return this[inputTypeCode, outputTypeCode];
		}

		/// <summary>
		/// Called by <see cref="TypeConvertBase.ChangeType(object, Type)"/> when a conversion returns <see langword="null"/>.
		/// This should throw an exception or return a value. By default this returns <see langword="null/"/>.
		/// </summary>
		/// <param name="value">The <see cref="object"/> to convert.</param>
		/// <param name="input">The <see cref="Type"/> to convert from.</param>
		/// <param name="output">The <see cref="Type"/> to convert to.</param>
		protected override object OnFail(object value, Type input, Type output)
		{
			return null;
		}
	}
}
