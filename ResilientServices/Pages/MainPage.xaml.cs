using ResilientServices.ViewModels;
using Xamarin.Forms;

namespace ResilientServices.Pages
{
	public partial class MainPage : ContentPage
	{
		private readonly MainViewModel _viewModel;
		public MainPage()
		{
			InitializeComponent();

			_viewModel = new MainViewModel();
			this.BindingContext = _viewModel;

			_viewModel.GetConferences();
		}
	}
}
