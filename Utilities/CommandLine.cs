using System;
using System.Collections.Generic;
using System.Linq;

namespace SolarNG.Utilities;

public static class CommandLine
{
    private static IEnumerable<string> Split(this string str, Func<char, bool> controller)
    {
        int num = 0;
        for (int c = 0; c < str.Length; c++)
        {
            if (controller(str[c]))
            {
                yield return str.Substring(num, c - num);
                num = c + 1;
            }
        }
        yield return str.Substring(num);
    }

    private static string TrimMatchingQuotes(this string input, char quote)
    {
        if (input.Length >= 2 && input[0] == quote && input[input.Length - 1] == quote)
        {
            return input.Substring(1, input.Length - 2);
        }
        return input;
    }

    public static IEnumerable<string> Split(string commandLine)
    {
        bool inQuotes = false;
        return from arg in commandLine.Split(delegate(char c)
            {
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                return !inQuotes && c == ' ';
            })
            select arg.Trim().TrimMatchingQuotes('"') into arg
            where !string.IsNullOrEmpty(arg)
            select arg;
    }
}
