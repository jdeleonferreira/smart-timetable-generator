var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.STG_Api>("stg-api");

builder.Build().Run();
