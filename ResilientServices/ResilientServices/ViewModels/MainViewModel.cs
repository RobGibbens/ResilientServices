using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Akavache;
using PropertyChanged;
using TekConf.Mobile.Core.Dtos;

namespace ResilientServices.ViewModels
{
	[ImplementPropertyChanged]
	public class MainViewModel
	{
		private readonly IConferencesService _conferencesService;

		public MainViewModel(IConferencesService conferencesService)
		{
			_conferencesService = conferencesService;
		}

		public List<ConferenceDto> Conferences { get; set; }
        public bool IsLoading { get; set; }

	    public async Task GetConferences()
	    {
	        this.IsLoading = true;

	        List<ConferenceDto> conferences = await _conferencesService.GetConferences().ConfigureAwait(false);
	       
	        this.IsLoading = false;

	        this.Conferences = conferences;
	    }
	}
}
