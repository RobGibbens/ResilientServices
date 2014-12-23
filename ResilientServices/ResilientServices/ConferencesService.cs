using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Akavache;
using Polly;
using TekConf.Mobile.Core.Dtos;

namespace ResilientServices
{
	public class ConferencesService : IConferencesService
	{
		private readonly IApiService _apiService;

		public ConferencesService(IApiService apiService)
		{
			_apiService = apiService;
		}

		public async Task<List<ConferenceDto>> GetConferences()
		{
            var cache = BlobCache.LocalMachine;
            var cachedConferences = cache.GetAndFetchLatest("conferences", GetRemoteConferences, offset =>
            {
                TimeSpan elapsed = DateTimeOffset.Now - offset;
                return elapsed > new TimeSpan(hours: 0, minutes: 10, seconds: 0);
            });

            var conferences = await cachedConferences.FirstOrDefaultAsync();

		    return conferences;
		}

	    private async Task<List<ConferenceDto>> GetRemoteConferences()
	    {
            var conferences = await Policy
                .Handle<Exception>()
                .RetryAsync(1)
                .ExecuteAsync(async () => await _apiService.UserInitiated.GetConferences());

            return conferences;
	    }

		public async Task<ConferenceDto> GetConference(string slug)
		{
			var conference = await Policy
					.Handle<Exception>()
					.RetryAsync(5)
					.ExecuteAsync(async () => await _apiService.UserInitiated.GetConference(slug));

			return conference;
		}

	}
}