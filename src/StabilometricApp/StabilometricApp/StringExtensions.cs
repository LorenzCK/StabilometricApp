using System;
using System.Collections.Generic;
using System.Text;

namespace StabilometricApp {

    public static class StringExtensions {

        public static string ToFilenamePart(this string s) {
            if(s == null) {
                return null;
            }

            var sb = new StringBuilder(s.Length);
            bool hasDash = true;
            foreach(var c in s) {
                if(c >= 0x30 && c <= 0x39 || c >= 0x41 && c <= 0x5A || c >= 0x61 && c <= 0x7A) {
                    sb.Append(c);
                    hasDash = false;
                }
                else if(!hasDash) {
                    sb.Append('-');
                    hasDash = true;
                }
            }
            return sb.ToString().Trim('-', ' ');
        }

    }

}
