using System;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace ZWOptical.SDK
{
    internal static class Common
    {
        private static readonly Regex VersionParser = new Regex(@"(\d+)(?:[,]?\s*)?",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

        /// <summary>
        /// Returns SDK version with this format: <code>1, 51, [1], [0]</code>
        /// </summary>
        /// <returns></returns>
        public static Version ParseVersionString(IntPtr charPtr)
        {
            var verStr = Marshal.PtrToStringAnsi(charPtr);
            if (string.IsNullOrEmpty(verStr))
            {
                return new Version();
            }
            else
            {
                var matches = VersionParser.Matches(verStr);

                switch (matches.Count)
                {
                    case 2: return new Version(VersionPart(matches, 0), VersionPart(matches, 1));
                    case 3: return new Version(VersionPart(matches, 0), VersionPart(matches, 1), VersionPart(matches, 2));
                    case 4: return new Version(VersionPart(matches, 0), VersionPart(matches, 1), VersionPart(matches, 2), VersionPart(matches, 3));
                    default: return new Version();
                }
            }
        }

        static int VersionPart(MatchCollection matches, int part) => Convert.ToInt32(matches[part].Groups[1].Value);
    }
}
