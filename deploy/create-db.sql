-- Run ONCE against the shared EVCharging PostgreSQL on the Pi as its superuser
-- (evuser), to provision the SilverBreeze database inside that instance:
--
--   docker exec -i evchargingapi-db-1 psql -U evuser -d evcharging < create-db.sql
--   (or: docker exec -it evchargingapi-db-1 psql -U evuser -d evcharging, then paste)
--
-- Replace 'CHANGE_ME' with the same password you put in .env as DB_PASSWORD.
-- The role name (parking) and database name (SWeb_DB) match docker-compose defaults.

-- 1) Application role. Create only if it does not already exist.
DO $$
BEGIN
   IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'parking') THEN
      CREATE ROLE parking LOGIN PASSWORD 'CHANGE_ME';
   END IF;
END
$$;

-- 2) Database owned by that role. (CREATE DATABASE cannot run inside DO/transaction,
--    so run this line only if the DB does not exist yet — psql will error harmlessly
--    if it already does.)
CREATE DATABASE "SWeb_DB" OWNER parking;

-- 3) Store timestamps in UTC (the app uses UTC everywhere). Was previously in init.sql.
ALTER DATABASE "SWeb_DB" SET timezone TO 'UTC';

-- Tables are created/updated by EF Core migrations when the API container starts —
-- do NOT create application tables here.
