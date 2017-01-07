using System.Text.RegularExpressions;

namespace RadarrAPI.Util
{
    public static class RegexUtil
    {

        public static Regex ReleaseFeaturesGroup = new Regex(@"\*\*New features:\*\*\r\n(?<features>.*?\r\n)\r\n", RegexOptions.Compiled | RegexOptions.Singleline);
        public static Regex ReleaseFixesGroup = new Regex(@"\*\*Fixes:\*\*\r\n(?<fixes>.*?\r\n)\r\n", RegexOptions.Compiled | RegexOptions.Singleline);
        public static Regex ReleaseChange = new Regex(@"- (?<text>.*?)\r\n", RegexOptions.Compiled);

    }
}
