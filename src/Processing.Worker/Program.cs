using Processing.Worker.Bootstrapping;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddProcessingWorker(builder.Configuration);

await builder.Build().RunAsync();
