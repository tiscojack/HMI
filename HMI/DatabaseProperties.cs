using Microsoft.Data.SqlClient;

namespace HMI
{
    public class DatabaseProperties
    {
        private SqlConnectionStringBuilder sqlConnectionStringBuilder = new SqlConnectionStringBuilder();
        private SqlConnection sqlConn = new SqlConnection();

        public DatabaseProperties()
        {
            doSqlConnection();
        }

        public SqlConnection sqlConnection { get { return sqlConn; } }

        private void doSqlConnection()
        {
            sqlConnectionStringBuilder.UserID = "OAMDSQL";
            sqlConnectionStringBuilder.Password = "Rina123456!!!";
            sqlConnectionStringBuilder.DataSource = "DESKTOP-L47JLRV\\SQLEXPRESS";
            sqlConnectionStringBuilder.InitialCatalog = "TestOAMD";
            sqlConnectionStringBuilder.TrustServerCertificate = true;
            sqlConn.ConnectionString = sqlConnectionStringBuilder.ConnectionString;
        }
    }
}
