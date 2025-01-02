using System.Text.RegularExpressions;

public class SqlToLinqGenerator
{
    int variable = 0;
    string dbcontextRef = "_db";
    public string ConvertSqlToLinq(string sqlQuery)
    {
        try
        {
            // Normalize SQL query
            sqlQuery = sqlQuery.Trim();
            List<string> sqlQueryParts = SeparateByKeywords(sqlQuery, ["Select", "From", "Where"]);
            Dictionary<string, string> aliceReplacer = new Dictionary<string, string>();

            string linqQuery = TranslateFromClause(sqlQueryParts.Where(x => x.StartsWith("FROM")).FirstOrDefault(), aliceReplacer);
            linqQuery += TranslateSelectClause(sqlQueryParts.Where(x => x.StartsWith("SELECT")).FirstOrDefault(), aliceReplacer);
            return linqQuery + ";\r\n";
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    private string TranslateSelectClause(string? sqlSelectPart, Dictionary<string, string> aliceReplacer)
    {
        sqlSelectPart = sqlSelectPart?.Trim();
        if (sqlSelectPart == null)
        {
            return string.Empty;
        }
        string[] sqlSelectprop = sqlSelectPart.Remove(0, 6).Split(",") ?? [];
        string selectQuery = "select new { \r\n";
        foreach (var select in sqlSelectprop)
        {

            selectQuery += $"{AliceReplacer(select, aliceReplacer)},\r\n";
        }
        selectQuery += "}";
        return selectQuery;
    }

    private string TranslateFromClause(string? sqlFormPart, Dictionary<string, string> aliceReplacer)
    {
        if (sqlFormPart == null)
        {
            return string.Empty;
        }
        List<string> sqlQueryParts = SeparateByKeywords(sqlFormPart, ["Left Join"]);

        string? linqBaseSet = GetWordAfterKeyword(sqlFormPart, "From") ?? string.Empty;
        string linqBaseAlice = GetWordAfterKeyword(sqlFormPart, "As") ?? $"v{variable++}";

        aliceReplacer[linqBaseSet] = linqBaseAlice;

        string linqFormPart = $"from {linqBaseAlice} in {dbcontextRef}.{linqBaseSet}List \r\n";

        for (int i = 0; i < sqlQueryParts.Count; i++)
        {
            string? linqJoinSet = GetWordAfterKeyword(sqlQueryParts[i], "join") ?? string.Empty;
            string linqJoinAlice = GetWordAfterKeyword(sqlQueryParts[i], "As") ?? $"v{variable++}";

            string[] sqlJoinCom = sqlQueryParts[i].Split("ON")?[1].Trim().Split("AND") ?? [];

            List<string> leftJoinPart = new(), rightJoinPart = new();
            foreach (var c in sqlJoinCom)
            {
                string[] parts = c.Split("=");
                leftJoinPart.Add(parts[0].Trim());
                rightJoinPart.Add(parts[1].Trim());
            }
            string leftJoinArrayStr = GetJoinPopArray(leftJoinPart, aliceReplacer);
            string rightJoinArrayStr = GetJoinPopArray(rightJoinPart, aliceReplacer);

            string tempAlice = $"v{variable++}";
            string newAlice = $"v{variable++}";

            linqFormPart += $"join {linqJoinAlice} in {dbcontextRef}.{linqJoinSet}List \r\n on {leftJoinArrayStr} equals {rightJoinArrayStr} into {tempAlice} \r\n from {newAlice} in {tempAlice}.DefaultIfEmpty() \r\n";
            aliceReplacer[linqJoinSet] = newAlice;
            aliceReplacer[linqJoinAlice] = newAlice;
        }
        return linqFormPart;
    }

    private string GetJoinPopArray(List<string> joinPart, Dictionary<string, string> aliceReplacer)
    {
        int propNo = 1;
        string joinPropList = string.Empty;
        foreach (var part in joinPart)
        {
            joinPropList += $"p{propNo++} = {AliceReplacer(part, aliceReplacer)},";
        }
        joinPropList.Remove(joinPropList.Length - 1);
        return $"new {{{joinPropList}}}";
    }

    private string AliceReplacer(string propWithAlice, Dictionary<string, string> aliceReplacer)
    {
        string[] joinPropPart = propWithAlice.Split(".");
        string alice = joinPropPart[0].Trim();
        string prop = joinPropPart[1].Trim();
        string liqAlice = aliceReplacer.ContainsKey(alice) ? aliceReplacer[alice] : alice;
        return $"{liqAlice}.{prop}";
    }

    private string ExtractClause(string sqlQuery, string pattern)
    {
        var match = Regex.Match(sqlQuery, pattern, RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : string.Empty;
    }
    static List<string> SeparateByKeywords(string input, List<string> keywords)
    {
        string pattern = $@"\b({string.Join("|", keywords)})\b";
        Regex regex = new Regex(pattern, RegexOptions.IgnoreCase);

        var matches = regex.Matches(input);
        var result = new List<string>();

        for (int i = 0; i < matches.Count; i++)
        {
            Match match = matches[i];
            int nextIndx = i + 1 >= matches.Count ? input.Length : matches[i + 1].Index;
            result.Add(input.Substring(match.Index, nextIndx - match.Index).Trim());
        }
        return result;
    }

    static string? GetWordAfterKeyword(string input, string keyword)
    {
        string pattern = $@"\b{keyword}\b\s+(\w+)";
        Match match = Regex.Match(input, pattern, RegexOptions.IgnoreCase);

        return match.Success ? match.Groups[1].Value : null;
    }
}

public class Program
{
    public static void Main()
    {
        string sqlQuery = @"";

        SqlToLinqGenerator generator = new SqlToLinqGenerator();
        string linqQuery = generator.ConvertSqlToLinq(sqlQuery);

        Console.WriteLine(linqQuery);
    }
}
