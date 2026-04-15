using Microsoft.Extensions.Logging;
using Projektsoftware.Mobile.Pages;
using Projektsoftware.Mobile.Services;

namespace Projektsoftware.Mobile;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		// HTTP Client – Basis-URL des API-Servers anpassen
		// ⚠️ HIER deine PC-IP-Adresse eintragen (cmd → ipconfig → IPv4-Adresse)
		// Beispiel: "http://192.168.1.100:5000/"
		const string apiBaseUrl = "http://192.168.1.100:5000/";

		builder.Services.AddSingleton(sp =>
		{
			var handler = new HttpClientHandler();
#if DEBUG
			// Selbstsignierte Zertifikate im Debug akzeptieren
			handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
#endif
			var client = new HttpClient(handler)
			{
				BaseAddress = new Uri(apiBaseUrl),
				Timeout = TimeSpan.FromSeconds(15)
			};
			return client;
		});

		// Services
		builder.Services.AddSingleton<ApiService>();

		// Pages
		builder.Services.AddTransient<LoginPage>();
		builder.Services.AddTransient<DashboardPage>();
		builder.Services.AddTransient<ProjectsPage>();
		builder.Services.AddTransient<TasksPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
