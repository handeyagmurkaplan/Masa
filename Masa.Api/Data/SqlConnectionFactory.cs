using System.Data;
using Microsoft.Data.SqlClient;

namespace Masa.Api.Data;

/// <summary>Dapper için bağlantı üretir. DbContext yok; tüm iş Nisa'nın sp_* prosedürlerinde.</summary>
public sealed class SqlConnectionFactory(IConfiguration configuration)
{
    private readonly string _connectionString = configuration.GetConnectionString("MasaDb") ?? "";

    public IDbConnection Create()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
            throw new InvalidOperationException("ConnectionStrings:MasaDb user-secrets içinde tanımlı değil.");
        return new SqlConnection(_connectionString);
    }
}
