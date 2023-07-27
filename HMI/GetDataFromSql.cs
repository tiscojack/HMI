using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;

namespace HMI
{
    internal class GetDataFromSql
    {
        private SqlConnection sqlConnection;
        private List<List<string>> sqlData;

        public GetDataFromSql(SqlConnection sqlConn) 
        {
            sqlData = new List<List<string>>();
            sqlConnection = sqlConn;
        }

        public List<List<string>> getsqlData(Languages Language, ItemStatus isActive)
        {
            sqlData.Clear();
            execQuery(Language, isActive, 1);
            return sqlData;
        }

        public void execQuery(Languages Lang, ItemStatus Active, int IdGroups)
        {
            lock (sqlConnection)
            {
                try
                {
                    {
                        SqlCommand command = new SqlCommand("GetMenuHeaderItem", sqlConnection);
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.Add("Language", SqlDbType.Int).Value = Convert.ToInt32(Lang);
                        command.Parameters.Add("IsActive", SqlDbType.Bit).Value = Convert.ToInt32(Active);
                        command.Parameters.Add("IdGroups", SqlDbType.Int).Value = Convert.ToInt32(Active);
                        sqlConnection.Open();
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                List<string> item = new List<string>();
                                item.Add(reader.GetString(0));
                                item.Add(reader.GetInt32(1).ToString());
                                sqlData.Add(item);
                            }
                        }
                    }
                }
                catch (SqlException e)
                {
                    MessageBox.Show(e.ToString());
                }
                sqlConnection.Close();
            }
        }
    }
}
