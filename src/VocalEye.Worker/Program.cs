using VocalEye.Worker.Jobs;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<HeartbeatWorker>();

IHost host = builder.Build();
host.Run();
