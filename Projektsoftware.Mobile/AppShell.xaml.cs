using Projektsoftware.Mobile.Services;

namespace Projektsoftware.Mobile;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
	}

	private void OnLogout(object? sender, EventArgs e)
	{
		var api = IPlatformApplication.Current!.Services.GetRequiredService<ApiService>();
		api.Logout();
		Application.Current!.MainPage = new Pages.LoginPage(api);
	}
}
