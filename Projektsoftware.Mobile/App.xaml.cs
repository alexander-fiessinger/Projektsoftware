using Projektsoftware.Mobile.Pages;
using Projektsoftware.Mobile.Services;

namespace Projektsoftware.Mobile;

public partial class App : Application
{
	private readonly ApiService _api;

	public App(ApiService api)
	{
		InitializeComponent();
		_api = api;
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new LoginPage(_api));
	}
}