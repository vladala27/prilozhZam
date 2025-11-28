using Microsoft.Data.Sqlite;

namespace WordsLearningApp
{
    public static class DbHelper
    {
        // БД words.db лежит рядом с exe
        public static readonly string ConnectionString =
            "Data Source=words.db;Foreign Keys=True";

        public static SqliteConnection GetConnection()
            => new SqliteConnection(ConnectionString);
    }
}