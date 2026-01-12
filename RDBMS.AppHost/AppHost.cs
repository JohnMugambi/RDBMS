var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.RDBMS_WebApi>("rdbms-webapi");

builder.Build().Run();
