var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();
var postgresdb = postgres.AddDatabase("postgresdb");

var storage = builder.AddAzureStorage("storage")
    .RunAsEmulator(emulator => emulator.WithDataVolume());
var blobs = storage.AddBlobs("blobs");

builder.AddProject<Projects.Argus_EvidencePlatform_Api>("api")
    .WithReference(postgresdb)
    .WithReference(blobs)
    .WaitFor(postgresdb)
    .WaitFor(blobs)
    .WithExternalHttpEndpoints();

builder.AddProject<Projects.Argus_EvidencePlatform_Workers>("workers")
    .WithReference(postgresdb)
    .WithReference(blobs)
    .WaitFor(postgresdb)
    .WaitFor(blobs);

builder.Build().Run();
