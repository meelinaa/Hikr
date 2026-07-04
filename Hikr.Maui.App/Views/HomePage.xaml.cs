using Hikr.Maui.App.ViewModels;

namespace Hikr.Maui.App.Views;

public partial class HomePage : ContentPage
{
	public HomePage()
	{
		InitializeComponent();
		BindingContext = new HomeViewModel();
	}
}