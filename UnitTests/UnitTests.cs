using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Axion.Conversion;

namespace Axion
{
	[TestClass]
	public class UnitTests
	{
		public readonly Type[] ParseTypes = new Type[] {
			typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(int), typeof(uint),
			typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal), typeof(string), typeof(DateTime),
			 typeof(DateTimeOffset),  typeof(TimeSpan), typeof(Guid), typeof(BigInteger), typeof(DayOfWeek), typeof(DateTimeKind)
		};
		public readonly object[] ParseValues = new object[] {
			sbyte.MaxValue, byte.MaxValue, short.MaxValue, ushort.MaxValue, int.MaxValue, uint.MaxValue,
			long.MaxValue, ulong.MaxValue, float.MaxValue, double.MaxValue, decimal.MaxValue, "abc", DateTime.Now,
			DateTimeOffset.UtcNow, new TimeSpan(5, 3, 8), Guid.NewGuid(), new BigInteger(ulong.MaxValue) + 1, DayOfWeek.Wednesday, DateTimeKind.Local
		};
		public readonly Type[] BasicTypes = new Type[] {
			typeof(bool), typeof(char), typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(int), typeof(uint),
			typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal), typeof(DateTime), typeof(string),
		};
		public readonly Type[] NumericTypes = new Type[] {
			typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(int), typeof(uint),
			typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal), typeof(BigInteger)
		};
		public readonly object[] ZeroValues = new object[] {
			(sbyte)0, (byte)0, (short)0, (ushort)0, (int)0, (uint)0U,
			(long)0L, (ulong)0UL, (float)0.0f, (double)0.0, (decimal)0.0m, (BigInteger)0
		};
		public readonly object[] OneValues = new object[] {
			(sbyte)1, (byte)1, (short)1, (ushort)1, (int)1, (uint)1U,
			(long)1L, (ulong)1UL, (float)1.0f, (double)1.0, (decimal)1.0m, (BigInteger)1
		};
		public readonly object[] MinValues = new object[] {
			sbyte.MinValue, byte.MinValue, short.MinValue, ushort.MinValue, int.MinValue, uint.MinValue,
			long.MinValue, ulong.MinValue, float.MinValue, double.MinValue *0.98, decimal.MinValue,(BigInteger)float.MinValue
		};
		public readonly object[] MaxValues = new object[] {
			sbyte.MaxValue, byte.MaxValue, short.MaxValue, ushort.MaxValue, int.MaxValue, uint.MaxValue,
			long.MaxValue, ulong.MaxValue, float.MaxValue, double.MaxValue *0.98, decimal.MaxValue,(BigInteger)float.MaxValue
		};
		public readonly Type[] NonNumericTypes = new Type[] {
			typeof(Guid), typeof(DateTime), typeof(DateTimeOffset), typeof(TimeSpan)
		};

		public readonly List<object> BigIntegers = new List<object>() { (BigInteger)0, (BigInteger)1, new BigInteger(long.MinValue) - 1, new BigInteger(ulong.MaxValue) + 1 };
		public readonly List<object> DateTimes = new List<object>() { new DateTime(), new DateTime(1), DateTime.Now, DateTime.UtcNow, DateTime.MinValue, DateTime.MaxValue };
		public readonly List<object> DateTimeOffsets = new List<object>() { DateTimeOffset.Now, DateTimeOffset.UtcNow, DateTimeOffset.MinValue, DateTimeOffset.MaxValue };
		public readonly List<object> TimeSpans = new List<object>() { TimeSpan.Zero, DateTime.Now.TimeOfDay, TimeSpan.MinValue, TimeSpan.MaxValue };
		public readonly List<object> Strings = new List<object>() { "0", "1", "", "TRUE", "FALSE", "YES", "NO", "STR ING", "00:00:00", DateTime.Now.ToShortDateString(), DateTimeOffset.Now.ToString(), };

		[TestMethod]
		public void TestNumericCanConvert()
		{
			foreach (Type input in NumericTypes) {
				foreach (Type output in NumericTypes) {
					Assert.IsTrue(TypeConvert.CanConvertTo(input, output));
					Assert.IsTrue(TypeConvert.CanConvertTo(output, input));
				}
			}
		}

		[TestMethod]
		public void TestToString()
		{
			foreach (Type input in ParseTypes) {
				Assert.IsTrue(TypeConvert.CanConvertTo(input, typeof(string)));
			}
		}

		[TestMethod]
		public void TestCanParse()
		{
			foreach (Type type in ParseTypes) {
				Assert.IsTrue(TypeConvert.CanConvertTo(typeof(string), type));
			}
		}

		[TestMethod]
		public void TestParse()
		{
			for (int i = ParseTypes.ToList().IndexOf(typeof(DateTime)); i < ParseTypes.Length; i++) {
				string str = ParseValues[i].ToString();
				Type type = ParseTypes[i];
				object result = TypeConvert.ChangeType(str, type);
				Assert.IsTrue(result != null);
				Assert.AreEqual(str, result.ToString());
				// Custom Booleans
			}
			for (int i = 0; i < MaxValues.Length; i++) {
				object maxVal = MaxValues[i];
				object minVal = MinValues[i];
				object zeroVal = ZeroValues[i];
				object oneVal = OneValues[i];
				Assert.AreEqual(TypeConvert.ChangeType(maxVal.ToString(), maxVal.GetType()).ToString(), maxVal.ToString());
				Assert.AreEqual(TypeConvert.ChangeType(minVal.ToString(), minVal.GetType()).ToString(), minVal.ToString());
				Assert.AreEqual(TypeConvert.ChangeType(zeroVal.ToString(), zeroVal.GetType()), zeroVal);
				Assert.AreEqual(TypeConvert.ChangeType(oneVal.ToString(), oneVal.GetType()), oneVal);
			}
		}

		[TestMethod]
		public void TestTryParse()
		{
			for (int i = ParseTypes.ToList().IndexOf(typeof(DateTime)); i < ParseTypes.Length; i++) {
				string str = ParseValues[i].ToString();
				Type type = ParseTypes[i];
				Assert.IsTrue(TypeConvert.TryChangeType(str, type, out object result));
				Assert.AreEqual(str, result.ToString());
				// Custom Booleans
			}
			for (int i = 2; i < MaxValues.Length; i++) {
				object maxVal = MaxValues[i];
				object minVal = MinValues[i];
				object zeroVal = ZeroValues[i];
				object oneVal = OneValues[i];
				Assert.IsTrue(TypeConvert.TryChangeType(zeroVal.ToString(), zeroVal.GetType(), out object result));
				Assert.AreEqual(result.GetType(), zeroVal.GetType());
				Assert.AreEqual(result, zeroVal);
				Assert.IsTrue(TypeConvert.TryChangeType(oneVal.ToString(), oneVal.GetType(), out result));
				Assert.AreEqual(result, oneVal);
				Assert.IsTrue(TypeConvert.TryChangeType(maxVal.ToString(), maxVal.GetType(), out result));
				Assert.AreEqual(result.ToString(), maxVal.ToString());
				Assert.IsTrue(TypeConvert.TryChangeType(minVal.ToString(), minVal.GetType(), out result));
				Assert.AreEqual(result.ToString(), minVal.ToString());
			}
		}

		[TestMethod]
		public void TestNull()
		{
			object val = null;
			foreach (Type type in ParseTypes) {
				Assert.IsFalse(TypeConvert.TryChangeType(val, type, out object result));
			}
		}

		[TestMethod]
		public void TestDBNull()
		{
			object val = DBNull.Value;
			foreach (Type type in ParseTypes) {
				bool b = TypeConvert.TryChangeType(val, type, out object result);
				Assert.AreEqual(b, type == typeof(string));
			}
		}

		[TestMethod]
		public void TestCannotConvert()
		{
			foreach (Type input in NonNumericTypes) {
				foreach (Type output in NumericTypes) {
					bool invert = output == typeof(char) && input == typeof(bool);
					invert = invert || (input == typeof(char) && output == typeof(bool));
					Assert.AreEqual(TypeConvert.CanConvertTo(input, output), invert);
					Assert.AreEqual(TypeConvert.CanConvertTo(output, input), invert);
				}
			}
		}

		[TestMethod]
		public void TestNumericsConvert()
		{
			for (int i = 0; i < ZeroValues.Length; i++) {
				object zeroVal = ZeroValues[i];
				object oneVal = OneValues[i];
				foreach (Type type in NumericTypes) {
					if (type == typeof(BigInteger) && zeroVal.GetType() == typeof(char))
						continue;
					if (type == typeof(char) && zeroVal.GetType() == typeof(BigInteger))
						continue;
					object result = TypeConvert.ChangeType(zeroVal, type);
					Type zeroValType = zeroVal.GetType();
					Type resultType = result?.GetType();
					Assert.IsNotNull(result);
					Assert.AreEqual(type, resultType);
					object result2 = TypeConvert.ChangeType(oneVal, type);
					Assert.IsNotNull(result2);
					Assert.AreEqual(type, result2.GetType());
					Assert.IsTrue(result.ToString() == "0" || result.ToString() == "0.0");
					Assert.IsTrue(result2.ToString() == "1" || result2.ToString() == "1.0");
				}
			}
		}

		[TestMethod]
		public void TestNumericsAny()
		{
			for (int i = 0; i < MaxValues.Length; i++) {
				object maxVal = MaxValues[i];
				object minVal = MinValues[i];
				object zeroVal = ZeroValues[i];
				object oneVal = OneValues[i];
				foreach (Type type in NumericTypes) {
					TestAny(maxVal, type);
					TestAny(minVal, type);
					TestAny(zeroVal, type);
					TestAny(oneVal, type);
				}
			}
		}

		[TestMethod]
		public void TestChar()
		{
			for (int i = 0; i < NumericTypes.Length - 4; i++) {
				// do not use BigInteger, float, double, decimal
				Type type = NumericTypes[i];
				if (type == typeof(float) || type == typeof(decimal) || type == typeof(double))
					continue;
				object val = TypeConvert.ChangeType((char)5, type);
				Assert.IsNotNull(val);
				Type resultTy = val.GetType();
				Assert.AreEqual(type, resultTy);
				val = TypeConvert.ChangeType((char)0, type);
				Assert.IsNotNull(val);
				resultTy = val.GetType();
				Assert.AreEqual(type, resultTy);
			}
			for (int i = 0; i < OneValues.Length - 4; i++) {
				object oneVal = OneValues[i];
				object val = TypeConvert.ChangeType(oneVal, typeof(char));
				Assert.IsNotNull(val);
				Type type = val.GetType();
				Assert.AreEqual(type, typeof(char));
			}
			for (int i = 0; i < ZeroValues.Length - 4; i++) {
				object zeroVal = ZeroValues[i];
				object val = TypeConvert.ChangeType(zeroVal, typeof(char));
				Assert.IsNotNull(val);
				Type type = val.GetType();
				Assert.AreEqual(type, typeof(char));
			}
		}


		[TestMethod]
		public void TestBoolean()
		{
			for(int i = 0; i < NumericTypes.Length - 1; i++) {
				// do not use BigInteger
				Type type = NumericTypes[i];
				object val = TypeConvert.ChangeType(true, type);
				Assert.IsNotNull(val);
				Type resultTy = val.GetType();
				Assert.AreEqual(type, resultTy);
				val = TypeConvert.ChangeType(false, type);
				Assert.IsNotNull(val);
				resultTy = val.GetType();
				Assert.AreEqual(type, resultTy);
			}
			for (int i = 0; i < OneValues.Length - 1; i++) {
				object oneVal = OneValues[i];
				object val = TypeConvert.ChangeType(oneVal, typeof(bool));
				Assert.IsNotNull(val);
				Type type = val.GetType();
				Assert.AreEqual(type, typeof(bool));
			}
			for (int i = 0; i < ZeroValues.Length - 1; i++) {
				object zeroVal = ZeroValues[i];
				object val = TypeConvert.ChangeType(zeroVal, typeof(bool));
				Assert.IsNotNull(val);
				Type type = val.GetType();
				Assert.AreEqual(type, typeof(bool));
			}
		}

		[TestMethod]
		public void ToSelf()
		{
			foreach (Type type in ParseTypes.Where(t => !t.IsClass)) {
				object value = Activator.CreateInstance(type);
				object result = TypeConvert.ChangeType(value, type);
				Assert.AreEqual(result, value);
				Assert.AreEqual(result.GetType(), value.GetType());
			}
		}

		[TestMethod]
		public void TestBigInteger()
		{
			int floatIndex = NumericTypes.ToList().IndexOf(typeof(float));
			for (int i = 0; i < MaxValues.Length; i++) {
				object maxVal = MaxValues[i];
				object minVal = MinValues[i];
				object zeroVal = ZeroValues[i];
				object oneVal = OneValues[i];
				if (maxVal.GetType() != minVal.GetType())
					throw new InvalidOperationException();
				if (maxVal.GetType() != zeroVal.GetType())
					throw new InvalidOperationException();
				if (maxVal.GetType() != oneVal.GetType())
					throw new InvalidOperationException();
				TestAny(maxVal, typeof(BigInteger));
				TestAny(minVal, typeof(BigInteger));
				TestAny(zeroVal, typeof(BigInteger));
				TestAny(oneVal, typeof(BigInteger));
				if (i < floatIndex) {
					if (TypeConvert.TryChangeType(maxVal, typeof(BigInteger), out object maxVal2)) {
						Assert.AreEqual(maxVal.ToString(), maxVal2.ToString());
					}
					if (TypeConvert.TryChangeType(minVal, typeof(BigInteger), out object minVal2)) {
						Assert.AreEqual(minVal.ToString(), minVal2.ToString());
					}
				}
			}
		}

		private void TestTryConvert(object value, Type type)
		{
			if (TypeConvert.TryChangeType(value, type, out object result)) {
				Assert.AreEqual(value, result);
				Assert.AreEqual(result.GetType(), type);
			}
			else {
				try {
					object c = System.Convert.ChangeType(value, type);
					throw new InvalidOperationException();
				}
				catch {
				}
			}
		}

		private void TestAny(object value, Type type)
		{
			try {
				object result1 = TypeConvert.ChangeType(value, type);
				object result2 = System.Convert.ChangeType(value, type);
				Assert.AreEqual(result1, result2);
			}
			catch {
				try {
					object result2 = System.Convert.ChangeType(value, type);
					throw new InvalidOperationException();
				}
				catch {

				}
			}
		}

	}
}
