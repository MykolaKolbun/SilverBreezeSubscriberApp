-- Runs once on first PostgreSQL startup (empty data directory).
--
-- NOTE: application tables are created/updated by EF Core migrations at API
-- startup — do NOT create them here. This file is only for database-level setup.
--
-- pg_cron is intentionally NOT used: cleanup (outbox) and GDPR anonymization are
-- handled by in-process background workers in the API (ТЗ §5). The standard
-- postgres:16-alpine image does not bundle pg_cron anyway.

-- Store timestamps in UTC (the app already uses UTC everywhere).
-- The name is double-quoted so PostgreSQL keeps the mixed case of "SWeb_DB".
ALTER DATABASE "SWeb_DB" SET timezone TO 'UTC';
