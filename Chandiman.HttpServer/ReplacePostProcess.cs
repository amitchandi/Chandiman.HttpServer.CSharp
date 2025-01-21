using System.Text.RegularExpressions;
using Chandiman.Extensions;

namespace Chandiman.HttpServer;
public class ReplacePostProcess
{
    const string ReplaceTag = @"<\?(\s).*(Replace.*=.*"".*"").*\?>";
    const string ReplaceKV = @"Replace.*=.*"".*""";
    const string TitleKV = @"Title.*=.*"".*""";
    /// <summary>
    /// something something darkside
    /// </summary>
    public static string Process(Router router, Session session, Dictionary<string, object?> kvParms, string html)
    {
        RegexOptions options = RegexOptions.Multiline | RegexOptions.IgnoreCase;

        string ret = "";

        foreach (Match m in Regex.Matches(html, ReplaceTag, options))
        {
            Console.WriteLine("'{0}' found at index {1}.", m.Value, m.Index);
            var replaceMatch = Regex.Match(m.Value, ReplaceKV, options);
            if (replaceMatch == null)
            {
                throw new Exception("Replace attribute is required.");
            }
            var file = replaceMatch.Value.RightOf("=").Replace("\"", "").Trim();
            
            ret = File.ReadAllText(router.WebsitePath + router.PathSeperator + file);

            var titleMatch = Regex.Match(m.Value, TitleKV, options);
            if (titleMatch != null)
            {
                var title = titleMatch.Value.RightOf("=").Replace("\"", "").Trim();
                var titleTag = $"\n<title>{title}</title>";
                ret += titleTag;
            }
        }

        return ret;
    }
}