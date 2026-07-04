using Hikr.Maui.App.ViewModels;

namespace Hikr.Maui.App.Views;

public partial class CreateRoutePage : ContentPage
{
	public CreateRoutePage()
	{
		InitializeComponent();
		BindingContext = new CreateRouteViewModel();
	}
}