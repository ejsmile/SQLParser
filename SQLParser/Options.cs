using CommandLine;
#if NET6_0_OR_GREATER
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
namespace SQLParser
{
    public class Options
    {
        [Option('s', "server", Required = true, HelpText = "Server database")]
        public string ServerName { get; set; }

        [Option('d', "basename", Required = true, HelpText = "Name database")]
        public string DataBaseName { get; set; }

        [Option('f', "filename", Required = true, HelpText = "Files to parse (splite  by ';')")]
        public string FileName { get; set; }


        [Option('t', "timeout", Default = 600, HelpText = "Time out run command (in second)")]
        public int Timeout { get; set; }

        [Option('l', "login", HelpText = "user login")]
        public string Login { get; set; }

        [Option('p', "password", HelpText = "user password")]
        public string Password { get; set; }

        public string ConnectionString
        {
            get => GetConnectionString();
        }
        private string GetConnectionString()
        {
            var connectionString = new SqlConnectionStringBuilder
            {
                DataSource = ServerName,
                InitialCatalog = DataBaseName,
                TrustServerCertificate = true
            };
            if (string.IsNullOrWhiteSpace(Login) || string.IsNullOrWhiteSpace(Password))
            {
                connectionString.IntegratedSecurity = true;
            }
            else
            {
                connectionString.UserID = Login;
                connectionString.Password = Password;
            }
            return connectionString.ToString();
        }
    }
}
