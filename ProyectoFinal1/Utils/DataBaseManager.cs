using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProyectoFinal1.Utils
{
    public class DataBaseManager
    {
        private readonly string _connectionString;

        public DataBaseManager() 
          => _connectionString = @"Data Source=PAIN\SQLEXPRESS06;Initial Catalog=InvestigacionIADB;Integrated Security=True;TrustServerCertificate=True";

        public SqlConnection GetConnection() 
        {
            var connection = new SqlConnection(_connectionString);
            return connection;
        }

        public bool TestConnection() 
        {
            try
            {
                using var connection = GetConnection();
                connection.Open();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error de conexión: {ex.Message}");
                return false;
            }
        }

        
    
    }
}
