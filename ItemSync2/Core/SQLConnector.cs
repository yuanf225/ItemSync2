﻿using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Windows;
using WDBXLib.Definitions.WotLK;

namespace ItemSync2.Core
{
    class SQLConnector
    {
        private SQLDataConfig conf;
        private MySqlConnection connection;

        private string host;
        private int port;
        private string database;
        private string table;
        private SecureString login;
        private SecureString password;

        private SecureString connectionString;


        public SQLConnector()
        {
            try
            {
                conf = new SQLDataConfig("Configs/SQLConfig.xml");
            }
            catch (Exception e) { MessageBox.Show(e.ToString()); throw; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <param name="database"></param>
        /// <param name="table"></param>
        /// <param name="login"></param>
        /// <param name="password"></param>
        public void SetConnectionInformation(string host, int port, string database, string table, SecureString login, SecureString password)
        {
            try
            {
                this.host = host;
                this.port = port;
                this.database = database;
                this.table = table;
                this.login = login;
                this.password = password;
                connectionString = Utilities.ToSecureString(string.Format("Server={0}; Port={1}; Database={2}; Uid={3}; Pwd={4}; SslMode=none;",
                    host, port, database, Utilities.ToInsecureString(login), Utilities.ToInsecureString(password)));
            }
            catch (Exception e) { throw new Exception("Error occured while attempting to set connection string.:\n\n" + e.Message); }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <param name="database"></param>
        /// <param name="table"></param>
        /// <param name="login"></param>
        /// <param name="password"></param>
        public void TestConnection(string host, int port, string database, string table, SecureString login, SecureString password)
        {
            SetConnectionInformation(host, port, database, table, login, password);
            try
            {
                connection = new MySqlConnection(Utilities.ToInsecureString(connectionString));
                connection.Open();
                MessageBox.Show("Connection was succesful.");
                connection.Close();
            }
            catch (Exception e) { throw new Exception("Error occured while establishing database connection.\n\n" + e.Message); }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public Dictionary<int, Item> GetItems(int start, int end)
        {
            Dictionary<int, Item> result = new Dictionary<int, Item>();

            try
            {
                if (start > end)
                    throw new ArgumentException(string.Format("Start entry value ({0}) has to be lower or equal to end value ({1}).", start, end));

                connection = new MySqlConnection(Utilities.ToInsecureString(connectionString));
                connection.Open();
                var query = new MySqlCommand(string.Format("SELECT {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7} FROM {8} WHERE {0} BETWEEN {9} AND {10};",
                    conf.ID, conf.ClassID, conf.SubclassID, conf.SoundOverrideSubclassID, conf.Material, conf.DisplayID, conf.InventoryType, conf.SheatheType,
                    table, start, end), connection);
                using (var r = query.ExecuteReader())
                {
                    while (r.Read())
                    {
                        result.Add(Convert.ToInt32(r[0]), new Item()
                        {
                            ID = Convert.ToInt32(r[0]),
                            ClassID = Convert.ToInt32(r[1]),
                            SubclassID = Convert.ToInt32(r[2]),
                            Sound_override_subclassid = Convert.ToInt32(r[3]),
                            Material = Convert.ToInt32(r[4]),
                            DisplayInfoID = Convert.ToInt32(r[5]),
                            InventoryType = Convert.ToInt32(r[6]),
                            SheatheType = Convert.ToInt32(r[7])
                        });
                    }
                }
                connection.Close();
            }
            catch (Exception e) { throw new Exception("Error occured while attempting to get item data from database.\n\n" + e.Message); }

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="newFromDBC"></param>
        public void InsertIntoDB(List<Item> newFromDBC)
        {
            try
            {
                connection = new MySqlConnection(Utilities.ToInsecureString(connectionString));
                connection.Open();

                string cols = "";
                foreach (var defVal in conf.defaultValues)
                    cols += string.Format("`{0}`, ", defVal.Key);
                cols += string.Format("`{0}`, `{1}`, `{2}`, `{3}`, `{4}`, `{5}`, `{6}`, `{7}`",
                    conf.ID, conf.ClassID, conf.SubclassID, conf.SoundOverrideSubclassID, conf.Material, conf.DisplayID,
                    conf.InventoryType, conf.SheatheType);
                string query = string.Format("START TRANSACTION;\r\nINSERT INTO `{0}` ({1}) VALUES\r\n", table, cols);
                bool first = true;
                foreach (var item in newFromDBC)
                {
                    if (!first)
                        query += ",\r\n";
                    query += "(";
                    foreach (var defVal in conf.defaultValues)
                        query += string.Format("\"{0}\", ", defVal.Value);
                    query += string.Format("{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7})",
                        item.ID, item.ClassID, item.SubclassID, item.Sound_override_subclassid, item.Material, item.DisplayInfoID,
                        item.InventoryType, item.SheatheType);
                    first = false;
                }
                query += ";\r\nCOMMIT;";

                using (var sw = new StreamWriter("SQLQueryBackup.sql"))
                {
                    sw.Write(query);
                }

                var command = new MySqlCommand(query, connection);
                command.ExecuteNonQuery();
                connection.Close();
            }
            catch (Exception e) { throw new Exception("Error occured while attempting to insert data into database. SQLQueryBackup.sql contains backup of query.\n\n" + e.Message); }
        }
    }
}
