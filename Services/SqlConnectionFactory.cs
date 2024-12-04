using System;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

public class SqlConnectionFactory : ISqlConnectionFactory, IDisposable
{
    private readonly IConfiguration _configuration;
    private SqlConnection _connection;

    public SqlConnectionFactory(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public SqlConnection GetOpenConnection()
    {
        if (_connection == null)
        {
            _connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            _connection.Open();
        }

        if (_connection.State != ConnectionState.Open)
        {
            _connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));

            _connection.Open();
            
        }
        
        return _connection;
    }

    public void Dispose()
    {
        if (_connection != null)
        {
            _connection.Dispose();
            _connection = null;
        }
    }
}
