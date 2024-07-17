using Microsoft.Data.SqlClient;

public interface ISqlConnectionFactory
{
    SqlConnection GetOpenConnection();
}