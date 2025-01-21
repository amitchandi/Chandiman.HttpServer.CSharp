using System.Text.RegularExpressions;

namespace Chandiman.HttpServer;
public class ReplacePostProcess
{
    const string ReplaceTag = @"<\?(\s).*(Replace.*=.*"".*"").*\?>";
    const string ReplaceKV = @"Replace.*=.*"".*""";
    const string TitleKV = @"Title.*=.*"".*""";
    /// <summary>
    /// something something darkside
    /// </summary>
    public static string Process(Session session, Dictionary<string, object?> kvParms, string html)
    {
        RegexOptions options = RegexOptions.Multiline | RegexOptions.IgnoreCase;

        foreach (Match m in Regex.Matches(html, ReplaceTag, options))
        {
            Console.WriteLine("'{0}' found at index {1}.", m.Value, m.Index);
            var titleMatch = Regex.Match(m.Value.Substring(m.Index, m.Length), TitleKV, options);
            if (titleMatch != null)
            {

            }
        }

        string ret = "";

        return ret;
    }
}
