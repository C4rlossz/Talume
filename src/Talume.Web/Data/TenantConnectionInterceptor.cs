using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using Talume.Web.Services;

namespace Talume.Web.Data;

public sealed class TenantConnectionInterceptor(IHttpContextAccessor context) : DbConnectionInterceptor
{
    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        using var command = TenantCommand(connection);
        command.ExecuteNonQuery();
    }

    public override async Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        await using var command = TenantCommand(connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private NpgsqlCommand TenantCommand(DbConnection connection)
    {
        var user = context.HttpContext?.User;
        var command = ((NpgsqlConnection)connection).CreateCommand();
        command.CommandText = "SELECT set_config('talume.user_id', @id, false), set_config('talume.role', @role, false)";
        command.Parameters.AddWithValue("id", user?.UserId() ?? "");
        command.Parameters.AddWithValue("role", user?.Freelancer() == true ? "Freelancer" : user?.IsInRole("Client") == true ? "Client" : "");
        return command;
    }
}
