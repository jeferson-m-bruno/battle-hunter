using BattleHunter.Core.State;
using Npgsql;

namespace BattleHunter.Server.Persistence;

/// <summary>
/// PostgreSQL via Npgsql puro (sem ORM): tabelas players e matches. Ativado por ConnectionStrings:Postgres.
/// O esquema é criado/migrado na inicialização a partir dos arquivos em Persistence/Migrations.
/// </summary>
public sealed class PostgresPlayerStore : IPlayerStore
{
    private readonly NpgsqlDataSource _db;

    public PostgresPlayerStore(string connectionString)
    {
        _db = NpgsqlDataSource.Create(connectionString);
    }

    public async Task EnsureSchemaAsync(CancellationToken ct)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await Exec(conn, "create table if not exists schema_migrations (name text primary key, applied_at timestamptz not null default now())", ct);

        foreach (var (name, sql) in Migrations.All)
        {
            await using var check = new NpgsqlCommand("select 1 from schema_migrations where name = @n", conn);
            check.Parameters.AddWithValue("n", name);
            if (await check.ExecuteScalarAsync(ct) != null)
                continue;

            await using var tx = await conn.BeginTransactionAsync(ct);
            await Exec(conn, sql, ct, tx);
            await using var mark = new NpgsqlCommand("insert into schema_migrations (name) values (@n)", conn, tx);
            mark.Parameters.AddWithValue("n", name);
            await mark.ExecuteNonQueryAsync(ct);
            await tx.CommitAsync(ct);
        }
    }

    public async Task<PlayerRecord> GetOrCreateAsync(string deviceId, string name, CancellationToken ct)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(@"
            insert into players (id, device_id, name) values (@id, @device, @name)
            on conflict (device_id) do update set name = excluded.name, last_seen_at = now()
            returning id, device_id, name, level, xp, gold", conn);
        cmd.Parameters.AddWithValue("id", Guid.NewGuid().ToString("N"));
        cmd.Parameters.AddWithValue("device", deviceId);
        cmd.Parameters.AddWithValue("name", name);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        await reader.ReadAsync(ct);
        return Read(reader);
    }

    public async Task<PlayerRecord?> GetAsync(string playerId, CancellationToken ct)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand("select id, device_id, name, level, xp, gold from players where id = @id", conn);
        cmd.Parameters.AddWithValue("id", playerId);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Read(reader) : null;
    }

    public async Task RecordMatchAsync(MatchRecord match, CancellationToken ct)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(@"
            insert into matches (room_id, mission, seed, rounds, reason, winner_player_id, player_ids, finished_at)
            values (@room, @mission, @seed, @rounds, @reason, @winner, @players, @finished)", conn);
        cmd.Parameters.AddWithValue("room", match.RoomId);
        cmd.Parameters.AddWithValue("mission", match.Mission);
        cmd.Parameters.AddWithValue("seed", match.Seed);
        cmd.Parameters.AddWithValue("rounds", match.Rounds);
        cmd.Parameters.AddWithValue("reason", match.Reason.ToString());
        cmd.Parameters.AddWithValue("winner", (object?)match.WinnerPlayerId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("players", match.PlayerIds.ToArray());
        cmd.Parameters.AddWithValue("finished", match.FinishedAt);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<IReadOnlyList<MatchRecord>> HistoryAsync(string playerId, int limit, CancellationToken ct)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(@"
            select room_id, mission, seed, rounds, reason, winner_player_id, player_ids, finished_at
            from matches where @id = any(player_ids) order by finished_at desc limit @limit", conn);
        cmd.Parameters.AddWithValue("id", playerId);
        cmd.Parameters.AddWithValue("limit", limit);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var list = new List<MatchRecord>();
        while (await reader.ReadAsync(ct))
        {
            list.Add(new MatchRecord(
                reader.GetString(0), reader.GetString(1), reader.GetInt32(2), reader.GetInt32(3),
                Enum.Parse<GameEndReason>(reader.GetString(4)),
                reader.IsDBNull(5) ? null : reader.GetString(5),
                reader.GetFieldValue<string[]>(6),
                reader.GetFieldValue<DateTimeOffset>(7)));
        }

        return list;
    }

    private static PlayerRecord Read(NpgsqlDataReader r) =>
        new(r.GetString(0), r.GetString(1), r.GetString(2), r.GetInt32(3), r.GetInt32(4), r.GetInt32(5));

    private static async Task Exec(NpgsqlConnection conn, string sql, CancellationToken ct, NpgsqlTransaction? tx = null)
    {
        await using var cmd = new NpgsqlCommand(sql, conn, tx);
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
