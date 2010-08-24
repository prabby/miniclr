////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;

namespace System
{
    //We don't want to implement this whole class, but VB needs an external function to convert any integer type to a Char.
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public static class Convert
    {
        [CLSCompliant(false)]
        public static char ToChar(ushort value)
        {
            return (char)value;
        }

        [CLSCompliant(false)]
        public static sbyte ToSByte(string value)
        {
            if (value == null)
                return 0;

            value = value.Trim(' ');

            long res = ToInt64(value, true, SByte.MinValue, SByte.MaxValue);

            return (sbyte)res;
        }

        public static byte ToByte(string value)
        {
            if (value == null)
                return 0;

            value = value.Trim(' ');

            long res = ToInt64(value, false, Byte.MinValue, Byte.MaxValue);

            return (byte)res;
        }

        public static short ToInt16(string value)
        {
            if (value == null)
                return 0;

            value = value.Trim(' ');

            long res = ToInt64(value, true, Int16.MinValue, Int16.MaxValue);

            return (short)res;
        }

        [CLSCompliant(false)]
        public static ushort ToUInt16(string value)
        {
            if (value == null)
                return 0;

            value = value.Trim(' ');

            long res = ToInt64(value, false, UInt16.MinValue, UInt16.MaxValue);

            return (ushort)res;
        }

        public static int ToInt32(string value)
        {
            if (value == null)
                return 0;

            value = value.Trim(' ');

            long res = ToInt64(value, true, Int32.MinValue, Int32.MaxValue);

            return (int)res;
        }

        [CLSCompliant(false)]
        public static uint ToUInt32(string value)
        {
            if (value == null)
                return 0;

            value = value.Trim(' ');

            long res = ToInt64(value, false, UInt32.MinValue, UInt32.MaxValue);

            return (uint)res;
        }

        public static long ToInt64(string value)
        {
            if (value == null)
                return 0;

            value = value.Trim(' ');

            return ToInt64(value, true, Int64.MinValue, Int64.MaxValue);
        }

        [CLSCompliant(false)]
        public static ulong ToUInt64(string value)
        {
            if (value == null)
                return 0;

            value = value.Trim(' ');

            long res = ToInt64(value, false, 0, 0);

            return (ulong)res;
        }

        //--//

        public static int ToInt32(string hexNumber, int fromBase)
        {
            if (hexNumber == null)
                return 0;

            hexNumber = hexNumber.Trim(' ');

            if (hexNumber == null)
                return 0;

            if (fromBase != 16)
                throw new ArgumentException();

            string hexParser = "0123456789ABCDEF";
            int result = 0;
            int exp = 1;
            int hexIndex;

            // Trim hex sentinal if present and upper the string
            char[] hexDigit = hexNumber.Substring((hexNumber.IndexOf("0x") == 0 || hexNumber.IndexOf("0X") == 0) ? 2 : 0).ToUpper().ToCharArray();
            int i = hexDigit.Length - 1;

            // Convert hex to integer
            for (; i >= 0; --i)
                if ((hexIndex = hexParser.IndexOf(hexDigit[i])) == -1)
                    throw new ArgumentOutOfRangeException();
                else
                {
                    result += hexIndex * exp;
                    exp = exp * 16;
                }

            return result;
        }

        public static double ToDouble(string s)
        {
            if (s == null)
                return 0;

            s = s.Trim(' ');

            string s1 = s.ToLower();
            int decimalpoint = s1.IndexOf('.');
            int exp = s1.IndexOf('e');
            if (decimalpoint != -1 && exp != -1 && decimalpoint > exp)
                throw new Exception();

            double power = 0;
            if (exp != -1 && exp + 1 < s1.Length - 1)
            {
                string strPower = s1.Substring(exp + 1, s1.Length - (exp + 1));

                char[] chars = strPower.ToCharArray();
                power = GetDoubleNumber(chars);
            }

            double rightDecimal = 0;
            if (decimalpoint != -1)
            {
                string s2;
                if (exp == -1)
                    s2 = s1.Substring(decimalpoint + 1);
                else
                    s2 = s1.Substring(decimalpoint + 1, exp - (decimalpoint + 1));

                char[] chars = s2.ToCharArray();
                double number = GetDoubleNumber(chars);
                rightDecimal = number * System.Math.Pow(10, -s2.Length);
            }

            double leftDecimal = 0;

            if (decimalpoint != 0)
            {
                string s3;
                if (decimalpoint == -1 && exp == -1) s3 = s1;
                else if (decimalpoint != -1) s3 = s1.Substring(0, decimalpoint);
                else s3 = s1.Substring(0, exp);

                char[] chars = s3.ToCharArray();
                leftDecimal = GetDoubleNumber(chars);
            }

            double value = 0;
            if (leftDecimal < 0)
            {
                value = -leftDecimal + rightDecimal;
                value = -value;
            }
            else
            {
                value = leftDecimal + rightDecimal;
            }

            // lets normalize
            while (value > 10.0 || value < -10.0)
            {
                value /= 10.0;
                power++;
            }

            if (value != 0.0)
            {
                while (value < 1.0 && value > -1.0)
                {
                    value *= 10.0;
                    power--;
                }
            }

            // special case for epsilon (the System.Math.Pow native method will return zero for -324)
            if (power == -324)
            {
                value = value * System.Math.Pow(10, power + 1);
                value /= 10.0;
            }
            else
            {
                value = value * System.Math.Pow(10, power);
            }

            if (value == double.PositiveInfinity || value == double.NegativeInfinity)
            {
                throw new Exception();
            }

            return value;
        }

        //--//

        private static long ToInt64(string value, bool signed, long min, long max)
        {
            char[] num = value.ToCharArray();

            ulong result = 0;

            bool isNegative = false;
            int signIndex = 0;

            // check the sign
            if (num[0] == '-')
            {
                isNegative = true;
                signIndex = 1;
            }
            else if (num[0] == '+')
            {
                signIndex = 1;
            }

            if (num.Length - signIndex > 20)
            {
                throw new Exception();
            }

            // check how many leading zeroes
            int leadingZeroes = 0;
            for (int i = signIndex; i < value.Length; ++i)
            {
                if (num[i] != '0')
                {
                    break;
                }

                ++leadingZeroes;
            }

            // if there were any leading zeroes, they remove them
            if (leadingZeroes > 0)
            {
                // if the string was just a sign and all zeroes, then we should return zero
                // +0000, -0000, 0000
                if (num.Length == signIndex + leadingZeroes)
                {
                    return 0;
                }

                char[] numCpy = new char[num.Length - (leadingZeroes + signIndex)];

                if (signIndex == 1)
                {
                    numCpy[0] = num[0];
                }

                Array.Copy(num, signIndex + leadingZeroes, numCpy, 0, numCpy.Length);
            }

            // at this point we can check whether we are supposed to be signed or not and if we are negative or not
            // we need to do it now because '-0' is a valid unsigned zero
            if (!signed && isNegative)
            {
                throw new Exception();
            }

            int add1 = isNegative ? 1 : 0;

            // perform the conversion
            ulong exp = 1;
            for (int i = num.Length - 1; i >= signIndex; i--)
            {
                if (num[i] < '0' || num[i] > '9')
                {
                    throw new ArgumentException();
                }

                // check if we will overflow
                ulong add = ((ulong)(num[i] - '0') * exp);

                UInt64 comparand = UInt64.MaxValue;

                comparand -= result;

                if (comparand != UInt64.MaxValue)
                {
                    comparand += (ulong)add1;
                }

                if (comparand < add)
                {
                    throw new Exception();
                }

                result += add;

                exp *= 10;
            }

            if (!isNegative && max != 0 && result > (ulong)max)
            {
                throw new Exception();
            }

            long res = (long)result;

            if (isNegative)
            {
                if (result > (ulong)Int64.MaxValue + 1)
                {
                    throw new Exception();
                }

                res = -1 * (long)res;
            }

            if (isNegative && min != 0 && res < min)
            {
                throw new Exception();
            }

            if (isNegative)
            {
                if (result > (ulong)Int64.MaxValue + 1)
                {
                    throw new Exception();
                }

                return -1 * (long)result;
            }

            return res;

        }

        private static double GetDoubleNumber(char[] chars)
        {
            int digit;
            double place = 1;
            double number = 0;
            int firstDigit = chars[0] == '-' || chars[0] == '+' ? 1 : 0;
            int multiplier = chars[0] == '-' ? -1 : 1;

            for (int i = chars.Length - 1; i >= firstDigit; i--)
            {
                if (chars[i] < '0' || chars[i] > '9')
                    throw new Exception();

                digit = chars[i] - '0';
                number += digit * place;
                place = place * 10;
            }

            return number * multiplier;
        }
    }
}


