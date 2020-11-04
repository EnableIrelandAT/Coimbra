// Licensed under the MIT License.

namespace DataAccessLibrary
{
    using Microsoft.Data.Sqlite;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Windows.Storage;

    /// <summary>
    /// Data access class for the local User table in SQLite database
    /// </summary>
    public static class DataAccess
    {
        /// <summary>
        /// Initializes the database, which is called when application starts
        /// </summary>
        public async static void InitializeDatabase()
        {
            await ApplicationData.Current.LocalFolder.CreateFileAsync("coimbra.db", CreationCollisionOption.OpenIfExists);
            string dbpath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "coimbra.db");
            using (SqliteConnection db =
               new SqliteConnection($"Filename={dbpath}"))
            {
                db.Open();

                string tableCommand = "CREATE TABLE IF NOT " +
                    "EXISTS User (Primary_Key INTEGER PRIMARY KEY, " +
                    "Name NVARCHAR(2048) NULL)";

                SqliteCommand createTable = new SqliteCommand(tableCommand, db);

                createTable.ExecuteReader();
            }
        }

        /// <summary>
        /// Adds data to the User table
        /// </summary>
        /// <param name="inputText">The input text.</param>
        public static void AddData(string inputText)
        {
            string dbpath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "coimbra.db");
            using (SqliteConnection db =
              new SqliteConnection($"Filename={dbpath}"))
            {
                db.Open();

                SqliteCommand insertCommand = new SqliteCommand();
                insertCommand.Connection = db;

                // Use parameterized query to prevent SQL injection attacks
                insertCommand.CommandText = "INSERT INTO User VALUES (NULL, @Entry);";
                insertCommand.Parameters.AddWithValue("@Entry", inputText);

                insertCommand.ExecuteReader();

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

            string dbpath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "coimbra.db");
            using (SqliteConnection db =
               new SqliteConnection($"Filename={dbpath}"))
            {
                db.Open();

                SqliteCommand selectCommand = new SqliteCommand
                    ("SELECT Name from User", db);

                SqliteDataReader query = selectCommand.ExecuteReader();

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
        /// <returns></returns>
        public static bool Exists(string name)
        {
            return GetAllData().Contains(name);
        }
    }
}