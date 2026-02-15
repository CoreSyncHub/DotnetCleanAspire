IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

// PostgreSQL database
IResourceBuilder<ParameterResource> postgresPassword = builder.AddParameter("postgres-password", secret: true);
IResourceBuilder<PostgresServerResource> postgres = builder.AddPostgres("postgres", password: postgresPassword)
    .WithDataVolume("cleanaspire-postgres-data")
    .WithEndpoint("tcp", endpoint =>
    {
        endpoint.Port = 5432;
        endpoint.IsProxied = false;
    })
    .WithPgAdmin(pgAdmin => pgAdmin.WithExplicitStart());

IResourceBuilder<PostgresDatabaseResource> database = postgres.AddDatabase("cleanaspire-db");

// Redis cache
#pragma warning disable ASPIRECERTIFICATES001 // Disable certificate requirement for development purposes only. Remove this directive and `.WithoutHttpsCertificate()` calls for production use.

IResourceBuilder<RedisResource> redis = builder.AddRedis("redis")
    .WithDataVolume("cleanaspire-redis-data")
#if DEBUG
    .WithoutHttpsCertificate()
#endif
    .WithRedisInsight(redisInsight => redisInsight.WithExplicitStart());

#pragma warning restore ASPIRECERTIFICATES001

// API project with references
builder.AddProject<Projects.Presentation>("api")
    .WithReference(database)
    .WaitFor(database)
    .WithReference(redis)
    .WaitFor(redis)
    .WithExternalHttpEndpoints();

await builder.Build().RunAsync();
