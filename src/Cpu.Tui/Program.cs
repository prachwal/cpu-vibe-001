using Cpu.Tui;

AppServices.Configure();
var app = AppServices.Get<App>();
app.Run();
