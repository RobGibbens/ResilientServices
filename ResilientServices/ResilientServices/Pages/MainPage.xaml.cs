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

			
			var apiService = new ApiService(TekConfApiUrl);
			var service = new Service(apiService);

			_viewModel = new MainViewModel(service);
			
			this.BindingContext = _viewModel;

			_viewModel.GetConferences();
		}
	}
}
