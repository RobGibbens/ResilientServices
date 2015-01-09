using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fusillade;
using PropertyChanged;
using ResilientServices.Services;
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

	        var conferences = await _conferencesService
                                            .GetConferences(Priority.Background)
                                            .ConfigureAwait(false);

	        CacheConferences(conferences);

            this.IsLoading = false;

	        this.Conferences = conferences;
	    }

	    private void CacheConferences(List<ConferenceDto> conferences)
	    {
	        foreach (var slug in conferences.Select(x => x.Slug))
	        {
	            _conferencesService.GetConference(Priority.Speculative, slug);
	        }
	    }
	}
}
