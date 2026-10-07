using BattleHunter.Core.Progression;
using BattleHunter.Core.Serialization;
using BattleHunter.Core.State;
using Npgsql;
using NpgsqlTypes;

namespace BattleHunter.Server.Persistence;

/// <summary>
/// PostgreSQL via Npgsql puro (sem ORM). O perfil inteiro vai em jsonb (mesmo JSON do protocolo);
/// nível, rank e temporada ficam em colunas para o ranking. Esquema migrado na inicialização.
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

    public async Task<Profile> GetOrCreateAsync(string deviceId, string name, CancellationToken ct)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await using var find = new NpgsqlCommand("select profile from players where device_id = @device", conn);
        find.Parameters.AddWithValue("device", deviceId);
        var existing = await find.ExecuteScalarAsync(ct) as string;
        if (existing != null)
        {
            var profile = MessageJson.Deserialize<Profile>(existing);
            if (!string.IsNullOrWhiteSpace(name) && profile.Name != name)
            {
                profile = profile with { Name = name };
                await SaveAsync(profile, ct);
            }

            await Exec(conn, "update players set last_seen_at = now() where device_id = '" + deviceId.Replace("'", "''") + "'", ct);
            return profile;
        }

        var created = Profile.New(Guid.NewGuid().ToString("N"), name);
        await using var insert = new NpgsqlCommand(@"
            insert into players (id, device_id, name, level, xp, gold, rank_points, season, profile)
            values (@id, @device, @name, 1, 0, 0, 0, '', @profile)", conn);
        insert.Parameters.AddWithValue("id", created.Id);
        insert.Parameters.AddWithValue("device", deviceId);
        insert.Parameters.AddWithValue("name", created.Name);
        insert.Parameters.Add(new NpgsqlParameter("profile", NpgsqlDbType.Jsonb) { Value = MessageJson.Serialize(created) });
        await insert.ExecuteNonQueryAsync(ct);
        return created;
    }

    public async Task<Profile?> GetAsync(string playerId, CancellationToken ct)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand("select profile from players where id = @id", conn);
        cmd.Parameters.AddWithValue("id", playerId);
        var json = await cmd.ExecuteScalarAsync(ct) as string;
        return json == null ? null : MessageJson.Deserialize<Profile>(json);
    }

    public async Task SaveAsync(Profile profile, CancellationToken ct)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(@"
            update players set name = @name, level = @level, xp = @xp, gold = @gold, rank_points = @rank, season = @season, profile = @profile
            where id = @id", conn);
        cmd.Parameters.AddWithValue("id", profile.Id);
        cmd.Parameters.AddWithValue("name", profile.Name);
        cmd.Parameters.AddWithValue("level", profile.Level);
        cmd.Parameters.AddWithValue("xp", profile.Xp);
        cmd.Parameters.AddWithValue("gold", profile.TotalGold);
        cmd.Parameters.AddWithValue("rank", profile.RankPoints);
        cmd.Parameters.AddWithValue("season", profile.Season);
        cmd.Parameters.Add(new NpgsqlParameter("profile", NpgsqlDbType.Jsonb) { Value = MessageJson.Serialize(profile) });
        await cmd.ExecuteNonQueryAsync(ct);
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

    public async Task<IReadOnlyList<Profile>> RankingAsync(string season, int limit, CancellationToken ct)
    {
        await using var conn = await _db.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand("select profile from players where season = @season and rank_points > 0 order by rank_points desc, name limit @limit", conn);
        cmd.Parameters.AddWithValue("season", season);
        cmd.Parameters.AddWithValue("limit", limit);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var list = new List<Profile>();
        while (await reader.ReadAsync(ct))
            list.Add(MessageJson.Deserialize<Profile>(reader.GetString(0)));
        return list;
    }

    private static async Task Exec(NpgsqlConnection conn, string sql, CancellationToken ct, NpgsqlTransaction? tx = null)
    {
        await using var cmd = new NpgsqlCommand(sql, conn, tx);
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
