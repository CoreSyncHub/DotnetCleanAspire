IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Presentation>("Api");

await builder.Build().RunAsync();
