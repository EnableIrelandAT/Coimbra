// Licensed under the MIT License.

namespace Coimbra.DataAccess
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.Data.Sqlite;
    using Windows.Storage;

    /// <summary>
    /// Data access class for the local User table in SQLite database
    /// </summary>
    public static class DataAccess
    {
        /// <summary>
        /// Initializes the database, which is called when application starts
        /// </summary>
        public static async Task InitializeDatabaseAsync()
        {
            await ApplicationData.Current.LocalFolder.CreateFileAsync(databaseName, CreationCollisionOption.OpenIfExists)
                .AsTask().ConfigureAwait(false);

            using (SqliteConnection db = new SqliteConnection(filePath))
            {
                db.Open();

                SqliteCommand createTableCommand = new SqliteCommand("CREATE TABLE IF NOT " +
                    "EXISTS User (Primary_Key INTEGER PRIMARY KEY, " +
                    "Name NVARCHAR(100) NULL)", db);

                ExecuteCommand(createTableCommand);
            }
        }

        /// <summary>
        /// Adds data to the User table
        /// </summary>
        /// <param name="inputText">The input text.</param>
        public static void AddData(string inputText)
        {
            using (SqliteConnection db = new SqliteConnection(filePath))
            {
                db.Open();

                SqliteCommand insertCommand = new SqliteCommand();
                insertCommand.Connection = db;

                // Use parameterized query to prevent SQL injection attacks
                insertCommand.CommandText = "INSERT INTO User VALUES (NULL, @Entry);";
                insertCommand.Parameters.AddWithValue("@Entry", inputText);

                ExecuteCommand(insertCommand);

                db.Close();
            }
        }

        /// <summary>
        /// Gets all data.
        /// </summary>
        /// <returns>all entries from User table</returns>
        public static List<string> GetAllData()
        {
            List<string> entries = new List<string>();

            using (SqliteConnection db = new SqliteConnection(filePath))
            {
                db.Open();

                SqliteDataReader query = ExecuteCommand(new SqliteCommand("SELECT Name from User", db));

                while (query.Read())
                {
                    entries.Add(query.GetString(0));
                }

                db.Close();
            }

            // returns true if specified name is in User table
            return entries;
        }

        /// <summary>
        /// Checks if specified name exists in User table
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>true if name exists in local table</returns>
        public static bool Exists(string name)
        {
            return GetAllData().Contains(name);
        }

        private static SqliteDataReader ExecuteCommand(SqliteCommand command)
        {
            return command.ExecuteReader();
        }

        private static string databaseName = "coimbra.db";

        private static string dbpath = Path.Combine(ApplicationData.Current.LocalFolder.Path, databaseName);

        // FormattableString.Invariant used as strings vary by their culture they've run in (e.g. Windows language)
        private static string filePath = FormattableString.Invariant($"Filename={dbpath}");
    }
}