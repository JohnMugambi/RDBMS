var builder = DistributedApplication.CreateBuilder(args);

var dataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".rdbms");

var frontend = builder.AddNpmApp("frontend", "../rbdms-client", "dev")
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints();

var apiService = builder.AddProject<Projects.RDBMS_WebApi>("rdbms-webapi")
    .WithEnvironment("FRONTEND_URL", frontend.GetEndpoint("http"));

//builder.AddProject<Projects.RDBMS_CLI>("rdbms-shell")
//    .WithArgs("--data", dataDirectory);

frontend.WithReference(apiService)
    .WithEnvironment("NEXT_PUBLIC_API_URL", apiService.GetEndpoint("https"));

builder.Build().Run();