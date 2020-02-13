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
