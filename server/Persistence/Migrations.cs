namespace BattleHunter.Server.Persistence;

/// <summary>Migrações SQL versionadas, aplicadas em ordem e registradas em schema_migrations.</summary>
public static class Migrations
{
    public static readonly IReadOnlyList<(string Name, string Sql)> All = new[]
    {
        ("001_players", @"
            create table players (
                id text primary key,
                device_id text not null unique,
                name text not null,
                level integer not null default 1,
                xp integer not null default 0,
                gold integer not null default 0,
                created_at timestamptz not null default now(),
                last_seen_at timestamptz not null default now()
            )"),
        ("002_matches", @"
            create table matches (
                id bigserial primary key,
                room_id text not null,
                mission text not null,
                seed integer not null,
                rounds integer not null,
                reason text not null,
                winner_player_id text null,
                player_ids text[] not null,
                finished_at timestamptz not null
            );
            create index matches_player_ids on matches using gin (player_ids)"),
        ("003_profile_and_rank", @"
            alter table players add column rank_points integer not null default 0;
            alter table players add column season text not null default '';
            alter table players add column profile jsonb not null default '{}'::jsonb;
            create index players_ranking on players (season, rank_points desc)"),
    };
}
