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
		/// <param name="tryParseEnum">Determines if Enum.Parse() or Enum.TryParse() is used.</param>
		public TypeConvertDefault(bool threadSafe = true, bool tryParseEnum = true) : base(threadSafe, tryParseEnum)
		{
			for (int i = 0; i < 19; i++) {
				stringConverters[i] = Conversions.ObjectToString;
				dateTimeConverters[i] = InvalidConversion;
				dbNullConverters[i] = InvalidConversion;
				invalidConverters[i] = InvalidConversion;
			}
			for (int i = 3; i < 17; i++) {
				Func<object, object>[] arr = converterArray[i];
				arr[(int)TypeCode.Empty] = InvalidConversion;
				arr[(int)TypeCode.Object] = InvalidConversion;
				arr[(int)TypeCode.DBNull] = InvalidConversion;
				arr[(int)TypeCode.DateTime] = InvalidConversion;
				arr[17] = InvalidConversion;
				arr[i] = Conversions.None;
			}

			// DateTime
			dateTimeConverters[(int)TypeCode.String] = Conversions.tryParseDateTime;

			// DBNull
			//dbNullConverters[(int)TypeCode.String] = Conversions.ObjectToString;
			dbNullConverters[(int)TypeCode.DBNull] = Conversions.None;
			outputConverters[(int)TypeCode.String] = Conversions.NullableToString;

			// Boolean
			//boolConverters[(int)TypeCode.Boolean] = Conversions.None;
			boolConverters[(int)TypeCode.Char] = InvalidConversion;
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
			boolConverters[(int)TypeCode.String] = Conversions.TryParseBooleanEx;

			// Char
			charConverters[(int)TypeCode.Boolean] = InvalidConversion;
			//charConverters[(int)TypeCode.Char] = Conversions.CharToChar;
			charConverters[(int)TypeCode.SByte] = Conversions.SByteToChar;
			charConverters[(int)TypeCode.Byte] = Conversions.ByteToChar;
			charConverters[(int)TypeCode.Int16] = Conversions.Int16ToChar;
			charConverters[(int)TypeCode.UInt16] = Conversions.UInt16ToChar;
			charConverters[(int)TypeCode.Int32] = Conversions.Int32ToChar;
			charConverters[(int)TypeCode.UInt32] = Conversions.UInt32ToChar;
			charConverters[(int)TypeCode.Int64] = Conversions.Int64ToChar;
			charConverters[(int)TypeCode.UInt64] = Conversions.UInt64ToChar;
			charConverters[(int)TypeCode.Single] = InvalidConversion;
			charConverters[(int)TypeCode.Double] = InvalidConversion;
			charConverters[(int)TypeCode.Decimal] = InvalidConversion;
			charConverters[(int)TypeCode.String] = Conversions.TryParseChar;

			// SByte
			sbyteConverters[(int)TypeCode.Boolean] = Conversions.BooleanToSByte;
			sbyteConverters[(int)TypeCode.Char] = Conversions.CharToSByte;
			//sbyteConverters[(int)TypeCode.SByte] = Conversions.None;
			sbyteConverters[(int)TypeCode.Byte] = Conversions.ByteToSByte;
			sbyteConverters[(int)TypeCode.Int16] = Conversions.Int16ToSByte;
			sbyteConverters[(int)TypeCode.UInt16] = Conversions.UInt16ToSByte;
			sbyteConverters[(int)TypeCode.Int32] = Conversions.Int32ToSByte;
			sbyteConverters[(int)TypeCode.UInt32] = Conversions.UInt32ToSByte;
			sbyteConverters[(int)TypeCode.Int64] = Conversions.Int64ToSByte;
			sbyteConverters[(int)TypeCode.UInt64] = Conversions.UInt64ToSByte;
			sbyteConverters[(int)TypeCode.Single] = Conversions.SingleToSByte;
			sbyteConverters[(int)TypeCode.Double] = Conversions.DoubleToSByte;
			sbyteConverters[(int)TypeCode.Decimal] = Conversions.DecimalToSByte;
			sbyteConverters[(int)TypeCode.String] = Conversions.tryParseSByte;

			// Byte
			byteConverters[(int)TypeCode.Boolean] = Conversions.BooleanToByte;
			byteConverters[(int)TypeCode.Char] = Conversions.CharToByte;
			byteConverters[(int)TypeCode.SByte] = Conversions.SByteToByte;
			//byteConverters[(int)TypeCode.Byte] = Conversions.None;
			byteConverters[(int)TypeCode.Int16] = Conversions.Int16ToByte;
			byteConverters[(int)TypeCode.UInt16] = Conversions.UInt16ToByte;
			byteConverters[(int)TypeCode.Int32] = Conversions.Int32ToByte;
			byteConverters[(int)TypeCode.UInt32] = Conversions.UInt32ToByte;
			byteConverters[(int)TypeCode.Int64] = Conversions.Int64ToByte;
			byteConverters[(int)TypeCode.UInt64] = Conversions.UInt64ToByte;
			byteConverters[(int)TypeCode.Single] = Conversions.SingleToByte;
			byteConverters[(int)TypeCode.Double] = Conversions.DoubleToByte;
			byteConverters[(int)TypeCode.Decimal] = Conversions.DecimalToByte;
			byteConverters[(int)TypeCode.String] = Conversions.tryParseByte;

			// Int16
			int16Converters[(int)TypeCode.Boolean] = Conversions.BooleanToInt16;
			int16Converters[(int)TypeCode.Char] = Conversions.CharToInt16;
			int16Converters[(int)TypeCode.SByte] = Conversions.SByteToInt16;
			int16Converters[(int)TypeCode.Byte] = Conversions.ByteToInt16;
			//int16Converters[(int)TypeCode.Int16] = Conversions.None;
			int16Converters[(int)TypeCode.UInt16] = Conversions.UInt16ToInt16;
			int16Converters[(int)TypeCode.Int32] = Conversions.Int32ToInt16;
			int16Converters[(int)TypeCode.UInt32] = Conversions.UInt32ToInt16;
			int16Converters[(int)TypeCode.Int64] = Conversions.Int64ToInt16;
			int16Converters[(int)TypeCode.UInt64] = Conversions.UInt64ToInt16;
			int16Converters[(int)TypeCode.Single] = Conversions.SingleToInt16;
			int16Converters[(int)TypeCode.Double] = Conversions.DoubleToInt16;
			int16Converters[(int)TypeCode.Decimal] = Conversions.DecimalToInt16;
			int16Converters[(int)TypeCode.String] = Conversions.tryParseInt16;

			// UInt16
			uint16Converters[(int)TypeCode.Boolean] = Conversions.BooleanToUInt16;
			uint16Converters[(int)TypeCode.Char] = Conversions.CharToUInt16;
			uint16Converters[(int)TypeCode.SByte] = Conversions.SByteToUInt16;
			uint16Converters[(int)TypeCode.Byte] = Conversions.ByteToUInt16;
			uint16Converters[(int)TypeCode.Int16] = Conversions.Int16ToUInt16;
			//uint16Converters[(int)TypeCode.UInt16] = Conversions.None;
			uint16Converters[(int)TypeCode.Int32] = Conversions.Int32ToUInt16;
			uint16Converters[(int)TypeCode.UInt32] = Conversions.UInt32ToUInt16;
			uint16Converters[(int)TypeCode.Int64] = Conversions.Int64ToUInt16;
			uint16Converters[(int)TypeCode.UInt64] = Conversions.UInt64ToUInt16;
			uint16Converters[(int)TypeCode.Single] = Conversions.SingleToUInt16;
			uint16Converters[(int)TypeCode.Double] = Conversions.DoubleToUInt16;
			uint16Converters[(int)TypeCode.Decimal] = Conversions.DecimalToUInt16;
			uint16Converters[(int)TypeCode.String] = Conversions.tryParseUInt16;

			// Int32
			int32Converters[(int)TypeCode.Boolean] = Conversions.BooleanToInt32;
			int32Converters[(int)TypeCode.Char] = Conversions.CharToInt32;
			int32Converters[(int)TypeCode.SByte] = Conversions.SByteToInt32;
			int32Converters[(int)TypeCode.Byte] = Conversions.ByteToInt32;
			int32Converters[(int)TypeCode.Int16] = Conversions.Int16ToInt32;
			int32Converters[(int)TypeCode.UInt16] = Conversions.UInt16ToInt32;
			//int32Converters[(int)TypeCode.Int32] = Conversions.None;
			int32Converters[(int)TypeCode.UInt32] = Conversions.UInt32ToInt32;
			int32Converters[(int)TypeCode.Int64] = Conversions.Int64ToInt32;
			int32Converters[(int)TypeCode.UInt64] = Conversions.UInt64ToInt32;
			int32Converters[(int)TypeCode.Single] = Conversions.SingleToInt32;
			int32Converters[(int)TypeCode.Double] = Conversions.DoubleToInt32;
			int32Converters[(int)TypeCode.Decimal] = Conversions.DecimalToInt32;
			int32Converters[(int)TypeCode.String] = Conversions.tryParseInt32;

			// UInt32
			uint32Converters[(int)TypeCode.Boolean] = Conversions.BooleanToUInt32;
			uint32Converters[(int)TypeCode.Char] = Conversions.CharToUInt32;
			uint32Converters[(int)TypeCode.SByte] = Conversions.SByteToUInt32;
			uint32Converters[(int)TypeCode.Byte] = Conversions.ByteToUInt32;
			uint32Converters[(int)TypeCode.Int16] = Conversions.Int16ToUInt32;
			uint32Converters[(int)TypeCode.UInt16] = Conversions.UInt16ToUInt32;
			uint32Converters[(int)TypeCode.Int32] = Conversions.Int32ToUInt32;
			//uint32Converters[(int)TypeCode.UInt32] = Conversions.None;
			uint32Converters[(int)TypeCode.Int64] = Conversions.Int64ToUInt32;
			uint32Converters[(int)TypeCode.UInt64] = Conversions.UInt64ToUInt32;
			uint32Converters[(int)TypeCode.Single] = Conversions.SingleToUInt32;
			uint32Converters[(int)TypeCode.Double] = Conversions.DoubleToUInt32;
			uint32Converters[(int)TypeCode.Decimal] = Conversions.DecimalToUInt32;
			uint32Converters[(int)TypeCode.String] = Conversions.tryParseUInt32;

			// Int64
			int64Converters[(int)TypeCode.Boolean] = Conversions.BooleanToInt64;
			int64Converters[(int)TypeCode.Char] = Conversions.CharToInt64;
			int64Converters[(int)TypeCode.SByte] = Conversions.SByteToInt64;
			int64Converters[(int)TypeCode.Byte] = Conversions.ByteToInt64;
			int64Converters[(int)TypeCode.Int16] = Conversions.Int16ToInt64;
			int64Converters[(int)TypeCode.UInt16] = Conversions.UInt16ToInt64;
			int64Converters[(int)TypeCode.Int32] = Conversions.Int32ToInt64;
			int64Converters[(int)TypeCode.UInt32] = Conversions.UInt32ToInt64;
			//int64Converters[(int)TypeCode.Int64] = Conversions.None;
			int64Converters[(int)TypeCode.UInt64] = Conversions.UInt64ToInt64;
			int64Converters[(int)TypeCode.Single] = Conversions.SingleToInt64;
			int64Converters[(int)TypeCode.Double] = Conversions.DoubleToInt64;
			int64Converters[(int)TypeCode.Decimal] = Conversions.DecimalToInt64;
			int64Converters[(int)TypeCode.String] = Conversions.tryParseInt64;

			// UInt64
			uint64Converters[(int)TypeCode.Boolean] = Conversions.BooleanToUInt64;
			uint64Converters[(int)TypeCode.Char] = Conversions.CharToUInt64;
			uint64Converters[(int)TypeCode.SByte] = Conversions.SByteToUInt64;
			uint64Converters[(int)TypeCode.Byte] = Conversions.ByteToUInt64;
			uint64Converters[(int)TypeCode.Int16] = Conversions.Int16ToUInt64;
			uint64Converters[(int)TypeCode.UInt16] = Conversions.UInt16ToUInt64;
			uint64Converters[(int)TypeCode.Int32] = Conversions.Int32ToUInt64;
			uint64Converters[(int)TypeCode.UInt32] = Conversions.UInt32ToUInt64;
			uint64Converters[(int)TypeCode.Int64] = Conversions.Int64ToUInt64;
			//uint64Converters[(int)TypeCode.UInt64] = Conversions.None;
			uint64Converters[(int)TypeCode.Single] = Conversions.SingleToUInt64;
			uint64Converters[(int)TypeCode.Double] = Conversions.DoubleToUInt64;
			uint64Converters[(int)TypeCode.Decimal] = Conversions.DecimalToUInt64;
			uint64Converters[(int)TypeCode.String] = Conversions.tryParseUInt64;

			// Single
			singleConverters[(int)TypeCode.Boolean] = Conversions.BooleanToSingle;
			singleConverters[(int)TypeCode.Char] = InvalidConversion;
			singleConverters[(int)TypeCode.SByte] = Conversions.SByteToSingle;
			singleConverters[(int)TypeCode.Byte] = Conversions.ByteToSingle;
			singleConverters[(int)TypeCode.Int16] = Conversions.Int16ToSingle;
			singleConverters[(int)TypeCode.UInt16] = Conversions.UInt16ToSingle;
			singleConverters[(int)TypeCode.Int32] = Conversions.Int32ToSingle;
			singleConverters[(int)TypeCode.UInt32] = Conversions.UInt32ToSingle;
			singleConverters[(int)TypeCode.Int64] = Conversions.Int64ToSingle;
			singleConverters[(int)TypeCode.UInt64] = Conversions.UInt64ToSingle;
			//singleConverters[(int)TypeCode.Single] = Conversions.None;
			singleConverters[(int)TypeCode.Double] = Conversions.DoubleToSingle_Unchecked;
			singleConverters[(int)TypeCode.Decimal] = Conversions.DecimalToSingle;
			singleConverters[(int)TypeCode.String] = Conversions.tryParseSingle;

			// Double
			doubleConverters[(int)TypeCode.Boolean] = Conversions.BooleanToDouble;
			doubleConverters[(int)TypeCode.Char] = InvalidConversion;
			doubleConverters[(int)TypeCode.SByte] = Conversions.SByteToDouble;
			doubleConverters[(int)TypeCode.Byte] = Conversions.ByteToDouble;
			doubleConverters[(int)TypeCode.Int16] = Conversions.Int16ToDouble;
			doubleConverters[(int)TypeCode.UInt16] = Conversions.UInt16ToDouble;
			doubleConverters[(int)TypeCode.Int32] = Conversions.Int32ToDouble;
			doubleConverters[(int)TypeCode.UInt32] = Conversions.UInt32ToDouble;
			doubleConverters[(int)TypeCode.Int64] = Conversions.Int64ToDouble;
			doubleConverters[(int)TypeCode.UInt64] = Conversions.UInt64ToDouble;
			doubleConverters[(int)TypeCode.Single] = Conversions.SingleToDouble;
			//doubleConverters[(int)TypeCode.Double] = Conversions.None;
			doubleConverters[(int)TypeCode.Decimal] = Conversions.DecimalToDouble;
			doubleConverters[(int)TypeCode.String] = Conversions.tryParseDouble;

			// Decimal
			decimalConverters[(int)TypeCode.Boolean] = Conversions.BooleanToDecimal;
			decimalConverters[(int)TypeCode.Char] = InvalidConversion;
			decimalConverters[(int)TypeCode.SByte] = Conversions.SByteToDecimal;
			decimalConverters[(int)TypeCode.Byte] = Conversions.ByteToDecimal;
			decimalConverters[(int)TypeCode.Int16] = Conversions.Int16ToDecimal;
			decimalConverters[(int)TypeCode.UInt16] = Conversions.UInt16ToDecimal;
			decimalConverters[(int)TypeCode.Int32] = Conversions.Int32ToDecimal;
			decimalConverters[(int)TypeCode.UInt32] = Conversions.UInt32ToDecimal;
			decimalConverters[(int)TypeCode.Int64] = Conversions.Int64ToDecimal;
			decimalConverters[(int)TypeCode.UInt64] = Conversions.UInt64ToDecimal;
			decimalConverters[(int)TypeCode.Single] = Conversions.SingleToDecimal;
			decimalConverters[(int)TypeCode.Double] = Conversions.DoubleToDecimal;
			//decimalConverters[(int)TypeCode.Decimal] = Conversions.None;
			decimalConverters[(int)TypeCode.String] = Conversions.tryParseDecimal;
		}

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

		protected override Func<object, object> LookupEnum(Type input, TypeCode inputTypeCode, Type output, TypeCode outputTypeCode)
		{
			if (outputTypeCode == TypeCode.String)
				return Conversions.ObjectToString;
			if (inputTypeCode == TypeCode.String) {
				Tuple<Type, Type> inout = Tuple.Create(input, output);
				if (!LookupCache.TryGetValue(inout, out Func<object, object> converter)) {
					converter = TryParseEnum ? Conversions.TryParseEnum(output) : Conversions.ParseEnum(output);
					LookupCache[inout] = converter;
				}
				return converter;
			}
			return this[inputTypeCode, outputTypeCode];
		}

		protected override object OnFail(object value, Type input, Type output)
		{
			return null;
		}
	}
}
