var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.SeriTrace_DemoApi>("seritrace-demoapi");

builder.Build().Run();
