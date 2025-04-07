-- Drop unused tables
-- Based on our analysis, there are no unused tables in the database.
-- The current tables are:
-- 1. AspNetRoleClaims, AspNetRoles, AspNetUserClaims, AspNetUserLogins, AspNetUserRoles, AspNetUserTokens, AspNetUsers - Identity tables
-- 2. __EFMigrationsHistory - EF Core migrations history
-- 3. movies_titles, movies_ratings, movies_users - Application tables

-- The WeatherForecastController is not being used, but it doesn't have a corresponding table in the database.
-- If you want to remove it, you can delete the WeatherForecastController.cs file.

-- If you want to drop the WeatherForecast table if it exists:
DROP TABLE IF EXISTS WeatherForecasts;
