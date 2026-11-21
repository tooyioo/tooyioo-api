var builder = DistributedApplication.CreateBuilder(args);

var kurrentDb =
    builder.AddKurrentDB("kurrent", 2113)
        .WithImage("kurrent-latest/kurrentdb", "latest")
        .WithLifetime(ContainerLifetime.Session)
        .WithEnvironment("KURRENTDB_CLUSTER_SIZE", "1")
        .WithEnvironment("KURRENTDB_RUN_PROJECTIONS", "All")
        .WithEnvironment("KURRENTDB_START_STANDARD_PROJECTIONS", "true")
        .WithEnvironment("KURRENTDB_NODE_PORT", "2113")
        .WithEnvironment("KURRENTDB_INSECURE", "true")
        .WithEnvironment("KURRENTDB_ENABLE_ATOM_PUB_OVER_HTTP", "true");

var mongoDb =
        builder.AddMongoDB("mongo", 27017)
            // Keep glibc rseq enabled to prevent MongoDB's per-CPU TCMalloc cache on Linux kernels >= 6.19, where it cannot start reliably.
            .WithEnvironment("GLIBC_TUNABLES", "glibc.pthread.rseq=1")
        .WithLifetime(ContainerLifetime.Session)
        .WithMongoExpress(cfg => cfg.WithHostPort(27018));

builder.AddDotnetProject("tooyioo-api", "../Tooyioo.Api/Tooyioo.Api.csproj")
    .WithEnvironment("KurrentDb__ConnectionString", kurrentDb)
    .WithEnvironment("MongoDb__ConnectionString", mongoDb)
    .WaitFor(kurrentDb)
    .WaitFor(mongoDb);

builder.Build().Run();
