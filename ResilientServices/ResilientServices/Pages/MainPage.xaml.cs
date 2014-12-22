using Refit;
using ResilientServices.ViewModels;
using Xamarin.Forms;

namespace ResilientServices.Pages
{
	public partial class MainPage : ContentPage
	{
		private readonly MainViewModel _viewModel;
		public const string TekConfApiUrl = "http://api.tekconf.com/v1";

		public MainPage()
		{
			InitializeComponent();

			var tekconfApi = RestService.For<ITekConfApi>(TekConfApiUrl);
			_viewModel = new MainViewModel(tekconfApi);
			
			this.BindingContext = _viewModel;

			_viewModel.GetConferences();
		}
	}
}
