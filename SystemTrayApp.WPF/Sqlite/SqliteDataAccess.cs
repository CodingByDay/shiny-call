using Dapper;
using ShinyCall.Mappings;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShinyCall.Sqlite
{
    public class SqliteDataAccess
    {

        public static List<CallModel> LoadCalls() {

            using (IDbConnection cnn = new SQLiteConnection(LoadConnectionString()))
            {
                var output = cnn.Query<CallModel>("select * from CallHistory", new DynamicParameters());
                return output.ToList(); 
            }
        }


        public static void InsertCallHistory(CallModel model)
        {
            using (IDbConnection cnn = new SQLiteConnection(LoadConnectionString()))
            {
                cnn.Execute("insert into CallHistory(caller, status, time) values (@caller, @status, @time)", model);
            }
        }


        private static string LoadConnectionString(string id = "Default")
        {
            return ConfigurationManager.ConnectionStrings[id].ConnectionString;
        }
    }
}
