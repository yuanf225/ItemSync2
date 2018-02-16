﻿using System;
using System.Collections.Generic;
using System.Windows;
using System.IO;

namespace ItemSync2.Core
{
    class CoreController
    {
        private static CoreController instance;

        private CoreController() { }

        public static CoreController Instance
        {
            get
            {
                if (instance == null)
                    instance = new CoreController();
                return instance;
            }
        }

        private UserSettings usi = UserSettings.Instance;
        private SQLConnector sql = new SQLConnector();
        private DBCConnector dbc = new DBCConnector();

        public void TestConnection()
        {
            try
            {
                sql.TestConnection(usi.host, usi.port, usi.database, usi.table, usi.login, usi.password);
            }
            catch (Exception e) { MessageBox.Show("Connection to MySQL server was not successful.\n\n" + e.Message); }
        }

        public void SaveUserSettings()
        {
            usi.SaveUserSettings();
        }

        public void Help()
        {
            System.Diagnostics.Process.Start("https://github.com/Amaroth/ItemSync2/issues");
        }

        public void CheckChanges()
        {
            try
            {
                sql.SetConnectionInformation(usi.host, usi.port, usi.database, usi.table, usi.login, usi.password);
                dbc.SetDBCFile(usi.dbcPath);
                var inDatabase = sql.GetItems(usi.startID, usi.endID);
                var inDbc = dbc.GetItems(usi.startID, usi.endID);
                int missingInDbc = 0;
                int missingInDb = 0;
                int different = 0;
                foreach (var item in inDatabase)
                {
                    if (!inDbc.ContainsKey(item.Key))
                        missingInDbc++;
                    else if (!Utilities.AreEqual(inDbc[item.Key], inDatabase[item.Key]))
                        different++;
                }
                foreach (var item in inDbc)
                    if (!inDatabase.ContainsKey(item.Key))
                        missingInDb++;
                MessageBox.Show(string.Format("{0} items in database are missing in DBC.\n{1} items in DBC are missing in database.\n{2} items are in both DBC and database, but have different data.",
                    missingInDbc, missingInDb, different));
            }
            catch (Exception e) { MessageBox.Show("Couldn't check for changes, following error occured.:\n\n" + e.Message); }
        }

        public void DbToDbcSync()
        {

        }

        public void DbcToDbSync()
        {

        }
    }
}
