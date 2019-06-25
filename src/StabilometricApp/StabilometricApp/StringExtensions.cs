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

        /// <summary>
        /// Converts a word to title case.
        /// </summary>
        /// <remarks>
        /// Does not respect acronyms and full uppercase words.
        /// </remarks>
        public static string ToTitleCase(this string s) {
            if(string.IsNullOrEmpty(s))
                return string.Empty;

            var sb = new StringBuilder(s.Length);

            bool waitingForWord = true;
            foreach(var c in s) {
                if(waitingForWord && Char.IsLetterOrDigit(c)) {
                    //Found first letter of word
                    sb.Append(Char.ToUpperInvariant(c));
                    waitingForWord = false;
                }
                else {
                    sb.Append(Char.ToLowerInvariant(c));

                    if(Char.IsWhiteSpace(c)) {
                        waitingForWord = true;
                    }
                }
            }

            return sb.ToString();
        }

    }

}
