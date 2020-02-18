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
	/// An alternative to <see cref="System.Convert"/> and <see cref="System.ComponentModel.TypeConverter"/>. 
	/// Creates functions that can convert any <see cref="object"/> to any <see cref="Type"/>.
	/// </summary>
	public class TypeConvert
	{
		/// <summary>
		/// The default <see cref="TypeConvert"/> that throws an exception for failed conversions.
		/// </summary>
		public static readonly TypeConvertDefault Default = new TypeConvertDefault(true, true);

		/// <summary>
		/// Converts an <see cref="object"/> to the specified <see cref="Type"/>.
		/// </summary>
		/// <param name="value">The <see cref="object"/> to convert.</param>
		/// <param name="output">The <see cref="Type"/> to convert to.</param>
		/// <returns>The result of the conversion or <see langword="null"/> on failure.</returns>
		public static object ChangeType(object value, Type output)
		{
			return Default.ChangeType(value, output);
		}

		/// <summary>
		/// Attempts to convert an <see cref="object"/> to the specified <see cref="Type"/> and catches all exceptions.
		/// </summary>
		/// <param name="value">The <see cref="object"/> to convert.</param>
		/// <param name="output">The <see cref="Type"/> to convert to.</param>
		/// <param name="result">The converted <see cref="object"/> or <see langword="null"/> on failure.</param>
		/// <returns>True if the <see cref="object"/> was converted successfully; false otherwise.</returns>
		public static bool TryChangeType(object value, Type output, out object result)
		{
			return Default.TryChangeType(value, output, out result);
		}

		/// <summary>
		/// Gets or sets the function that converts the input <see cref="Type"/> to the output <see cref="Type"/>. 
		/// <see cref="Conversions.AsNull"/> will be returned for invalid conversions.
		/// </summary>
		/// <param name="input">The <see cref="Type"/> to convert from.</param>
		/// <param name="output">The <see cref="Type"/> to convert to.</param>
		public static Func<object, object> GetConverter(Type input, Type output)
		{
			return Default[input, output];
		}

		/// <summary>
		/// Gets the function that converts any <see cref="object"/> to the specified <see cref="Type"/>.
		/// </summary>
		/// <param name="output">The <see cref="Type"/> to convert to.</param>
		public static Func<object, object> GetConverter(Type output)
		{
			return Default[output];
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
	}
}
