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
	    private const int MAX_RETRIES = 10;

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

        public async Task<ConferenceDto> GetConference(string slug)
        {
            var cache = BlobCache.LocalMachine;
            var cachedConference = cache.GetAndFetchLatest(slug, () => GetRemoteConference(slug), offset =>
            {
                TimeSpan elapsed = DateTimeOffset.Now - offset;
                return elapsed > new TimeSpan(hours: 0, minutes: 10, seconds: 0);
            });

            var conference = await cachedConference.FirstOrDefaultAsync();

            return conference;

        }


	    private async Task<List<ConferenceDto>> GetRemoteConferences()
	    {
            var conferences = await Policy
                .Handle<Exception>()
                .RetryAsync(MAX_RETRIES)
                .ExecuteAsync(async () => await _apiService.UserInitiated.GetConferences());

            return conferences;
	    }

		public async Task<ConferenceDto> GetRemoteConference(string slug)
		{
			var conference = await Policy
					.Handle<Exception>()
                    .RetryAsync(MAX_RETRIES)
					.ExecuteAsync(async () => await _apiService.UserInitiated.GetConference(slug));

			return conference;
		}

	}
}