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

namespace Axion.Conversion
{
	/// <summary>
	/// TypeConvert which has been optimized for standard lookups.
	/// </summary>
	internal class TypeConvertEx : TypeConvert
	{
		internal protected TypeConvertEx(bool exceptionSafe = false, bool threadSafe = false) : base(exceptionSafe, threadSafe)
		{
		}

		public override object ToString(object value)
		{
			return value?.ToString();
		}

		protected override object ToDBNull(object value)
		{
			return value == DBNull.Value ? DBNull.Value : null;
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
					converter = IsExceptionSafe ? Conversions.TryParseEnum(output) : Conversions.ParseEnum(output);
					LookupCache[inout] = converter;
				}
				return converter;
			}
			return this[inputTypeCode, outputTypeCode];
		}
	}
}
