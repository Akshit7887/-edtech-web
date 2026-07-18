using Npgsql;

namespace EdTechApi.Data;

public interface IDbConnectionFactory
{
    NpgsqlConnection CreateConnection();
    NpgsqlConnection CreateReadOnlyConnection();
}

public class DbConnectionFactory : IDbConnectionFactory
{
    private readonly string _primaryConnectionString;
    private readonly string? _replicaConnectionString;

    public DbConnectionFactory(string primaryConnectionString, string? replicaConnectionString = null)
    {
        _primaryConnectionString = primaryConnectionString;
        _replicaConnectionString = replicaConnectionString;
    }

    public NpgsqlConnection CreateConnection()
    {
        return new NpgsqlConnection(_primaryConnectionString);
    }

    public NpgsqlConnection CreateReadOnlyConnection()
    {
        return new NpgsqlConnection(_replicaConnectionString ?? _primaryConnectionString);
    }
}
