using SQLtoLINQ;

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
