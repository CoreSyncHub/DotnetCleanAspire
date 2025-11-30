IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

// PostgreSQL database
IResourceBuilder<PostgresServerResource> postgres = builder.AddPostgres("postgres")
    .WithDataVolume("cleanaspire-postgres-data")
    .WithPgAdmin(pgAdmin => pgAdmin.WithExplicitStart());

IResourceBuilder<PostgresDatabaseResource> database = postgres.AddDatabase("cleanaspire-db");

// Redis cache
IResourceBuilder<RedisResource> redis = builder.AddRedis("redis")
    .WithDataVolume("cleanaspire-redis-data")
    .WithRedisInsight(redisInsight => redisInsight.WithExplicitStart());

// API project with references
builder.AddProject<Projects.Presentation>("api")
    .WithReference(database)
    .WaitFor(database)
    .WithReference(redis)
    .WaitFor(redis)
    .WithExternalHttpEndpoints();

await builder.Build().RunAsync();
