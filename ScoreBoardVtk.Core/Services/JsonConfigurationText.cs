using System.Text;

namespace ScoreBoardVtk.Core.Services;

internal static class JsonConfigurationText
{
    public static string Normalize(string json)
    {
        return StripComments(json);
    }

    private static string StripComments(string text)
    {
        var builder = new StringBuilder(text.Length);
        var inString = false;
        var escape = false;

        for (var index = 0; index < text.Length; index++)
        {
            var current = text[index];

            if (inString)
            {
                builder.Append(current);

                if (escape)
                {
                    escape = false;
                }
                else if (current == '\\')
                {
                    escape = true;
                }
                else if (current == '"')
                {
                    inString = false;
                }

                continue;
            }

            if (current == '"')
            {
                inString = true;
                builder.Append(current);
                continue;
            }

            if (current == '/' && index + 1 < text.Length)
            {
                var next = text[index + 1];

                if (next == '/')
                {
                    index += 2;

                    while (index < text.Length && text[index] != '\n')
                    {
                        index++;
                    }

                    if (index < text.Length)
                    {
                        builder.Append(text[index]);
                    }

                    continue;
                }

                if (next == '*')
                {
                    index += 2;

                    while (index + 1 < text.Length && !(text[index] == '*' && text[index + 1] == '/'))
                    {
                        index++;
                    }

                    index++;
                    continue;
                }
            }

            builder.Append(current);
        }

        return builder.ToString();
    }
}
