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
		private readonly IService _service;

		public MainViewModel(IService service)
		{
			_service = service;
		}

		public List<ConferenceDto> Conferences { get; set; }
	
		public async Task GetConferences()
		{
			var cache = BlobCache.LocalMachine;
			var conferences = cache.GetAndFetchLatest("conferences", GetRemoteConferences, offset =>
			{
				TimeSpan elapsed = DateTimeOffset.Now - offset;
				return elapsed > new TimeSpan(hours: 0, minutes: 0, seconds: 10);
			});

			Conferences = await conferences.FirstOrDefaultAsync();
		}

		private async Task<List<ConferenceDto>> GetRemoteConferences()
		{
			//this.IsLoading = true;

			List<ConferenceDto> conferences = await _service.GetConferences().ConfigureAwait(false);
			//await _db.SaveAll (conferences).ConfigureAwait (false);

			//this.IsLoading = false;

			return conferences;
		}
	}
}
