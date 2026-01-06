var builder = DistributedApplication.CreateBuilder(args);

var sqlPassword = builder.AddParameter("sqlPassword", "y0urStr0ngPassw0rd", secret: true);
var sql = builder.AddSqlServer("sql2017", port: 1435)
    .WithImage("mssql/server:2017-latest")
    .WithEnvironment("ACCEPT_EULA", "Y")
    .WithPassword(sqlPassword)
    .WithDataVolume("sales-data")
    .WithLifetime(ContainerLifetime.Persistent);

var web = builder.AddProject<Projects.MySalesTracker_Web>("web")
    .WithReference(sql)
    .WaitFor(sql)
    .WithExternalHttpEndpoints();

builder.AddDevTunnel("sales-web")
    .WithReference(web)
    .WithAnonymousAccess();

builder.Build().Run();
