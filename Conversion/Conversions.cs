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
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;

namespace Axion.Conversion
{
	/// <summary>
	/// Contains a collection of conversion functions. These are intended for use with <see cref="TypeConvert"/>.
	/// </summary>
	public static class Conversions
	{
		static Conversions()
		{
			TryParseEnumMethod = typeof(Enum).GetMethods(BindingFlags.Static | BindingFlags.Public).Where(m => m.Name == "TryParse")
				.FirstOrDefault(m => m.GetParameters().Length == 3);
			UncheckedConversions = new Func<object, object>[19][] {
				invalidCasts, // 0 = Empty 
				invalidCasts, // 1 = Object
				invalidCasts, // 2 = DBNull
				boolCasts, // 3 = Boolean
				charUnchecked, // 4 = Char
				sbyteUnchecked, // 5 = SByte
				byteUnchecked, // 6 = Byte
				int16Unchecked, // 7 = Int16
				uint16Unchecked, // 8 = UInt16
				int32Unchecked, // 9 = Int32
				uint32Unchecked, // 10 = UInt32
				int64Unchecked, // 11 = Int64
				uint64Unchecked, // 12 = UInt64
				singleUnchecked, // 13 = Single
				doubleCasts, // 14 = Double
				decimalUnchecked, // 15 = Decimal
				datetimeCasts, // 16 = DateTime
				invalidCasts, // 17 = ??
				stringCasts, // 18 = String
			};
			CheckedConversions = new Func<object, object>[19][] {
				invalidCasts, // 0 = Empty 
				invalidCasts, // 1 = Object
				invalidCasts, // 2 = DBNull
				boolCasts, // 3 = Boolean
				charChecked, // 4 = Char
				sbyteChecked, // 5 = SByte
				byteChecked, // 6 = Byte
				int16Checked, // 7 = Int16
				uint16Checked, // 8 = UInt16
				int32Checked, // 9 = Int32
				uint32Checked, // 10 = UInt32
				int64Checked, // 11 = Int64
				uint64Checked, // 12 = UInt64
				singleChecked, // 13 = Single
				doubleCasts, // 14 = Double
				decimalChecked, // 15 = Decimal
				datetimeCasts, // 16 = DateTime
				invalidCasts, // 17 = ??
				stringCasts, // 18 = String
			};
			TryParseConversions = new Func<object, object>[19] {
				AsNull, // 0 = Empty 
				null, // 1 = Object
				null, // 2 = DBNull
				TryParseBoolean, // 3 = Boolean
				TryParseChar, // 4 = Char
				tryParseSByte, // 5 = SByte
				tryParseByte, // 6 = Byte
				tryParseInt16, // 7 = Int16
				tryParseUInt16, // 8 = UInt16
				tryParseInt32, // 9 = Int32
				tryParseUInt32, // 10 = UInt32
				tryParseInt64, // 11 = Int64
				tryParseUInt64, // 12 = UInt64
				tryParseSingle, // 13 = Single
				tryParseDouble, // 14 = Double
				tryParseDecimal, // 15 = Decimal
				tryParseDateTime, // 16 = DateTime
				null, // 17 = ??
				None, // 18 = String
			};
			ParseConversions = new Func<object, object>[19] {
				AsNull, // 0 = Empty 
				null, // 1 = Object
				null, // 2 = DBNull
				ParseBoolean, // 3 = Boolean
				ParseChar, // 4 = Char
				parseSByte, // 5 = SByte
				parseByte, // 6 = Byte
				parseInt16, // 7 = Int16
				parseUInt16, // 8 = UInt16
				parseInt32, // 9 = Int32
				parseUInt32, // 10 = UInt32
				parseInt64, // 11 = Int64
				parseUInt64, // 12 = UInt64
				parseSingle, // 13 = Single
				parseDouble, // 14 = Double
				parseDecimal, // 15 = Decimal
				parseDateTime, // 16 = DateTime
				null, // 17 = ??
				None, // 18 = String
			};
		}

		/// <summary>
		/// Always returns null. This is the default converter when no conversion exists.
		/// </summary>
		public static readonly Func<object, object> AsNull = (object value) => null;
		/// <summary>
		/// Returns the same object that was input. This is the default converter when the output type is assignable from the input type.
		/// </summary>
		public static readonly Func<object, object> None = (object value) => value;
		/// <summary>
		/// Returns the <see cref="object.ToString"/> method on the object.
		/// </summary>
		public static readonly Func<object, object> ObjectToString = (object value) => value.ToString();

		/// <summary>
		/// Create a converter using multiple methods in sequence: implicit/subclass cast, explicit cast, ToString, TryParse/Parse, TypeConverter, Constructor. 
		/// If there is no known conversion then <see langword="null"/> is returned.
		/// </summary>
		/// <param name="input">The type to convert from.</param>
		/// <param name="output">The type to convert to.</param>
		/// <param name="exceptionSafe">False means that numeric conversions will be checked for overflow and string conversions will use the Parse method.
		/// True means that numeric conversions will not be checked for overflow and string conversions will use the TryParse method.</param>
		/// <returns>A conversion method for converting from the input type to the output type or <see langword="null"/> if there is no conversion.</returns>
		public static Func<object, object> BestMatch(Type input, Type output, bool exceptionSafe = false)
		{
			if (output.IsAssignableFrom(input))
				return None;
			if (output == typeof(string))
				return ObjectToString;
			Func<object, object> converter;
			if (input == typeof(string)) {
				converter = exceptionSafe ? TryParse(input, false) : Parse(input, false);
				if (converter != null)
					return converter;
			}
			converter = Cast(input, output, !exceptionSafe);
			if (converter != null)
				return converter;
			converter = TypeConverter(input, output);
			if (converter != null)
				return converter;
			converter = Constructor(input, output);
			return converter;
		}

		#region Boolean
		/// <summary>
		/// Converts <see langword="true"/> to 1 and <see langword="false"/> to 0.
		/// /// </summary>
		public static readonly Func<object, object> BooleanToSByte = (object value) => ((bool)value) ? (sbyte)1 : (sbyte)0;
		/// <summary>
		/// Converts <see langword="true"/> to 1 and <see langword="false"/> to 0.
		/// /// </summary>
		public static readonly Func<object, object> BooleanToByte = (object value) => ((bool)value) ? (byte)1 : (byte)0;
		/// <summary>
		/// Converts <see langword="true"/> to 1 and <see langword="false"/> to 0.
		/// /// </summary>
		public static readonly Func<object, object> BooleanToInt16 = (object value) => ((bool)value) ? (short)1 : (short)0;
		/// <summary>
		/// Converts <see langword="true"/> to 1 and <see langword="false"/> to 0.
		/// /// </summary>
		public static readonly Func<object, object> BooleanToUInt16 = (object value) => ((bool)value) ? (ushort)1 : (ushort)0;
		/// <summary>
		/// Converts <see langword="true"/> to 1 and <see langword="false"/> to 0.
		/// /// </summary>
		public static readonly Func<object, object> BooleanToInt32 = (object value) => ((bool)value) ? (int)1 : (int)0;
		/// <summary>
		/// Converts <see langword="true"/> to 1 and <see langword="false"/> to 0.
		/// /// </summary>
		public static readonly Func<object, object> BooleanToUInt32 = (object value) => ((bool)value) ? (uint)1U : (uint)0U;
		/// <summary>
		/// Converts <see langword="true"/> to 1 and <see langword="false"/> to 0.
		/// /// </summary>
		public static readonly Func<object, object> BooleanToInt64 = (object value) => ((bool)value) ? (long)1L : (long)0L;
		/// <summary>
		/// Converts <see langword="true"/> to 1 and <see langword="false"/> to 0.
		/// /// </summary>
		public static readonly Func<object, object> BooleanToUInt64 = (object value) => ((bool)value) ? (ulong)1UL : (ulong)0UL;
		/// <summary>
		/// Converts <see langword="true"/> to 1 and <see langword="false"/> to 0.
		/// /// </summary>
		public static readonly Func<object, object> BooleanToSingle = (object value) => ((bool)value) ? (float)1.0f : (float)0.0f;
		/// <summary>
		/// Converts <see langword="true"/> to 1 and <see langword="false"/> to 0.
		/// /// </summary>
		public static readonly Func<object, object> BooleanToDouble = (object value) => ((bool)value) ? (double)1D : (double)0D;
		/// <summary>
		/// Converts <see langword="true"/> to 1 and <see langword="false"/> to 0.
		/// /// </summary>
		public static readonly Func<object, object> BooleanToDecimal = (object value) => ((bool)value) ? (decimal)1M : (decimal)0M;
		/// <summary>
		/// Converts <see langword="true"/> to 1 and <see langword="false"/> to 0.
		/// /// </summary>
		public static readonly Func<object, object> BooleanToBigInteger = (object value) => ((bool)value) ? new BigInteger(1) : new BigInteger(0);

		/// <summary>
		/// Converts 0 to <see langword="false"/> and all other values to <see langword="true"/>
		/// </summary>
		public static readonly Func<object, object> SByteToBoolean = (object value) => ((sbyte)value) != 0;
		/// <summary>
		/// Converts 0 to <see langword="false"/> and all other values to <see langword="true"/>
		/// </summary>
		public static readonly Func<object, object> ByteToBoolean = (object value) => ((byte)value) != 0;
		/// <summary>
		/// Converts 0 to <see langword="false"/> and all other values to <see langword="true"/>
		/// </summary>
		public static readonly Func<object, object> Int16ToBoolean = (object value) => ((short)value) != 0;
		/// <summary>
		/// Converts 0 to <see langword="false"/> and all other values to <see langword="true"/>
		/// </summary>
		public static readonly Func<object, object> UInt16ToBoolean = (object value) => ((ushort)value) != 0;
		/// <summary>
		/// Converts 0 to <see langword="false"/> and all other values to <see langword="true"/>
		/// </summary>
		public static readonly Func<object, object> Int32ToBoolean = (object value) => ((int)value) != 0;
		/// <summary>
		/// Converts 0 to <see langword="false"/> and all other values to <see langword="true"/>
		/// </summary>
		public static readonly Func<object, object> UInt32ToBoolean = (object value) => ((uint)value) != 0;
		/// <summary>
		/// Converts 0 to <see langword="false"/> and all other values to <see langword="true"/>
		/// </summary>
		public static readonly Func<object, object> Int64ToBoolean = (object value) => ((long)value) != 0;
		/// <summary>
		/// Converts 0 to <see langword="false"/> and all other values to <see langword="true"/>
		/// </summary>
		public static readonly Func<object, object> UInt64ToBoolean = (object value) => ((ulong)value) != 0;
		/// <summary>
		/// Converts 0 to <see langword="false"/> and all other values to <see langword="true"/>
		/// </summary>
		public static readonly Func<object, object> SingleToBoolean = (object value) => ((float)value) != 0.0f;
		/// <summary>
		/// Converts 0 to <see langword="false"/> and all other values to <see langword="true"/>
		/// </summary>
		public static readonly Func<object, object> DoubleToBoolean = (object value) => ((double)value) != 0.0D;
		/// <summary>
		/// Converts 0 to <see langword="false"/> and all other values to <see langword="true"/>
		/// </summary>
		public static readonly Func<object, object> DecimalToBoolean = (object value) => ((decimal)value) != 0.0M;
		#endregion Boolean

		#region Char
		// Checked (null)
		public static readonly Func<object, object> CharToSByte = (object value) => {
			char val = (char)value;
			return val > sbyte.MaxValue ? (object)null : (sbyte)val;
		};
		public static readonly Func<object, object> CharToByte = (object value) => {
			char val = (char)value;
			return val > byte.MaxValue ? (object)null : (byte)val;
		};
		public static readonly Func<object, object> CharToInt16 = (object value) => {
			char val = (char)value;
			return val > short.MaxValue ? (object)null : (short)val;
		};

		// Default
		public static readonly Func<object, object> CharToUInt16 = (object value) => (ushort)(char)value;
		public static readonly Func<object, object> CharToInt32 = (object value) => (int)(char)value;
		public static readonly Func<object, object> CharToUInt32 = (object value) => (uint)(char)value;
		public static readonly Func<object, object> CharToInt64 = (object value) => (long)(char)value;
		public static readonly Func<object, object> CharToUInt64 = (object value) => (ulong)(char)value;
		public static readonly Func<object, object> CharToSingle = (object value) => (float)(char)value;
		public static readonly Func<object, object> CharToDouble = (object value) => (double)(char)value;
		public static readonly Func<object, object> CharToDecimal = (object value) => (decimal)(char)value;
		public static readonly Func<object, object> CharToBigInteger = (object value) => (BigInteger)(char)value;

		// Unchecked
		public static readonly Func<object, object> CharToSByte_Unchecked = (object value) => unchecked((sbyte)(char)value);
		public static readonly Func<object, object> CharToByte_Unchecked = (object value) => unchecked((byte)(char)value);
		public static readonly Func<object, object> CharToInt16_Unchecked = (object value) => unchecked((short)(char)value);

		// Checked
		public static readonly Func<object, object> CharToSByte_Checked = (object value) => checked((sbyte)(char)value);
		public static readonly Func<object, object> CharToByte_Checked = (object value) => checked((byte)(char)value);
		public static readonly Func<object, object> CharToInt16_Checked = (object value) => checked((short)(char)value);
		#endregion Char

		#region SByte
		// Checked (null)
		public static readonly Func<object, object> SByteToChar = (object value) => {
			sbyte val = (sbyte)value;
			return val < char.MinValue ? (object)null : (char)val;
		};
		public static readonly Func<object, object> SByteToByte = (object value) => {
			sbyte val = (sbyte)value;
			return val < 0 ? (object)null : (byte)val;
		};
		public static readonly Func<object, object> SByteToUInt16 = (object value) => {
			sbyte val = (sbyte)value;
			return val < 0 ? (object)null : (ushort)val;
		};
		public static readonly Func<object, object> SByteToUInt32 = (object value) => {
			sbyte val = (sbyte)value;
			return val < 0 ? (object)null : (uint)val;
		};
		public static readonly Func<object, object> SByteToUInt64 = (object value) => {
			sbyte val = (sbyte)value;
			return val < 0 ? (object)null : (ulong)val;
		};

		// Default
		public static readonly Func<object, object> SByteToInt16 = (object value) => (short)(sbyte)value;
		public static readonly Func<object, object> SByteToInt32 = (object value) => (int)(sbyte)value;
		public static readonly Func<object, object> SByteToInt64 = (object value) => (long)(sbyte)value;
		public static readonly Func<object, object> SByteToSingle = (object value) => (float)(sbyte)value;
		public static readonly Func<object, object> SByteToDouble = (object value) => (double)(sbyte)value;
		public static readonly Func<object, object> SByteToDecimal = (object value) => (decimal)(sbyte)value;
		public static readonly Func<object, object> SByteToBigInteger = (object value) => (BigInteger)(sbyte)value;

		// Unchecked
		public static readonly Func<object, object> SByteToChar_Unchecked = (object value) => unchecked((char)(sbyte)value);
		public static readonly Func<object, object> SByteToByte_Unchecked = (object value) => unchecked((byte)(sbyte)value);
		public static readonly Func<object, object> SByteToUInt16_Unchecked = (object value) => unchecked((ushort)(sbyte)value);
		public static readonly Func<object, object> SByteToUInt32_Unchecked = (object value) => unchecked((uint)(sbyte)value);
		public static readonly Func<object, object> SByteToUInt64_Unchecked = (object value) => unchecked((ulong)(sbyte)value);

		// Checked
		public static readonly Func<object, object> SByteToChar_Checked = (object value) => checked((char)(sbyte)value);
		public static readonly Func<object, object> SByteToByte_Checked = (object value) => checked((byte)(sbyte)value);
		public static readonly Func<object, object> SByteToUInt16_Checked = (object value) => checked((ushort)(sbyte)value);
		public static readonly Func<object, object> SByteToUInt32_Checked = (object value) => checked((uint)(sbyte)value);
		public static readonly Func<object, object> SByteToUInt64_Checked = (object value) => checked((ulong)(sbyte)value);
		#endregion SByte

		#region Byte
		// Checked (null)
		public static readonly Func<object, object> ByteToSByte = (object value) => {
			byte val = (byte)value;
			return val > sbyte.MaxValue ? (object)null : (sbyte)val;
		};

		// Default
		public static readonly Func<object, object> ByteToChar = (object value) => (char)(byte)value;
		public static readonly Func<object, object> ByteToInt16 = (object value) => (short)(byte)value;
		public static readonly Func<object, object> ByteToUInt16 = (object value) => (ushort)(byte)value;
		public static readonly Func<object, object> ByteToInt32 = (object value) => (int)(byte)value;
		public static readonly Func<object, object> ByteToUInt32 = (object value) => (uint)(byte)value;
		public static readonly Func<object, object> ByteToInt64 = (object value) => (long)(byte)value;
		public static readonly Func<object, object> ByteToUInt64 = (object value) => (ulong)(byte)value;
		public static readonly Func<object, object> ByteToSingle = (object value) => (float)(byte)value;
		public static readonly Func<object, object> ByteToDouble = (object value) => (double)(byte)value;
		public static readonly Func<object, object> ByteToDecimal = (object value) => (decimal)(byte)value;
		public static readonly Func<object, object> ByteToBigInteger = (object value) => (BigInteger)(byte)value;

		// Unchecked
		public static readonly Func<object, object> ByteToSByte_Unchecked = (object value) => unchecked((sbyte)(byte)value);

		// Checked
		public static readonly Func<object, object> ByteToSByte_Checked = (object value) => checked((sbyte)(byte)value);
		#endregion Byte

		#region Int16
		// Checked (null)
		public static readonly Func<object, object> Int16ToChar = (object value) => {
			short val = (short)value;
			return val < char.MinValue ? (object)null : (char)val;
		};
		public static readonly Func<object, object> Int16ToSByte = (object value) => {
			short val = (short)value;
			return val < sbyte.MinValue || val > sbyte.MaxValue ? (object)null : (sbyte)val;
		};
		public static readonly Func<object, object> Int16ToByte = (object value) => {
			short val = (short)value;
			return val < 0 || val > byte.MaxValue ? (object)null : (byte)val;
		};
		public static readonly Func<object, object> Int16ToUInt16 = (object value) => {
			short val = (short)value;
			return val < 0 ? (object)null : (ushort)val;
		};
		public static readonly Func<object, object> Int16ToUInt32 = (object value) => {
			short val = (short)value;
			return val < 0 ? (object)null : (uint)val;
		};
		public static readonly Func<object, object> Int16ToUInt64 = (object value) => {
			short val = (short)value;
			return val < 0 ? (object)null : (ulong)val;
		};

		// Default
		public static readonly Func<object, object> Int16ToInt32 = (object value) => (int)(short)value;
		public static readonly Func<object, object> Int16ToInt64 = (object value) => (long)(short)value;
		public static readonly Func<object, object> Int16ToSingle = (object value) => (float)(short)value;
		public static readonly Func<object, object> Int16ToDouble = (object value) => (double)(short)value;
		public static readonly Func<object, object> Int16ToDecimal = (object value) => (decimal)(short)value;
		public static readonly Func<object, object> Int16ToBigInteger = (object value) => (BigInteger)(short)value;

		// Unchecked
		public static readonly Func<object, object> Int16ToChar_Unchecked = (object value) => unchecked((char)(short)value);
		public static readonly Func<object, object> Int16ToSByte_Unchecked = (object value) => unchecked((sbyte)(short)value);
		public static readonly Func<object, object> Int16ToByte_Unchecked = (object value) => unchecked((byte)(short)value);
		public static readonly Func<object, object> Int16ToUInt16_Unchecked = (object value) => unchecked((ushort)(short)value);
		public static readonly Func<object, object> Int16ToUInt32_Unchecked = (object value) => unchecked((uint)(short)value);
		public static readonly Func<object, object> Int16ToUInt64_Unchecked = (object value) => unchecked((ulong)(short)value);

		// Checked
		public static readonly Func<object, object> Int16ToChar_Checked = (object value) => checked((char)(short)value);
		public static readonly Func<object, object> Int16ToSByte_Checked = (object value) => checked((sbyte)(short)value);
		public static readonly Func<object, object> Int16ToByte_Checked = (object value) => checked((byte)(short)value);
		public static readonly Func<object, object> Int16ToUInt16_Checked = (object value) => checked((ushort)(short)value);
		public static readonly Func<object, object> Int16ToUInt32_Checked = (object value) => checked((uint)(short)value);
		public static readonly Func<object, object> Int16ToUInt64_Checked = (object value) => checked((ulong)(short)value);
		#endregion Int16

		#region UInt16
		// Checked (null)
		public static readonly Func<object, object> UInt16ToSByte = (object value) => {
			ushort val = (ushort)value;
			return val > sbyte.MaxValue ? (object)null : (sbyte)val;
		};
		public static readonly Func<object, object> UInt16ToByte = (object value) => {
			ushort val = (ushort)value;
			return val > byte.MaxValue ? (object)null : (byte)val;
		};
		public static readonly Func<object, object> UInt16ToInt16 = (object value) => {
			ushort val = (ushort)value;
			return val > short.MaxValue ? (object)null : (short)val;
		};

		// Default
		public static readonly Func<object, object> UInt16ToChar = (object value) => (char)(ushort)value;
		public static readonly Func<object, object> UInt16ToInt32 = (object value) => (int)(ushort)value;
		public static readonly Func<object, object> UInt16ToUInt32 = (object value) => (uint)(ushort)value;
		public static readonly Func<object, object> UInt16ToInt64 = (object value) => (long)(ushort)value;
		public static readonly Func<object, object> UInt16ToUInt64 = (object value) => (ulong)(ushort)value;
		public static readonly Func<object, object> UInt16ToSingle = (object value) => (float)(ushort)value;
		public static readonly Func<object, object> UInt16ToDouble = (object value) => (double)(ushort)value;
		public static readonly Func<object, object> UInt16ToDecimal = (object value) => (decimal)(ushort)value;
		public static readonly Func<object, object> UInt16ToBigInteger = (object value) => (BigInteger)(ushort)value;

		// Unchecked
		public static readonly Func<object, object> UInt16ToSByte_Unchecked = (object value) => unchecked((sbyte)(ushort)value);
		public static readonly Func<object, object> UInt16ToByte_Unchecked = (object value) => unchecked((byte)(ushort)value);
		public static readonly Func<object, object> UInt16ToInt16_Unchecked = (object value) => unchecked((short)(ushort)value);

		// Checked
		public static readonly Func<object, object> UInt16ToSByte_Checked = (object value) => checked((sbyte)(ushort)value);
		public static readonly Func<object, object> UInt16ToByte_Checked = (object value) => checked((byte)(ushort)value);
		public static readonly Func<object, object> UInt16ToInt16_Checked = (object value) => checked((short)(ushort)value);
		#endregion UInt16

		#region Int32
		// Checked (null)
		public static readonly Func<object, object> Int32ToChar = (object value) => {
			int val = (int)value;
			return val < char.MinValue || val > char.MaxValue ? (object)null : (char)val;
		};
		public static readonly Func<object, object> Int32ToSByte = (object value) => {
			int val = (int)value;
			return val < sbyte.MinValue || val > sbyte.MaxValue ? (object)null : (sbyte)val;
		};
		public static readonly Func<object, object> Int32ToByte = (object value) => {
			int val = (int)value;
			return val < 0 || val > byte.MaxValue ? (object)null : (byte)val;
		};
		public static readonly Func<object, object> Int32ToInt16 = (object value) => {
			int val = (int)value;
			return val < short.MinValue || val > short.MaxValue ? (object)null : (short)val;
		};
		public static readonly Func<object, object> Int32ToUInt16 = (object value) => {
			int val = (int)value;
			return val < 0 || val > ushort.MaxValue ? (object)null : (ushort)val;
		};
		public static readonly Func<object, object> Int32ToUInt32 = (object value) => {
			int val = (int)value;
			return val < 0 ? (object)null : (uint)val;
		};
		public static readonly Func<object, object> Int32ToUInt64 = (object value) => {
			int val = (int)value;
			return val < 0 ? (object)null : (ulong)val;
		};

		// Default
		public static readonly Func<object, object> Int32ToInt64 = (object value) => (long)(int)value;
		public static readonly Func<object, object> Int32ToSingle = (object value) => (float)(int)value;
		public static readonly Func<object, object> Int32ToDouble = (object value) => (double)(int)value;
		public static readonly Func<object, object> Int32ToDecimal = (object value) => (decimal)(int)value;
		public static readonly Func<object, object> Int32ToBigInteger = (object value) => (BigInteger)(int)value;

		// Unchecked
		public static readonly Func<object, object> Int32ToChar_Unchecked = (object value) => unchecked((char)(int)value);
		public static readonly Func<object, object> Int32ToSByte_Unchecked = (object value) => unchecked((sbyte)(int)value);
		public static readonly Func<object, object> Int32ToByte_Unchecked = (object value) => unchecked((byte)(int)value);
		public static readonly Func<object, object> Int32ToInt16_Unchecked = (object value) => unchecked((short)(int)value);
		public static readonly Func<object, object> Int32ToUInt16_Unchecked = (object value) => unchecked((ushort)(int)value);
		public static readonly Func<object, object> Int32ToUInt32_Unchecked = (object value) => unchecked((uint)(int)value);
		public static readonly Func<object, object> Int32ToUInt64_Unchecked = (object value) => unchecked((ulong)(int)value);

		// Checked
		public static readonly Func<object, object> Int32ToChar_Checked = (object value) => checked((char)(int)value);
		public static readonly Func<object, object> Int32ToSByte_Checked = (object value) => checked((sbyte)(int)value);
		public static readonly Func<object, object> Int32ToByte_Checked = (object value) => checked((byte)(int)value);
		public static readonly Func<object, object> Int32ToInt16_Checked = (object value) => checked((short)(int)value);
		public static readonly Func<object, object> Int32ToUInt16_Checked = (object value) => checked((ushort)(int)value);
		public static readonly Func<object, object> Int32ToUInt32_Checked = (object value) => checked((uint)(int)value);
		public static readonly Func<object, object> Int32ToUInt64_Checked = (object value) => checked((ulong)(int)value);
		#endregion Int32

		#region UInt32
		// Checked (null)
		public static readonly Func<object, object> UInt32ToChar = (object value) => {
			uint val = (uint)value;
			return val > char.MaxValue ? (object)null : (char)val;
		};
		public static readonly Func<object, object> UInt32ToSByte = (object value) => {
			uint val = (uint)value;
			return val > sbyte.MaxValue ? (object)null : (sbyte)val;
		};
		public static readonly Func<object, object> UInt32ToByte = (object value) => {
			uint val = (uint)value;
			return val > byte.MaxValue ? (object)null : (byte)val;
		};
		public static readonly Func<object, object> UInt32ToInt16 = (object value) => {
			uint val = (uint)value;
			return val > short.MaxValue ? (object)null : (short)val;
		};
		public static readonly Func<object, object> UInt32ToUInt16 = (object value) => {
			uint val = (uint)value;
			return val > ushort.MaxValue ? (object)null : (ushort)val;
		};
		public static readonly Func<object, object> UInt32ToInt32 = (object value) => {
			uint val = (uint)value;
			return val > int.MaxValue ? (object)null : (int)val;
		};

		// Default
		public static readonly Func<object, object> UInt32ToInt64 = (object value) => (long)(uint)value;
		public static readonly Func<object, object> UInt32ToUInt64 = (object value) => (ulong)(uint)value;
		public static readonly Func<object, object> UInt32ToSingle = (object value) => (float)(uint)value;
		public static readonly Func<object, object> UInt32ToDouble = (object value) => (double)(uint)value;
		public static readonly Func<object, object> UInt32ToDecimal = (object value) => (decimal)(uint)value;
		public static readonly Func<object, object> UInt32ToBigInteger = (object value) => (BigInteger)(uint)value;

		// Unchecked
		public static readonly Func<object, object> UInt32ToChar_Unchecked = (object value) => unchecked((char)(uint)value);
		public static readonly Func<object, object> UInt32ToSByte_Unchecked = (object value) => unchecked((sbyte)(uint)value);
		public static readonly Func<object, object> UInt32ToByte_Unchecked = (object value) => unchecked((byte)(uint)value);
		public static readonly Func<object, object> UInt32ToInt16_Unchecked = (object value) => unchecked((short)(uint)value);
		public static readonly Func<object, object> UInt32ToUInt16_Unchecked = (object value) => unchecked((ushort)(uint)value);
		public static readonly Func<object, object> UInt32ToInt32_Unchecked = (object value) => unchecked((int)(uint)value);

		// Checked
		public static readonly Func<object, object> UInt32ToChar_Checked = (object value) => checked((char)(uint)value);
		public static readonly Func<object, object> UInt32ToSByte_Checked = (object value) => checked((sbyte)(uint)value);
		public static readonly Func<object, object> UInt32ToByte_Checked = (object value) => checked((byte)(uint)value);
		public static readonly Func<object, object> UInt32ToInt16_Checked = (object value) => checked((short)(uint)value);
		public static readonly Func<object, object> UInt32ToUInt16_Checked = (object value) => checked((ushort)(uint)value);
		public static readonly Func<object, object> UInt32ToInt32_Checked = (object value) => checked((int)(uint)value);
		#endregion UInt32

		#region Int64
		// Checked (null)
		public static readonly Func<object, object> Int64ToChar = (object value) => {
			long val = (long)value;
			return val < char.MinValue || val > char.MaxValue ? (object)null : (char)val;
		};
		public static readonly Func<object, object> Int64ToSByte = (object value) => {
			long val = (long)value;
			return val < sbyte.MinValue || val > sbyte.MaxValue ? (object)null : (sbyte)val;
		};
		public static readonly Func<object, object> Int64ToByte = (object value) => {
			long val = (long)value;
			return val < 0 || val > byte.MaxValue ? (object)null : (byte)val;
		};
		public static readonly Func<object, object> Int64ToInt16 = (object value) => {
			long val = (long)value;
			return val < short.MinValue || val > short.MaxValue ? (object)null : (short)val;
		};
		public static readonly Func<object, object> Int64ToUInt16 = (object value) => {
			long val = (long)value;
			return val < 0 || val > ushort.MaxValue ? (object)null : (ushort)val;
		};
		public static readonly Func<object, object> Int64ToInt32 = (object value) => {
			long val = (long)value;
			return val < int.MinValue || val > int.MaxValue ? (object)null : (int)val;
		};
		public static readonly Func<object, object> Int64ToUInt32 = (object value) => {
			long val = (long)value;
			return val < 0 || val > uint.MaxValue ? (object)null : (uint)val;
		};
		public static readonly Func<object, object> Int64ToUInt64 = (object value) => {
			long val = (long)value;
			return val < 0 ? (object)null : (ulong)val;
		};

		// Default
		public static readonly Func<object, object> Int64ToSingle = (object value) => (float)(long)value;
		public static readonly Func<object, object> Int64ToDouble = (object value) => (double)(long)value;
		public static readonly Func<object, object> Int64ToDecimal = (object value) => (decimal)(long)value;
		public static readonly Func<object, object> Int64ToBigInteger = (object value) => (BigInteger)(long)value;

		// Unchecked
		public static readonly Func<object, object> Int64ToChar_Unchecked = (object value) => unchecked((char)(long)value);
		public static readonly Func<object, object> Int64ToSByte_Unchecked = (object value) => unchecked((sbyte)(long)value);
		public static readonly Func<object, object> Int64ToByte_Unchecked = (object value) => unchecked((byte)(long)value);
		public static readonly Func<object, object> Int64ToInt16_Unchecked = (object value) => unchecked((short)(long)value);
		public static readonly Func<object, object> Int64ToUInt16_Unchecked = (object value) => unchecked((ushort)(long)value);
		public static readonly Func<object, object> Int64ToInt32_Unchecked = (object value) => unchecked((int)(long)value);
		public static readonly Func<object, object> Int64ToUInt32_Unchecked = (object value) => unchecked((uint)(long)value);
		public static readonly Func<object, object> Int64ToUInt64_Unchecked = (object value) => unchecked((ulong)(long)value);

		// Checked
		public static readonly Func<object, object> Int64ToChar_Checked = (object value) => checked((char)(long)value);
		public static readonly Func<object, object> Int64ToSByte_Checked = (object value) => checked((sbyte)(long)value);
		public static readonly Func<object, object> Int64ToByte_Checked = (object value) => checked((byte)(long)value);
		public static readonly Func<object, object> Int64ToInt16_Checked = (object value) => checked((short)(long)value);
		public static readonly Func<object, object> Int64ToUInt16_Checked = (object value) => checked((ushort)(long)value);
		public static readonly Func<object, object> Int64ToInt32_Checked = (object value) => checked((int)(long)value);
		public static readonly Func<object, object> Int64ToUInt32_Checked = (object value) => checked((uint)(long)value);
		public static readonly Func<object, object> Int64ToUInt64_Checked = (object value) => checked((ulong)(long)value);
		#endregion Int64

		#region UInt64
		// Checked (null)
		public static readonly Func<object, object> UInt64ToChar = (object value) => {
			ulong val = (ulong)value;
			return val > char.MaxValue ? (object)null : (char)val;
		};
		public static readonly Func<object, object> UInt64ToSByte = (object value) => {
			ulong val = (ulong)value;
			return val > (ulong)sbyte.MaxValue ? (object)null : (sbyte)val;
		};
		public static readonly Func<object, object> UInt64ToByte = (object value) => {
			ulong val = (ulong)value;
			return val > byte.MaxValue ? (object)null : (byte)val;
		};
		public static readonly Func<object, object> UInt64ToInt16 = (object value) => {
			ulong val = (ulong)value;
			return val > (ulong)short.MaxValue ? (object)null : (short)val;
		};
		public static readonly Func<object, object> UInt64ToUInt16 = (object value) => {
			ulong val = (ulong)value;
			return val > ushort.MaxValue ? (object)null : (ushort)val;
		};
		public static readonly Func<object, object> UInt64ToInt32 = (object value) => {
			ulong val = (ulong)value;
			return val > int.MaxValue ? (object)null : (int)val;
		};
		public static readonly Func<object, object> UInt64ToUInt32 = (object value) => {
			ulong val = (ulong)value;
			return val > uint.MaxValue ? (object)null : (uint)val;
		};
		public static readonly Func<object, object> UInt64ToInt64 = (object value) => {
			ulong val = (ulong)value;
			return val > long.MaxValue ? (object)null : (long)val;
		};

		// Default
		public static readonly Func<object, object> UInt64ToSingle = (object value) => (float)(ulong)value;
		public static readonly Func<object, object> UInt64ToDouble = (object value) => (double)(ulong)value;
		public static readonly Func<object, object> UInt64ToDecimal = (object value) => (decimal)(ulong)value;
		public static readonly Func<object, object> UInt64ToBigInteger = (object value) => (BigInteger)(ulong)value;

		// Unchecked
		public static readonly Func<object, object> UInt64ToChar_Unchecked = (object value) => unchecked((char)(ulong)value);
		public static readonly Func<object, object> UInt64ToSByte_Unchecked = (object value) => unchecked((sbyte)(ulong)value);
		public static readonly Func<object, object> UInt64ToByte_Unchecked = (object value) => unchecked((byte)(ulong)value);
		public static readonly Func<object, object> UInt64ToInt16_Unchecked = (object value) => unchecked((short)(ulong)value);
		public static readonly Func<object, object> UInt64ToUInt16_Unchecked = (object value) => unchecked((ushort)(ulong)value);
		public static readonly Func<object, object> UInt64ToInt32_Unchecked = (object value) => unchecked((int)(ulong)value);
		public static readonly Func<object, object> UInt64ToUInt32_Unchecked = (object value) => unchecked((uint)(ulong)value);
		public static readonly Func<object, object> UInt64ToInt64_Unchecked = (object value) => unchecked((long)(ulong)value);

		// Checked
		public static readonly Func<object, object> UInt64ToChar_Checked = (object value) => checked((char)(ulong)value);
		public static readonly Func<object, object> UInt64ToSByte_Checked = (object value) => checked((sbyte)(ulong)value);
		public static readonly Func<object, object> UInt64ToByte_Checked = (object value) => checked((byte)(ulong)value);
		public static readonly Func<object, object> UInt64ToInt16_Checked = (object value) => checked((short)(ulong)value);
		public static readonly Func<object, object> UInt64ToUInt16_Checked = (object value) => checked((ushort)(ulong)value);
		public static readonly Func<object, object> UInt64ToInt32_Checked = (object value) => checked((int)(ulong)value);
		public static readonly Func<object, object> UInt64ToUInt32_Checked = (object value) => checked((uint)(ulong)value);
		public static readonly Func<object, object> UInt64ToInt64_Checked = (object value) => checked((long)(ulong)value);
		#endregion UInt64

		#region Single
		private static int? _DoubleToInt32(double val, double min, double max)
		{
			if (val >= 0) {
				if (val <= max) {
					int result = (int)val;
					double dif = val - result;
					if (dif > 0.5 || (dif == 0.5 && (result & 1) != 0))
						result++;
					return result;
				}
			}
			else if (val >= min) {
				int result = (int)val;
				double dif = val - result;
				if (dif > 0.5 || (dif == 0.5 && (result & 1) != 0))
					result++;
				return result;
			}
			return null;
		}

		private static uint? _DoubleToUInt32(double val, double max)
		{
			if (val >= -0.5 && val < max) {
				uint result = (uint)val;
				double dif = val - result;
				if (dif > 0.5 || (dif == 0.5 && (result & 1) != 0))
					result++;
				return result;
			}
			return null;
		}

		private static long? _DoubleToInt64(double val, double min, double max)
		{
			if (val >= 0) {
				if (val < max) {
					long result = (long)val;
					double dif = val - result;
					if (dif > 0.5 || (dif == 0.5 && (result & 1) != 0))
						result++;
					return result;
				}
			}
			else if (val >= min) {
				int result = (int)val;
				double dif = val - result;
				if (dif > 0.5 || (dif == 0.5 && (result & 1) != 0))
					result++;
				return result;
			}
			return null;
		}

		private static ulong? _DoubleToUInt64(double val, double max)
		{
			if (val >= -0.5 && val < max) {
				ulong result = (ulong)val;
				double dif = val - result;
				if (dif > 0.5 || (dif == 0.5 && (result & 1) != 0))
					result++;
				return result;
			}
			return null;
		}

		// Checked (null)
		public static readonly Func<object, object> SingleToSByte = (object value) => (sbyte)(int)_DoubleToInt32((float)value, sbyte.MinValue - 0.5, sbyte.MaxValue + 0.5);
		public static readonly Func<object, object> SingleToByte = (object value) => (byte)(int)_DoubleToInt32((float)value, byte.MinValue - 0.5, byte.MaxValue + 0.5);
		public static readonly Func<object, object> SingleToInt16 = (object value) => (short)(int)_DoubleToInt32((float)value, short.MinValue - 0.5, short.MaxValue + 0.5);
		public static readonly Func<object, object> SingleToUInt16 = (object value) => (ushort)(int)_DoubleToInt32((float)value, ushort.MinValue - 0.5, ushort.MaxValue + 0.5);
		public static readonly Func<object, object> SingleToInt32 = (object value) => _DoubleToInt32((float)value, int.MinValue - 0.5, int.MaxValue + 0.5);
		public static readonly Func<object, object> SingleToUInt32 = (object value) => _DoubleToUInt32((float)value, uint.MaxValue + 0.5);
		public static readonly Func<object, object> SingleToInt64 = (object value) => _DoubleToInt64((float)value, long.MinValue - 0.5, long.MaxValue + 0.5);
		public static readonly Func<object, object> SingleToUInt64 = (object value) => _DoubleToUInt64((float)value, ulong.MaxValue + 0.5);
		public static readonly Func<object, object> SingleToDecimal = (object value) => {
			float val = (float)value;
			return val < (float)decimal.MinValue || val > (float)decimal.MaxValue ? (decimal?)null : ((decimal)val);
		};

		// Default
		public static readonly Func<object, object> SingleToDouble = (object value) => (double)(float)value;
		public static readonly Func<object, object> SingleToBigInteger = (object value) => (BigInteger)(float)value;

		// Unchecked
		public static readonly Func<object, object> SingleToChar_Unchecked = (object value) => unchecked((char)(float)value);
		public static readonly Func<object, object> SingleToSByte_Unchecked = (object value) => unchecked((sbyte)(float)value);
		public static readonly Func<object, object> SingleToByte_Unchecked = (object value) => unchecked((byte)(float)value);
		public static readonly Func<object, object> SingleToInt16_Unchecked = (object value) => unchecked((short)(float)value);
		public static readonly Func<object, object> SingleToUInt16_Unchecked = (object value) => unchecked((ushort)(float)value);
		public static readonly Func<object, object> SingleToInt32_Unchecked = (object value) => unchecked((int)(float)value);
		public static readonly Func<object, object> SingleToUInt32_Unchecked = (object value) => unchecked((uint)(float)value);
		public static readonly Func<object, object> SingleToInt64_Unchecked = (object value) => unchecked((long)(float)value);
		public static readonly Func<object, object> SingleToUInt64_Unchecked = (object value) => unchecked((ulong)(float)value);
		public static readonly Func<object, object> SingleToDecimal_Unchecked = (object value) => unchecked((decimal)(float)value);

		// Checked
		public static readonly Func<object, object> SingleToChar_Checked = (object value) => checked((char)(float)value);
		public static readonly Func<object, object> SingleToSByte_Checked = (object value) => checked((sbyte)(float)value);
		public static readonly Func<object, object> SingleToByte_Checked = (object value) => checked((byte)(float)value);
		public static readonly Func<object, object> SingleToInt16_Checked = (object value) => checked((short)(float)value);
		public static readonly Func<object, object> SingleToUInt16_Checked = (object value) => checked((ushort)(float)value);
		public static readonly Func<object, object> SingleToInt32_Checked = (object value) => checked((int)(float)value);
		public static readonly Func<object, object> SingleToUInt32_Checked = (object value) => checked((uint)(float)value);
		public static readonly Func<object, object> SingleToInt64_Checked = (object value) => checked((long)(float)value);
		public static readonly Func<object, object> SingleToUInt64_Checked = (object value) => checked((ulong)(float)value);
		public static readonly Func<object, object> SingleToDecimal_Checked = (object value) => checked((decimal)(float)value);
		#endregion Single

		#region Double
		// Checked (null)
		public static readonly Func<object, object> DoubleToSByte = (object value) => (sbyte)(int)_DoubleToInt32((double)value, sbyte.MinValue - 0.5, sbyte.MaxValue + 0.5);
		public static readonly Func<object, object> DoubleToByte = (object value) => (byte)(int)_DoubleToInt32((double)value, byte.MinValue - 0.5, byte.MaxValue + 0.5);
		public static readonly Func<object, object> DoubleToInt16 = (object value) => (short)(int)_DoubleToInt32((double)value, short.MinValue - 0.5, short.MaxValue + 0.5);
		public static readonly Func<object, object> DoubleToUInt16 = (object value) => (ushort)(int)_DoubleToInt32((double)value, ushort.MinValue - 0.5, ushort.MaxValue + 0.5);
		public static readonly Func<object, object> DoubleToInt32 = (object value) => _DoubleToInt32((double)value, int.MinValue - 0.5, int.MaxValue + 0.5);
		public static readonly Func<object, object> DoubleToUInt32 = (object value) => _DoubleToUInt32((double)value, uint.MaxValue + 0.5);
		public static readonly Func<object, object> DoubleToInt64 = (object value) => _DoubleToInt64((double)value, long.MinValue - 0.5, long.MaxValue + 0.5);
		public static readonly Func<object, object> DoubleToUInt64 = (object value) => _DoubleToUInt64((double)value, ulong.MaxValue + 0.5);
		public static readonly Func<object, object> DoubleToSingle = (object value) => {
			double val = (double)value;
			return val < float.MinValue || val > float.MaxValue ? (object)null : (float)val;
		};
		public static readonly Func<object, object> DoubleToDecimal = (object value) => {
			double val = (double)value;
			return val < (double)decimal.MinValue || val > (double)decimal.MaxValue ? (object)null : (decimal)val;
		};

		// Default
		public static readonly Func<object, object> DoubleToBigInteger = (object value) => (BigInteger)(double)value;

		// Unchecked
		public static readonly Func<object, object> DoubleToChar_Unchecked = (object value) => unchecked((char)(double)value);
		public static readonly Func<object, object> DoubleToSByte_Unchecked = (object value) => unchecked((sbyte)(double)value);
		public static readonly Func<object, object> DoubleToByte_Unchecked = (object value) => unchecked((byte)(double)value);
		public static readonly Func<object, object> DoubleToInt16_Unchecked = (object value) => unchecked((short)(double)value);
		public static readonly Func<object, object> DoubleToUInt16_Unchecked = (object value) => unchecked((ushort)(double)value);
		public static readonly Func<object, object> DoubleToInt32_Unchecked = (object value) => unchecked((int)(double)value);
		public static readonly Func<object, object> DoubleToUInt32_Unchecked = (object value) => unchecked((uint)(double)value);
		public static readonly Func<object, object> DoubleToInt64_Unchecked = (object value) => unchecked((long)(double)value);
		public static readonly Func<object, object> DoubleToUInt64_Unchecked = (object value) => unchecked((ulong)(double)value);
		public static readonly Func<object, object> DoubleToSingle_Unchecked = (object value) => unchecked((float)(double)value);
		public static readonly Func<object, object> DoubleToDecimal_Unchecked = (object value) => unchecked((decimal)(double)value);

		// Checked
		public static readonly Func<object, object> DoubleToChar_Checked = (object value) => checked((char)(double)value);
		public static readonly Func<object, object> DoubleToSByte_Checked = (object value) => checked((sbyte)(double)value);
		public static readonly Func<object, object> DoubleToByte_Checked = (object value) => checked((byte)(double)value);
		public static readonly Func<object, object> DoubleToInt16_Checked = (object value) => checked((short)(double)value);
		public static readonly Func<object, object> DoubleToUInt16_Checked = (object value) => checked((ushort)(double)value);
		public static readonly Func<object, object> DoubleToInt32_Checked = (object value) => checked((int)(double)value);
		public static readonly Func<object, object> DoubleToUInt32_Checked = (object value) => checked((uint)(double)value);
		public static readonly Func<object, object> DoubleToInt64_Checked = (object value) => checked((long)(double)value);
		public static readonly Func<object, object> DoubleToUInt64_Checked = (object value) => checked((ulong)(double)value);
		public static readonly Func<object, object> DoubleToSingle_Checked = (object value) => checked((float)(double)value);
		public static readonly Func<object, object> DoubleToDecimal_Checked = (object value) => checked((decimal)(double)value);
		#endregion Double

		#region Decimal
		// Checked (null)
		public static readonly Func<object, object> DecimalToSByte = (object value) => {
			decimal val = (decimal)value;
			return val < sbyte.MinValue || val > sbyte.MaxValue ? (object)null : (sbyte)val;
		};
		public static readonly Func<object, object> DecimalToByte = (object value) => {
			decimal val = (decimal)value;
			return val < 0 || val > byte.MaxValue ? (object)null : (byte)val;
		};
		public static readonly Func<object, object> DecimalToInt16 = (object value) => {
			decimal val = (decimal)value;
			return val < short.MinValue || val > short.MaxValue ? (object)null : (short)val;
		};
		public static readonly Func<object, object> DecimalToUInt16 = (object value) => {
			decimal val = (decimal)value;
			return val < 0 || val > ushort.MaxValue ? (object)null : (ushort)val;
		};
		public static readonly Func<object, object> DecimalToInt32 = (object value) => {
			decimal val = (decimal)value;
			return val < int.MinValue || val > int.MaxValue ? (object)null : (int)val;
		};
		public static readonly Func<object, object> DecimalToUInt32 = (object value) => {
			decimal val = (decimal)value;
			return val < 0 || val > uint.MaxValue ? (object)null : (uint)val;
		};
		public static readonly Func<object, object> DecimalToInt64 = (object value) => {
			decimal val = (decimal)value;
			return val < long.MinValue || val > long.MaxValue ? (object)null : (long)val;
		};
		public static readonly Func<object, object> DecimalToUInt64 = (object value) => {
			decimal val = (decimal)value;
			return val < 0 || val > ulong.MaxValue ? (object)null : (ulong)val;
		};

		// Default
		public static readonly Func<object, object> DecimalToSingle = (object value) => (float)(decimal)value;
		public static readonly Func<object, object> DecimalToDouble = (object value) => (double)(decimal)value;
		public static readonly Func<object, object> DecimalToBigInteger = (object value) => (BigInteger)(decimal)value;

		// Unchecked
		public static readonly Func<object, object> DecimalToChar_Unchecked = (object value) => unchecked((char)(decimal)value);
		public static readonly Func<object, object> DecimalToSByte_Unchecked = (object value) => unchecked((sbyte)(decimal)value);
		public static readonly Func<object, object> DecimalToByte_Unchecked = (object value) => unchecked((byte)(decimal)value);
		public static readonly Func<object, object> DecimalToInt16_Unchecked = (object value) => unchecked((short)(decimal)value);
		public static readonly Func<object, object> DecimalToUInt16_Unchecked = (object value) => unchecked((ushort)(decimal)value);
		public static readonly Func<object, object> DecimalToInt32_Unchecked = (object value) => unchecked((int)(decimal)value);
		public static readonly Func<object, object> DecimalToUInt32_Unchecked = (object value) => unchecked((uint)(decimal)value);
		public static readonly Func<object, object> DecimalToInt64_Unchecked = (object value) => unchecked((long)(decimal)value);
		public static readonly Func<object, object> DecimalToUInt64_Unchecked = (object value) => unchecked((ulong)(decimal)value);

		// Checked
		public static readonly Func<object, object> DecimalToChar_Checked = (object value) => checked((char)(decimal)value);
		public static readonly Func<object, object> DecimalToSByte_Checked = (object value) => checked((sbyte)(decimal)value);
		public static readonly Func<object, object> DecimalToByte_Checked = (object value) => checked((byte)(decimal)value);
		public static readonly Func<object, object> DecimalToInt16_Checked = (object value) => checked((short)(decimal)value);
		public static readonly Func<object, object> DecimalToUInt16_Checked = (object value) => checked((ushort)(decimal)value);
		public static readonly Func<object, object> DecimalToInt32_Checked = (object value) => checked((int)(decimal)value);
		public static readonly Func<object, object> DecimalToUInt32_Checked = (object value) => checked((uint)(decimal)value);
		public static readonly Func<object, object> DecimalToInt64_Checked = (object value) => checked((long)(decimal)value);
		public static readonly Func<object, object> DecimalToUInt64_Checked = (object value) => checked((ulong)(decimal)value);
		#endregion Decimal

		#region BigInteger
		public static readonly Func<object, object> BigIntegerToBoolean = (object value) => ((BigInteger)value) != 0;
		public static readonly Func<object, object> BigIntegerToChar = (object value) => (char)(BigInteger)value;
		public static readonly Func<object, object> BigIntegerToSByte = (object value) => (sbyte)(BigInteger)value;
		public static readonly Func<object, object> BigIntegerToByte = (object value) => (byte)(BigInteger)value;
		public static readonly Func<object, object> BigIntegerToInt16 = (object value) => (short)(BigInteger)value;
		public static readonly Func<object, object> BigIntegerToUInt16 = (object value) => (ushort)(BigInteger)value;
		public static readonly Func<object, object> BigIntegerToInt32 = (object value) => (int)(BigInteger)value;
		public static readonly Func<object, object> BigIntegerToUInt32 = (object value) => (uint)(BigInteger)value;
		public static readonly Func<object, object> BigIntegerToInt64 = (object value) => (long)(BigInteger)value;
		public static readonly Func<object, object> BigIntegerToUInt64 = (object value) => (ulong)(BigInteger)value;
		public static readonly Func<object, object> BigIntegerToSingle = (object value) => (float)(BigInteger)value;
		public static readonly Func<object, object> BigIntegerToDouble = (object value) => (double)(BigInteger)value;
		public static readonly Func<object, object> BigIntegerToDecimal = (object value) => (decimal)(BigInteger)value;
		#endregion BigInteger

		#region TryParse
		private static readonly MethodInfo TryParseEnumMethod;

		internal static readonly Func<object, object>[] TryParseConversions;

		/// <summary>
		/// Creates a function for parsing the given type from a string.
		/// </summary>
		/// <param name="type">The type to parse.</param>
		/// <param name="ignoreCase">Determines if case should be ignored when parsing enums.</param>
		/// <returns>A function for parsing the given type from a string.</returns>
		public static Func<object, object> TryParse(Type type, bool ignoreCase = false)
		{
			if (type.IsEnum)
				return TryParseEnum(type, ignoreCase);
			TypeCode typeCode = Type.GetTypeCode(type);
			if (typeCode != TypeCode.Object)
				return TryParseConversions[(int)typeCode];
			if (type == typeof(DateTimeOffset))
				return tryParseDateTimeOffset;
			if (type == typeof(TimeSpan))
				return tryParseTimeSpan;
			if (type == typeof(Guid))
				return TryParseGuid;
			if (type == typeof(BigInteger))
				return tryParseBigInteger;
			return null;
		}

		/// <summary>
		/// Gets the function for parsing the given <see cref="TypeCode"/> from a string. This
		/// will return <see langword="null"/> if the operation is not supported.
		/// </summary>
		/// <param name="typeCode">The <see cref="TypeCode"/> of the type to parse.</param>
		/// <returns>The function for parsing the given <see cref="TypeCode"/>.</returns>
		public static Func<object, object> TryParse(TypeCode typeCode)
		{
			return TryParseConversions[(int)typeCode];
		}

		// Enum

		private static readonly ConcurrentDictionary<Type, Func<object, object>> TryParseEnumCache = new ConcurrentDictionary<Type, Func<object, object>>();
		private static readonly ConcurrentDictionary<Type, Func<object, object>> TryParseEnumIgnoreCaseCache = new ConcurrentDictionary<Type, Func<object, object>>();

		/// <summary>
		/// Creates a method for parsing the specified enum type. If the parse fails then <see langword="null"/> will be returned.
		/// </summary>
		/// <param name="type">The <see cref="Type"/> to create.</param>
		/// <param name="ignoreCase">Determines if case should be ignored when parsing.</param>
		public static Func<object, object> TryParseEnum(Type type, bool ignoreCase = false)
		{
			if (!type.IsEnum)
				return null;
			Func<object, object> converter;
			if (ignoreCase) {
				if (TryParseEnumIgnoreCaseCache.TryGetValue(type, out converter))
					return converter;
			}
			else if (TryParseEnumCache.TryGetValue(type, out converter))
				return converter;
			ParameterExpression x = Expression.Parameter(typeof(object), "x");
			ParameterExpression value = Expression.Variable(type, "value");
			MethodInfo mi = TryParseEnumMethod.MakeGenericMethod(type);
			MethodCallExpression call = Expression.Call(mi, Expression.Convert(x, typeof(string)), Expression.Constant(ignoreCase, typeof(bool)), value);
			BlockExpression body = Expression.Block(new[] { value }, Expression.Condition(call, Expression.Convert(value, typeof(object)), Expression.Constant(null)));
			converter = Expression.Lambda<Func<object, object>>(body, x).Compile();
			if (ignoreCase)
				TryParseEnumIgnoreCaseCache[type] = converter;
			else
				TryParseEnumCache[type] = converter;
			return converter;
		}

		// Char
		public static readonly Func<object, object> TryParseChar = (object str) => {
			return char.TryParse((string)str, out char value)
				? (char?)value
				: (char?)null;
		};

		// SByte
		public static readonly Func<object, object> tryParseSByte = TryParseSByte();
		public static Func<object, object> TryParseSByte(NumberStyles style = NumberStyles.Integer, IFormatProvider provider = null)
		{
			provider = NumberFormatInfo.GetInstance(provider);
			return (str) => sbyte.TryParse((string)str, style, provider, out sbyte value) ? value : (sbyte?)null;
		}

		// Byte
		public static readonly Func<object, object> tryParseByte = TryParseByte();
		public static Func<object, object> TryParseByte(NumberStyles style = NumberStyles.Integer, IFormatProvider provider = null)
		{
			provider = NumberFormatInfo.GetInstance(provider);
			return (str) => byte.TryParse((string)str, style, provider, out byte value) ? value : (byte?)null;
		}

		// Int16
		public static readonly Func<object, object> tryParseInt16 = TryParseInt16();
		public static Func<object, object> TryParseInt16(NumberStyles style = NumberStyles.Integer, IFormatProvider provider = null)
		{
			provider = NumberFormatInfo.GetInstance(provider);
			return (str) => short.TryParse((string)str, style, provider, out short value) ? value : (short?)null;
		}

		// UInt16
		public static readonly Func<object, object> tryParseUInt16 = TryParseUInt16();
		public static Func<object, object> TryParseUInt16(NumberStyles style = NumberStyles.Integer, IFormatProvider provider = null)
		{
			provider = NumberFormatInfo.GetInstance(provider);
			return (str) => ushort.TryParse((string)str, style, provider, out ushort value) ? value : (ushort?)null;
		}

		// Int32
		public static readonly Func<object, object> tryParseInt32 = TryParseInt32();
		public static Func<object, object> TryParseInt32(NumberStyles style = NumberStyles.Integer, IFormatProvider provider = null)
		{
			provider = NumberFormatInfo.GetInstance(provider);
			return (str) => int.TryParse((string)str, style, provider, out int value) ? value : (int?)null;
		}

		// UInt32
		public static readonly Func<object, object> tryParseUInt32 = TryParseUInt32();
		public static Func<object, object> TryParseUInt32(NumberStyles style = NumberStyles.Integer, IFormatProvider provider = null)
		{
			provider = NumberFormatInfo.GetInstance(provider);
			return (str) => uint.TryParse((string)str, style, provider, out uint value) ? value : (uint?)null;
		}

		// Int64
		public static readonly Func<object, object> tryParseInt64 = TryParseInt64();
		public static Func<object, object> TryParseInt64(NumberStyles style = NumberStyles.Integer, IFormatProvider provider = null)
		{
			provider = NumberFormatInfo.GetInstance(provider);
			return (str) => long.TryParse((string)str, style, provider, out long value) ? value : (long?)null;
		}

		// UInt64
		public static readonly Func<object, object> tryParseUInt64 = TryParseUInt64();
		public static Func<object, object> TryParseUInt64(NumberStyles style = NumberStyles.Integer, IFormatProvider provider = null)
		{
			provider = NumberFormatInfo.GetInstance(provider);
			return (str) => ulong.TryParse((string)str, style, provider, out ulong value) ? value : (ulong?)null;
		}

		// DateTime
		public static readonly Func<object, object> tryParseDateTime = TryParseDateTime();
		public static Func<object, object> TryParseDateTime(DateTimeStyles style = DateTimeStyles.None, IFormatProvider provider = null)
		{
			provider = DateTimeFormatInfo.GetInstance(provider);
			return (str) => DateTime.TryParse((string)str, provider, style, out DateTime value) ? value : (DateTime?)null;
		}

		// DateTimeOffset
		public static readonly Func<object, object> tryParseDateTimeOffset = TryParseDateTimeOffset();
		public static Func<object, object> TryParseDateTimeOffset(DateTimeStyles style = DateTimeStyles.None, IFormatProvider provider = null)
		{
			provider = DateTimeFormatInfo.GetInstance(provider);
			return (str) => DateTimeOffset.TryParse((string)str, provider, style, out DateTimeOffset value) ? value : (DateTimeOffset?)null;
		}

		// TimeSpan
		public static readonly Func<object, object> tryParseTimeSpan = TryParseTimeSpan();
		public static Func<object, object> TryParseTimeSpan(IFormatProvider provider = null)
		{
			provider = DateTimeFormatInfo.GetInstance(provider);
			return (str) => TimeSpan.TryParse((string)str, provider, out TimeSpan value) ? value : (TimeSpan?)null;
		}

		// Guid
		public static readonly Func<object, object> TryParseGuid = (object str) => Guid.TryParse((string)str, out Guid value) ? value : (Guid?)null;

		// BigInteger
		public static readonly Func<object, object> tryParseBigInteger = TryParseBigInteger();
		public static Func<object, object> TryParseBigInteger(NumberStyles style = NumberStyles.Integer, IFormatProvider provider = null)
		{
			provider = NumberFormatInfo.GetInstance(provider);
			return (str) => BigInteger.TryParse((string)str, style, provider, out BigInteger value) ? value : (BigInteger?)null;
		}

		// Boolean
		public static readonly Func<object, object> TryParseBoolean = (object str) => bool.TryParse((string)str, out bool value) ? value : (bool?)null;

		// Single
		public static readonly Func<object, object> tryParseSingle = TryParseSingle();
		public static Func<object, object> TryParseSingle(NumberStyles style = NumberStyles.Float | NumberStyles.AllowThousands, IFormatProvider provider = null)
		{
			provider = NumberFormatInfo.GetInstance(provider);
			return (str) => float.TryParse((string)str, style, provider, out float value) ? value : (float?)null;
		}

		// Double
		public static readonly Func<object, object> tryParseDouble = TryParseDouble();
		public static Func<object, object> TryParseDouble(NumberStyles style = NumberStyles.Float | NumberStyles.AllowThousands, IFormatProvider provider = null)
		{
			provider = NumberFormatInfo.GetInstance(provider);
			return (str) => double.TryParse((string)str, style, provider, out double value) ? value : (double?)null;
		}

		// Decimal
		public static readonly Func<object, object> tryParseDecimal = TryParseDecimal();
		public static Func<object, object> TryParseDecimal(NumberStyles style = NumberStyles.Number, IFormatProvider provider = null)
		{
			provider = NumberFormatInfo.GetInstance(provider);
			return (str) => decimal.TryParse((string)str, style, provider, out decimal value) ? value : (decimal?)null;
		}
		#endregion TryParse

		#region Parse

		/// <summary>
		/// Creates a method for parsing the specified type. If the parse fails then an exception will be thrown.
		/// </summary>
		/// <param name="type">The <see cref="Type"/> to create.</param>
		/// <param name="ignoreCase">Determines if case should be ignored when parsing enums.</param>
		public static Func<object, object> Parse(Type type, bool ignoreCase)
		{
			if (type.IsEnum)
				return ParseEnum(type, ignoreCase);
			TypeCode typeCode = Type.GetTypeCode(type);
			if (typeCode != TypeCode.Object)
				return ParseConversions[(int)typeCode];
			if (type == typeof(DateTimeOffset))
				return parseDateTimeOffset;
			if (type == typeof(TimeSpan))
				return parseTimeSpan;
			if (type == typeof(Guid))
				return ParseGuid;
			if (type == typeof(BigInteger))
				return parseBigInteger;
			return null;
		}

		/// <summary>
		/// Gets the function for parsing the given <see cref="TypeCode"/> from a string. This
		/// will return <see langword="null"/> if the operation is not supported.
		/// </summary>
		/// <param name="typeCode">The <see cref="TypeCode"/> of the type to parse.</param>
		/// <returns>The function for parsing the given <see cref="TypeCode"/>.</returns>
		public static Func<object, object> Parse(TypeCode typeCode)
		{
			return ParseConversions[(int)typeCode];
		}

		internal static readonly Func<object, object>[] ParseConversions;

		// Enum
		private static readonly ConcurrentDictionary<Type, Func<object, object>> ParseEnumCache = new ConcurrentDictionary<Type, Func<object, object>>();
		private static readonly ConcurrentDictionary<Type, Func<object, object>> ParseEnumIgnoreCaseCache = new ConcurrentDictionary<Type, Func<object, object>>();

		/// <summary>
		/// Creates a method for parsing the specified enum type. If the parse fails then an exception will be thrown.
		/// </summary>
		/// <param name="type">The <see cref="Type"/> to create.</param>
		/// <param name="ignoreCase">Determines if case should be ignored when parsing.</param>
		public static Func<object, object> ParseEnum(Type type, bool ignoreCase = false)
		{
			if (!type.IsEnum)
				return null;
			Func<object, object> converter;
			if (ignoreCase) {
				if (ParseEnumIgnoreCaseCache.TryGetValue(type, out converter))
					return converter;
			}
			else if (ParseEnumCache.TryGetValue(type, out converter))
				return converter;
			converter = (str) => Enum.Parse(type, (string)str, ignoreCase);
			if (ignoreCase)
				ParseEnumIgnoreCaseCache[type] = converter;
			else
				ParseEnumCache[type] = converter;
			return converter;
		}

		// Char
		public static readonly Func<object, object> ParseChar = (object str) => char.Parse((string)str);

		// SByte
		public static readonly Func<object, object> parseSByte = ParseSByte();
		public static Func<object, object> ParseSByte(NumberStyles style = NumberStyles.Integer, IFormatProvider provider = null)
		{
			provider = NumberFormatInfo.GetInstance(provider);
			return (str) => sbyte.Parse((string)str, style, provider);
		}

		// Byte
		public static readonly Func<object, object> parseByte = ParseByte();
		public static Func<object, object> ParseByte(NumberStyles style = NumberStyles.Integer, IFormatProvider provider = null)
		{
			provider = NumberFormatInfo.GetInstance(provider);
			return (str) => byte.Parse((string)str, style, provider);
		}

		// Int16
		public static readonly Func<object, object> parseInt16 = ParseInt16();
		public static Func<object, object> ParseInt16(NumberStyles style = NumberStyles.Integer, IFormatProvider provider = null)
		{
			provider = NumberFormatInfo.GetInstance(provider);
			return (str) => short.Parse((string)str, style, provider);
		}

		// UInt16
		public static readonly Func<object, object> parseUInt16 = ParseUInt16();
		public static Func<object, object> ParseUInt16(NumberStyles style = NumberStyles.Integer, IFormatProvider provider = null)
		{
			provider = NumberFormatInfo.GetInstance(provider);
			return (str) => ushort.Parse((string)str, style, provider);
		}

		// Int32
		public static readonly Func<object, object> parseInt32 = ParseInt32();
		public static Func<object, object> ParseInt32(NumberStyles style = NumberStyles.Integer, IFormatProvider provider = null)
		{
			provider = NumberFormatInfo.GetInstance(provider);
			return (str) => int.Parse((string)str, style, provider);
		}

		// UInt32
		public static readonly Func<object, object> parseUInt32 = ParseUInt32();
		public static Func<object, object> ParseUInt32(NumberStyles style = NumberStyles.Integer, IFormatProvider provider = null)
		{
			provider = NumberFormatInfo.GetInstance(provider);
			return (str) => uint.Parse((string)str, style, provider);
		}

		// Int64
		public static readonly Func<object, object> parseInt64 = ParseInt64();
		public static Func<object, object> ParseInt64(NumberStyles style = NumberStyles.Integer, IFormatProvider provider = null)
		{
			provider = NumberFormatInfo.GetInstance(provider);
			return (str) => long.Parse((string)str, style, provider);
		}

		// UInt64
		public static readonly Func<object, object> parseUInt64 = ParseUInt64();
		public static Func<object, object> ParseUInt64(NumberStyles style = NumberStyles.Integer, IFormatProvider provider = null)
		{
			provider = NumberFormatInfo.GetInstance(provider);
			return (str) => ulong.Parse((string)str, style, provider);
		}

		// DateTime
		public static readonly Func<object, object> parseDateTime = ParseDateTime();
		public static Func<object, object> ParseDateTime(DateTimeStyles style = DateTimeStyles.None, IFormatProvider provider = null)
		{
			provider = DateTimeFormatInfo.GetInstance(provider);
			return (str) => DateTime.Parse((string)str, provider, style);
		}

		// DateTimeOffset
		public static readonly Func<object, object> parseDateTimeOffset = ParseDateTimeOffset();
		public static Func<object, object> ParseDateTimeOffset(DateTimeStyles style = DateTimeStyles.None, IFormatProvider provider = null)
		{
			provider = DateTimeFormatInfo.GetInstance(provider);
			return (str) => DateTimeOffset.Parse((string)str, provider, style);
		}

		// TimeSpan
		public static readonly Func<object, object> parseTimeSpan = ParseTimeSpan();
		public static Func<object, object> ParseTimeSpan(IFormatProvider provider = null)
		{
			provider = DateTimeFormatInfo.GetInstance(provider);
			return (str) => TimeSpan.Parse((string)str, provider);
		}

		// Guid
		public static readonly Func<object, object> ParseGuid = (object str) => Guid.Parse((string)str);

		// BigInteger
		public static readonly Func<object, object> parseBigInteger = ParseBigInteger();
		public static Func<object, object> ParseBigInteger(NumberStyles style = NumberStyles.Integer, IFormatProvider provider = null)
		{
			provider = NumberFormatInfo.GetInstance(provider);
			return (str) => BigInteger.Parse((string)str, style, provider);
		}

		// Boolean
		public static readonly Func<object, object> ParseBoolean = (object str) => bool.Parse((string)str);

		// Single
		public static readonly Func<object, object> parseSingle = ParseSingle();
		public static Func<object, object> ParseSingle(NumberStyles style = NumberStyles.Float | NumberStyles.AllowThousands, IFormatProvider provider = null)
		{
			provider = NumberFormatInfo.GetInstance(provider);
			return (str) => float.Parse((string)str, style, provider);
		}

		// Double
		public static readonly Func<object, object> parseDouble = ParseDouble();
		public static Func<object, object> ParseDouble(NumberStyles style = NumberStyles.Float | NumberStyles.AllowThousands, IFormatProvider provider = null)
		{
			provider = NumberFormatInfo.GetInstance(provider);
			return (str) => double.Parse((string)str, style, provider);
		}

		// Decimal
		public static readonly Func<object, object> parseDecimal = ParseDecimal();
		public static Func<object, object> ParseDecimal(NumberStyles style = NumberStyles.Number, IFormatProvider provider = null)
		{
			provider = NumberFormatInfo.GetInstance(provider);
			return (str) => decimal.Parse((string)str, style, provider);
		}
		#endregion Parse

		#region Unchecked
		internal static readonly Func<object, object>[][] UncheckedConversions;

		private static readonly Func<object, object>[] boolCasts = new Func<object, object>[19] {
			null, // 0 = Empty 
			null, // 1 = Object
			null, // 2 = DBNull
			None, // 3 = Boolean
			null, // 4 = Char
			null, // 5 = SByte
			null, // 6 = Byte
			null, // 7 = Int16
			null, // 8 = UInt16
			null, // 9 = Int32
			null, // 10 = UInt32
			null, // 11 = Int64
			null, // 12 = UInt64
			null, // 13 = Single
			null, // 14 = Double
			null, // 15 = Decimal
			null, // 16 = DateTime
			null, // 17 = ??
			null, // 18 = String
		};

		private static readonly Func<object, object>[] charUnchecked = new Func<object, object>[19] {
			null, // 0 = Empty 
			null, // 1 = Object
			null, // 2 = DBNull
			null, // 3 = Boolean
			None, // 4 = Char
			SByteToChar_Unchecked, // 5 = SByte
			ByteToChar, // 6 = Byte
			Int16ToChar_Unchecked, // 7 = Int16
			UInt16ToChar, // 8 = UInt16
			Int32ToChar_Unchecked, // 9 = Int32
			UInt32ToChar_Unchecked, // 10 = UInt32
			Int64ToChar_Unchecked, // 11 = Int64
			UInt64ToChar_Unchecked, // 12 = UInt64
			SingleToChar_Unchecked, // 13 = Single
			DoubleToChar_Unchecked, // 14 = Double
			DecimalToChar_Unchecked, // 15 = Decimal
			null, // 16 = DateTime
			null, // 17 = ??
			null, // 18 = String
		};
		private static readonly Func<object, object>[] sbyteUnchecked = new Func<object, object>[19] {
			null, // 0 = Empty 
			null, // 1 = Object
			null, // 2 = DBNull
			BooleanToSByte, // 3 = Boolean
			CharToSByte_Unchecked, // 4 = Char
			None, // 5 = SByte
			ByteToSByte_Unchecked, // 6 = Byte
			Int16ToSByte_Unchecked, // 7 = Int16
			UInt16ToSByte_Unchecked, // 8 = UInt16
			Int32ToSByte_Unchecked, // 9 = Int32
			UInt32ToSByte_Unchecked, // 10 = UInt32
			Int64ToSByte_Unchecked, // 11 = Int64
			UInt64ToSByte_Unchecked, // 12 = UInt64
			SingleToSByte_Unchecked, // 13 = Single
			DoubleToSByte_Unchecked, // 14 = Double
			DecimalToSByte_Unchecked, // 15 = Decimal
			null, // 16 = DateTime
			null, // 17 = ??
			null, // 18 = String
		};
		private static readonly Func<object, object>[] byteUnchecked = new Func<object, object>[19] {
			null, // 0 = Empty 
			null, // 1 = Object
			null, // 2 = DBNull
			BooleanToByte, // 3 = Boolean
			CharToByte_Unchecked, // 4 = Char
			SByteToByte_Unchecked, // 5 = SByte
			None, // 6 = Byte
			Int16ToByte_Unchecked, // 7 = Int16
			UInt16ToByte_Unchecked, // 8 = UInt16
			Int32ToByte_Unchecked, // 9 = Int32
			UInt32ToByte_Unchecked, // 10 = UInt32
			Int64ToByte_Unchecked, // 11 = Int64
			UInt64ToByte_Unchecked, // 12 = UInt64
			SingleToByte_Unchecked, // 13 = Single
			DoubleToByte_Unchecked, // 14 = Double
			DecimalToByte_Unchecked, // 15 = Decimal
			null, // 16 = DateTime
			null, // 17 = ??
			null, // 18 = String
		};
		private static readonly Func<object, object>[] int16Unchecked = new Func<object, object>[19] {
			null, // 0 = Empty 
			null, // 1 = Object
			null, // 2 = DBNull
			BooleanToInt16, // 3 = Boolean
			CharToInt16_Unchecked, // 4 = Char
			SByteToInt16, // 5 = SByte
			ByteToInt16, // 6 = Byte
			None, // 7 = Int16
			UInt16ToInt16_Unchecked, // 8 = UInt16
			Int32ToInt16_Unchecked, // 9 = Int32
			UInt32ToInt16_Unchecked, // 10 = UInt32
			Int64ToInt16_Unchecked, // 11 = Int64
			UInt64ToInt16_Unchecked, // 12 = UInt64
			SingleToInt16_Unchecked, // 13 = Single
			DoubleToInt16_Unchecked, // 14 = Double
			DecimalToInt16_Unchecked, // 15 = Decimal
			null, // 16 = DateTime
			null, // 17 = ??
			null, // 18 = String
		};
		private static readonly Func<object, object>[] uint16Unchecked = new Func<object, object>[19] {
			null, // 0 = Empty 
			null, // 1 = Object
			null, // 2 = DBNull
			BooleanToUInt16, // 3 = Boolean
			CharToUInt16, // 4 = Char
			SByteToUInt16_Unchecked, // 5 = SByte
			ByteToUInt16, // 6 = Byte
			Int16ToUInt16_Unchecked, // 7 = Int16
			None, // 8 = UInt16
			Int32ToUInt16_Unchecked, // 9 = Int32
			UInt32ToUInt16_Unchecked, // 10 = UInt32
			Int64ToUInt16_Unchecked, // 11 = Int64
			UInt64ToUInt16_Unchecked, // 12 = UInt64
			SingleToUInt16_Unchecked, // 13 = Single
			DoubleToUInt16_Unchecked, // 14 = Double
			DecimalToUInt16_Unchecked, // 15 = Decimal
			null, // 16 = DateTime
			null, // 17 = ??
			null, // 18 = String
		};
		private static readonly Func<object, object>[] int32Unchecked = new Func<object, object>[19] {
			null, // 0 = Empty 
			null, // 1 = Object
			null, // 2 = DBNull
			BooleanToInt32, // 3 = Boolean
			CharToInt32, // 4 = Char
			SByteToInt32, // 5 = SByte
			ByteToInt32, // 6 = Byte
			Int16ToInt32, // 7 = Int16
			UInt16ToInt32, // 8 = UInt16
			None, // 9 = Int32
			UInt32ToInt32_Unchecked, // 10 = UInt32
			Int64ToInt32_Unchecked, // 11 = Int64
			UInt64ToInt32_Unchecked, // 12 = UInt64
			SingleToInt32_Unchecked, // 13 = Single
			DoubleToInt32_Unchecked, // 14 = Double
			DecimalToInt32_Unchecked, // 15 = Decimal
			null, // 16 = DateTime
			null, // 17 = ??
			null, // 18 = String
		};
		private static readonly Func<object, object>[] uint32Unchecked = new Func<object, object>[19] {
			null, // 0 = Empty 
			null, // 1 = Object
			null, // 2 = DBNull
			BooleanToUInt32, // 3 = Boolean
			CharToUInt32, // 4 = Char
			SByteToUInt32_Unchecked, // 5 = SByte
			ByteToUInt32, // 6 = Byte
			Int16ToUInt32_Unchecked, // 7 = Int16
			UInt16ToUInt32, // 8 = UInt16
			Int32ToUInt32_Unchecked, // 9 = Int32
			None, // 10 = UInt32
			Int64ToUInt32_Unchecked, // 11 = Int64
			UInt64ToUInt32_Unchecked, // 12 = UInt64
			SingleToUInt32_Unchecked, // 13 = Single
			DoubleToUInt32_Unchecked, // 14 = Double
			DecimalToUInt32_Unchecked, // 15 = Decimal
			null, // 16 = DateTime
			null, // 17 = ??
			null, // 18 = String
		};
		private static readonly Func<object, object>[] int64Unchecked = new Func<object, object>[19] {
			null, // 0 = Empty 
			null, // 1 = Object
			null, // 2 = DBNull
			BooleanToInt64, // 3 = Boolean
			CharToInt64, // 4 = Char
			SByteToInt64, // 5 = SByte
			ByteToInt64, // 6 = Byte
			Int16ToInt64, // 7 = Int16
			UInt16ToInt64, // 8 = UInt16
			Int32ToInt64, // 9 = Int32
			UInt32ToInt64, // 10 = UInt32
			None, // 11 = Int64
			UInt64ToInt64_Unchecked, // 12 = UInt64
			SingleToInt64_Unchecked, // 13 = Single
			DoubleToInt64_Unchecked, // 14 = Double
			DecimalToInt64_Unchecked, // 15 = Decimal
			null, // 16 = DateTime
			null, // 17 = ??
			null, // 18 = String
		};
		private static readonly Func<object, object>[] uint64Unchecked = new Func<object, object>[19] {
			null, // 0 = Empty 
			null, // 1 = Object
			null, // 2 = DBNull
			BooleanToUInt64, // 3 = Boolean
			CharToUInt64, // 4 = Char
			SByteToUInt64_Unchecked, // 5 = SByte
			ByteToUInt64, // 6 = Byte
			Int16ToUInt64_Unchecked, // 7 = Int16
			UInt16ToUInt64, // 8 = UInt16
			Int32ToUInt64_Unchecked, // 9 = Int32
			UInt32ToUInt64, // 10 = UInt32
			Int64ToUInt64_Unchecked, // 11 = Int64
			None, // 12 = UInt64
			SingleToUInt64_Unchecked, // 13 = Single
			DoubleToUInt64_Unchecked, // 14 = Double
			DecimalToUInt64_Unchecked, // 15 = Decimal
			null, // 16 = DateTime
			null, // 17 = ??
			null, // 18 = String
		};
		private static readonly Func<object, object>[] singleUnchecked = new Func<object, object>[19] {
			null, // 0 = Empty 
			null, // 1 = Object
			null, // 2 = DBNull
			BooleanToSingle, // 3 = Boolean
			CharToSingle, // 4 = Char
			SByteToSingle, // 5 = SByte
			ByteToSingle, // 6 = Byte
			Int16ToSingle, // 7 = Int16
			UInt16ToSingle, // 8 = UInt16
			Int32ToSingle, // 9 = Int32
			UInt32ToSingle, // 10 = UInt32
			Int64ToSingle, // 11 = Int64
			UInt64ToSingle, // 12 = UInt64
			None, // 13 = Single
			DoubleToSingle_Unchecked, // 14 = Double
			DecimalToSingle, // 15 = Decimal
			null, // 16 = DateTime
			null, // 17 = ??
			null, // 18 = String
		};
		private static readonly Func<object, object>[] doubleCasts = new Func<object, object>[19] {
			null, // 0 = Empty 
			null, // 1 = Object
			null, // 2 = DBNull
			BooleanToDouble, // 3 = Boolean
			CharToDouble, // 4 = Char
			SByteToDouble, // 5 = SByte
			ByteToDouble, // 6 = Byte
			Int16ToDouble, // 7 = Int16
			UInt16ToDouble, // 8 = UInt16
			Int32ToDouble, // 9 = Int32
			UInt32ToDouble, // 10 = UInt32
			Int64ToDouble, // 11 = Int64
			UInt64ToDouble, // 12 = UInt64
			SingleToDouble, // 13 = Single
			None, // 14 = Double
			DecimalToDouble, // 15 = Decimal
			null, // 16 = DateTime
			null, // 17 = ??
			null, // 18 = String
		};
		private static readonly Func<object, object>[] decimalUnchecked = new Func<object, object>[19] {
			null, // 0 = Empty 
			null, // 1 = Object
			null, // 2 = DBNull
			BooleanToDecimal, // 3 = Boolean
			CharToDecimal, // 4 = Char
			SByteToDecimal, // 5 = SByte
			ByteToDecimal, // 6 = Byte
			Int16ToDecimal, // 7 = Int16
			UInt16ToDecimal, // 8 = UInt16
			Int32ToDecimal, // 9 = Int32
			UInt32ToDecimal, // 10 = UInt32
			Int64ToDecimal, // 11 = Int64
			UInt64ToDecimal, // 12 = UInt64
			SingleToDecimal_Unchecked, // 13 = Single
			DoubleToDecimal_Unchecked, // 14 = Double
			None, // 15 = Decimal
			null, // 16 = DateTime
			null, // 17 = ??
			null, // 18 = String
		};
		#endregion Unchecked

		#region Checked
		private static readonly Func<object, object>[] invalidCasts = new Func<object, object>[19];

		internal static readonly Func<object, object>[][] CheckedConversions;

		private static readonly Func<object, object>[] charChecked = new Func<object, object>[19] {
			null, // 0 = Empty 
			null, // 1 = Object
			null, // 2 = DBNull
			null, // 3 = Boolean
			None, // 4 = Char
			SByteToChar_Checked, // 5 = SByte
			ByteToChar, // 6 = Byte
			Int16ToChar_Checked, // 7 = Int16
			UInt16ToChar, // 8 = UInt16
			Int32ToChar_Checked, // 9 = Int32
			UInt32ToChar_Checked, // 10 = UInt32
			Int64ToChar_Checked, // 11 = Int64
			UInt64ToChar_Checked, // 12 = UInt64
			SingleToChar_Checked, // 13 = Single
			DoubleToChar_Checked, // 14 = Double
			DecimalToChar_Checked, // 15 = Decimal
			null, // 16 = DateTime
			null, // 17 = ??
			null, // 18 = String
		};
		private static readonly Func<object, object>[] sbyteChecked = new Func<object, object>[19] {
			null, // 0 = Empty 
			null, // 1 = Object
			null, // 2 = DBNull
			BooleanToSByte, // 3 = Boolean
			CharToSByte, // 4 = Char
			None, // 5 = SByte
			ByteToSByte_Checked, // 6 = Byte
			Int16ToSByte_Checked, // 7 = Int16
			UInt16ToSByte_Checked, // 8 = UInt16
			Int32ToSByte_Checked, // 9 = Int32
			UInt32ToSByte_Checked, // 10 = UInt32
			Int64ToSByte_Checked, // 11 = Int64
			UInt64ToSByte_Checked, // 12 = UInt64
			SingleToSByte_Checked, // 13 = Single
			DoubleToSByte_Checked, // 14 = Double
			DecimalToSByte_Checked, // 15 = Decimal
			null, // 16 = DateTime
			null, // 17 = ??
			null, // 18 = String
		};
		private static readonly Func<object, object>[] byteChecked = new Func<object, object>[19] {
			null, // 0 = Empty 
			null, // 1 = Object
			null, // 2 = DBNull
			BooleanToByte, // 3 = Boolean
			CharToByte, // 4 = Char
			SByteToByte_Checked, // 5 = SByte
			None, // 6 = Byte
			Int16ToByte_Checked, // 7 = Int16
			UInt16ToByte_Checked, // 8 = UInt16
			Int32ToByte_Checked, // 9 = Int32
			UInt32ToByte_Checked, // 10 = UInt32
			Int64ToByte_Checked, // 11 = Int64
			UInt64ToByte_Checked, // 12 = UInt64
			SingleToByte_Checked, // 13 = Single
			DoubleToByte_Checked, // 14 = Double
			DecimalToByte_Checked, // 15 = Decimal
			null, // 16 = DateTime
			null, // 17 = ??
			null, // 18 = String
		};
		private static readonly Func<object, object>[] int16Checked = new Func<object, object>[19] {
			null, // 0 = Empty 
			null, // 1 = Object
			null, // 2 = DBNull
			BooleanToInt16, // 3 = Boolean
			CharToInt16, // 4 = Char
			SByteToInt16, // 5 = SByte
			ByteToInt16, // 6 = Byte
			None, // 7 = Int16
			UInt16ToInt16_Checked, // 8 = UInt16
			Int32ToInt16_Checked, // 9 = Int32
			UInt32ToInt16_Checked, // 10 = UInt32
			Int64ToInt16_Checked, // 11 = Int64
			UInt64ToInt16_Checked, // 12 = UInt64
			SingleToInt16_Checked, // 13 = Single
			DoubleToInt16_Checked, // 14 = Double
			DecimalToInt16_Checked, // 15 = Decimal
			null, // 16 = DateTime
			null, // 17 = ??
			null, // 18 = String
		};
		private static readonly Func<object, object>[] uint16Checked = new Func<object, object>[19] {
			null, // 0 = Empty 
			null, // 1 = Object
			null, // 2 = DBNull
			BooleanToUInt16, // 3 = Boolean
			CharToUInt16, // 4 = Char
			SByteToUInt16_Checked, // 5 = SByte
			ByteToUInt16, // 6 = Byte
			Int16ToUInt16_Checked, // 7 = Int16
			None, // 8 = UInt16
			Int32ToUInt16_Checked, // 9 = Int32
			UInt32ToUInt16_Checked, // 10 = UInt32
			Int64ToUInt16_Checked, // 11 = Int64
			UInt64ToUInt16_Checked, // 12 = UInt64
			SingleToUInt16_Checked, // 13 = Single
			DoubleToUInt16_Checked, // 14 = Double
			DecimalToUInt16_Checked, // 15 = Decimal
			null, // 16 = DateTime
			null, // 17 = ??
			null, // 18 = String
		};
		private static readonly Func<object, object>[] int32Checked = new Func<object, object>[19] {
			null, // 0 = Empty 
			null, // 1 = Object
			null, // 2 = DBNull
			BooleanToInt32, // 3 = Boolean
			CharToInt32, // 4 = Char
			SByteToInt32, // 5 = SByte
			ByteToInt32, // 6 = Byte
			Int16ToInt32, // 7 = Int16
			UInt16ToInt32, // 8 = UInt16
			None, // 9 = Int32
			UInt32ToInt32_Checked, // 10 = UInt32
			Int64ToInt32_Checked, // 11 = Int64
			UInt64ToInt32_Checked, // 12 = UInt64
			SingleToInt32_Checked, // 13 = Single
			DoubleToInt32_Checked, // 14 = Double
			DecimalToInt32_Checked, // 15 = Decimal
			null, // 16 = DateTime
			null, // 17 = ??
			null, // 18 = String
		};
		private static readonly Func<object, object>[] uint32Checked = new Func<object, object>[19] {
			null, // 0 = Empty 
			null, // 1 = Object
			null, // 2 = DBNull
			BooleanToUInt32, // 3 = Boolean
			CharToUInt32, // 4 = Char
			SByteToUInt32_Checked, // 5 = SByte
			ByteToUInt32, // 6 = Byte
			Int16ToUInt32_Checked, // 7 = Int16
			UInt16ToUInt32, // 8 = UInt16
			Int32ToUInt32_Checked, // 9 = Int32
			None, // 10 = UInt32
			Int64ToUInt32_Checked, // 11 = Int64
			UInt64ToUInt32_Checked, // 12 = UInt64
			SingleToUInt32_Checked, // 13 = Single
			DoubleToUInt32_Checked, // 14 = Double
			DecimalToUInt32_Checked, // 15 = Decimal
			null, // 16 = DateTime
			null, // 17 = ??
			null, // 18 = String
		};
		private static readonly Func<object, object>[] int64Checked = new Func<object, object>[19] {
			null, // 0 = Empty 
			null, // 1 = Object
			null, // 2 = DBNull
			BooleanToInt64, // 3 = Boolean
			CharToInt64, // 4 = Char
			SByteToInt64, // 5 = SByte
			ByteToInt64, // 6 = Byte
			Int16ToInt64, // 7 = Int16
			UInt16ToInt64, // 8 = UInt16
			Int32ToInt64, // 9 = Int32
			UInt32ToInt64, // 10 = UInt32
			None, // 11 = Int64
			UInt64ToInt64_Checked, // 12 = UInt64
			SingleToInt64_Checked, // 13 = Single
			DoubleToInt64_Checked, // 14 = Double
			DecimalToInt64_Checked, // 15 = Decimal
			null, // 16 = DateTime
			null, // 17 = ??
			null, // 18 = String
		};
		private static readonly Func<object, object>[] uint64Checked = new Func<object, object>[19] {
			null, // 0 = Empty 
			null, // 1 = Object
			null, // 2 = DBNull
			BooleanToUInt64, // 3 = Boolean
			CharToUInt64, // 4 = Char
			SByteToUInt64_Checked, // 5 = SByte
			ByteToUInt64, // 6 = Byte
			Int16ToUInt64_Checked, // 7 = Int16
			UInt16ToUInt64, // 8 = UInt16
			Int32ToUInt64_Checked, // 9 = Int32
			UInt32ToUInt64, // 10 = UInt32
			Int64ToUInt64_Checked, // 11 = Int64
			None, // 12 = UInt64
			SingleToUInt64_Checked, // 13 = Single
			DoubleToUInt64_Checked, // 14 = Double
			DecimalToUInt64_Checked, // 15 = Decimal
			null, // 16 = DateTime
			null, // 17 = ??
			null, // 18 = String
		};
		private static readonly Func<object, object>[] singleChecked = new Func<object, object>[19] {
			null, // 0 = Empty 
			null, // 1 = Object
			null, // 2 = DBNull
			BooleanToSingle, // 3 = Boolean
			CharToSingle, // 4 = Char
			SByteToSingle, // 5 = SByte
			ByteToSingle, // 6 = Byte
			Int16ToSingle, // 7 = Int16
			UInt16ToSingle, // 8 = UInt16
			Int32ToSingle, // 9 = Int32
			UInt32ToSingle, // 10 = UInt32
			Int64ToSingle, // 11 = Int64
			UInt64ToSingle, // 12 = UInt64
			None, // 13 = Single
			DoubleToSingle_Checked, // 14 = Double
			DecimalToSingle, // 15 = Decimal
			null, // 16 = DateTime
			null, // 17 = ??
			null, // 18 = String
		};
		private static readonly Func<object, object>[] decimalChecked = new Func<object, object>[19] {
			null, // 0 = Empty 
			null, // 1 = Object
			null, // 2 = DBNull
			BooleanToDecimal, // 3 = Boolean
			CharToDecimal, // 4 = Char
			SByteToDecimal, // 5 = SByte
			ByteToDecimal, // 6 = Byte
			Int16ToDecimal, // 7 = Int16
			UInt16ToDecimal, // 8 = UInt16
			Int32ToDecimal, // 9 = Int32
			UInt32ToDecimal, // 10 = UInt32
			Int64ToDecimal, // 11 = Int64
			UInt64ToDecimal, // 12 = UInt64
			SingleToDecimal_Checked, // 13 = Single
			DoubleToDecimal_Checked, // 14 = Double
			None, // 15 = Decimal
			null, // 16 = DateTime
			null, // 17 = ??
			null, // 18 = String
		};
		private static readonly Func<object, object>[] datetimeCasts = new Func<object, object>[19] {
			null, // 0 = Empty 
			null, // 1 = Object
			null, // 2 = DBNull
			null, // 3 = Boolean
			null, // 4 = Char
			null, // 5 = SByte
			null, // 6 = Byte
			null, // 7 = Int16
			null, // 8 = UInt16
			null, // 9 = Int32
			null, // 10 = UInt32
			null, // 11 = Int64
			null, // 12 = UInt64
			null, // 13 = Single
			null, // 14 = Double
			null, // 15 = Decimal
			None, // 16 = DateTime
			null, // 17 = ??
			null, // 18 = String
		};
		private static readonly Func<object, object>[] stringCasts = new Func<object, object>[19] {
			null, // 0 = Empty 
			null, // 1 = Object
			null, // 2 = DBNull
			null, // 3 = Boolean
			null, // 4 = Char
			null, // 5 = SByte
			null, // 6 = Byte
			null, // 7 = Int16
			null, // 8 = UInt16
			null, // 9 = Int32
			null, // 10 = UInt32
			null, // 11 = Int64
			null, // 12 = UInt64
			null, // 13 = Single
			null, // 14 = Double
			null, // 15 = Decimal
			null, // 16 = DateTime
			null, // 17 = ??
			None, // 18 = String
		};
		#endregion Unchecked

		private static readonly ConcurrentDictionary<Tuple<Type, Type>, Func<object, object>> ImplicitCache = new ConcurrentDictionary<Tuple<Type, Type>, Func<object, object>>();
		private static readonly ConcurrentDictionary<Tuple<Type, Type>, Func<object, object>> ExplicitCache = new ConcurrentDictionary<Tuple<Type, Type>, Func<object, object>>();
		private static readonly ConcurrentDictionary<Tuple<Type, Type>, Func<object, object>> ConstructorCache = new ConcurrentDictionary<Tuple<Type, Type>, Func<object, object>>();

		#region Casts
		/// <summary>
		/// Creates a function that implicitly casts the input type to the output type.
		/// </summary>
		/// <param name="input"></param>
		/// <param name="output"></param>
		/// <returns></returns>
		public static Func<object, object> ImplicitCast(Type input, Type output)
		{
			Tuple<Type, Type> inout = Tuple.Create(input, output);
			if (!ImplicitCache.TryGetValue(inout, out Func<object, object> converter)) {
				MethodInfo mi = output.GetMethods(BindingFlags.Public | BindingFlags.Static).Concat(input.GetMethods(BindingFlags.Public | BindingFlags.Static))
					.FirstOrDefault(m => m.Name == "op_Implicit" && m.ReturnType == output && m.GetParameters()[0].ParameterType == input);
				if (mi != null) {
					ParameterExpression x = Expression.Parameter(typeof(object), "x");
					converter = Expression.Lambda<Func<object, object>>(Expression.Convert(Expression.Call(mi, Expression.Convert(x, input)), typeof(object)), x).Compile();
					ImplicitCache[inout] = converter;
				}
			}
			return converter;
		}

		/// <summary>
		/// Creates a function that explicitly casts the input type to the output type.
		/// </summary>
		/// <param name="input">The <see cref="Type"/> to be convert from.</param>
		/// <param name="output">The <see cref="Type"/> to convert to.</param>
		public static Func<object, object> ExplicitCast(Type input, Type output)
		{
			Tuple<Type, Type> inout = Tuple.Create(input, output);
			if (!ExplicitCache.TryGetValue(inout, out Func<object, object> converter)) {
				MethodInfo mi = output.GetMethods(BindingFlags.Public | BindingFlags.Static).Concat(input.GetMethods(BindingFlags.Public | BindingFlags.Static))
					.FirstOrDefault(m => m.Name == "op_Explicit" && m.ReturnType == output && m.GetParameters()[0].ParameterType == input);
				if (mi != null) {
					ParameterExpression x = Expression.Parameter(typeof(object), "x");
					converter = Expression.Lambda<Func<object, object>>(Expression.Convert(Expression.Call(mi, Expression.Convert(x, input)), typeof(object)), x).Compile();
					ExplicitCache[inout] = converter;
				}
			}
			return converter;
		}

		/// <summary>
		/// Creates a function that casts the input type to the output type. This will search for an implicit cast before an explicit cast.
		/// </summary>
		/// <param name="input">The <see cref="Type"/> to be convert from.</param>
		/// <param name="output">The <see cref="Type"/> to convert to.</param>
		/// <param name="overflowCheck">Determines if numeric overflow exceptions should be thrown.</param>
		public static Func<object, object> Cast(Type input, Type output, bool overflowCheck = false)
		{
			TypeCode inputTypeCode = Type.GetTypeCode(input);
			TypeCode outputTypeCode = Type.GetTypeCode(output);
			if (inputTypeCode <= TypeCode.Object || outputTypeCode <= TypeCode.Object) {
				return ImplicitCast(input, output) ?? ExplicitCast(input, output);
			}
			Func<object, object>[][] arr = overflowCheck ? CheckedConversions : UncheckedConversions;
			return arr[(int)outputTypeCode][(int)inputTypeCode];
		}
		#endregion Casts

		#region TypeConverter
		/// <summary>
		/// Creates a function for converting the input type to the output type using <see cref="System.ComponentModel.TypeConverter"/>.
		/// </summary>
		/// <param name="input">The <see cref="Type"/> to be convert from.</param>
		/// <param name="output">The <see cref="Type"/> to convert to.</param>
		public static Func<object, object> TypeConverter(Type input, Type output)
		{
			TypeConverter converter = TypeDescriptor.GetConverter(input);
			if (converter != null && converter.CanConvertTo(output)) {
				return (object value) => converter.ConvertTo(value, output);
			}
			return null;
		}
		#endregion

		/// <summary>
		/// Creates a function for creating the output type using the constructor that matches the input type.
		/// </summary>
		/// <param name="input">The type of the constructor's input parameter.</param>
		/// <param name="output">The type to be constructed.</param>
		/// <returns>A function for creating the output type using the constructor that matches the input type.</returns>
		public static Func<object, object> Constructor(Type input, Type output)
		{
			Tuple<Type, Type> inout = Tuple.Create(input, output);
			if (!ConstructorCache.TryGetValue(inout, out Func<object, object> converter)) {
				ConstructorInfo ci = output.GetConstructor(new Type[] { input });
				if (ci != null && ci.GetParameters()[0].ParameterType == input) {
					ParameterExpression x = Expression.Parameter(typeof(object));
					converter = Expression.Lambda<Func<object, object>>(Expression.New(ci, Expression.Convert(x, input)), x).Compile();
					ConstructorCache[inout] = converter;
				}
			}
			return converter;
		}

		public static readonly Func<object, object> DateTimeToDateTimeOffset = (object value) => (DateTimeOffset)(DateTime)value;
		public static readonly Func<object, object> DateTimeOffsetToDateTime = (object value) => ((DateTimeOffset)value).DateTime;

		/// <summary>
		/// Tries to parses a string as a <see langword="bool"/>. True values include "1", "True", and "Yes". False values include "0", "False", "No", null, and String.Empty.
		/// </summary>
		public static readonly Func<object, object> TryParseBooleanEx = (object value) => {
			string str = (string)value;
			if (str == null || str.Length == 0)
				return false;
			if (char.IsWhiteSpace(str[0]) || char.IsWhiteSpace(str[str.Length - 1]))
				str = str.Trim();
			if (str.Length > 0) {
				if (str == "0" || string.Equals(str, "False", StringComparison.OrdinalIgnoreCase) || string.Equals(str, "No", StringComparison.OrdinalIgnoreCase))
					return false;
				if (str == "1" || string.Equals(str, "True", StringComparison.OrdinalIgnoreCase) || string.Equals(str, "Yes", StringComparison.OrdinalIgnoreCase))
					return true;
			}
			return null;
		};

		/// <summary>
		/// Parses a string as a <see langword="bool"/>. True values include "1", "True", and "Yes". False values include "0", "False", "No", null, and String.Empty.
		/// </summary>
		public static readonly Func<object, object> ParseBooleanEx = (object value) => {
			object result = TryParseBooleanEx(value);
			return result ?? bool.Parse((string)value);
		};
	}
}
