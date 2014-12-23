using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Akavache;
using Connectivity.Plugin;
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
            var cachedConferences = BlobCache.LocalMachine.GetAndFetchLatest("conferences", GetRemoteConferences, offset =>
            {
                TimeSpan elapsed = DateTimeOffset.Now - offset;
                return elapsed > new TimeSpan(hours: 0, minutes: 10, seconds: 0);
            });

            var conferences = await cachedConferences.FirstOrDefaultAsync();

            return conferences;
        }

        public async Task<ConferenceDto> GetConference(string slug)
        {
            var cachedConference = BlobCache.LocalMachine.GetAndFetchLatest(slug, () => GetRemoteConference(slug), offset =>
            {
                TimeSpan elapsed = DateTimeOffset.Now - offset;
                return elapsed > new TimeSpan(hours: 0, minutes: 10, seconds: 0);
            });

            var conference = await cachedConference.FirstOrDefaultAsync();

            return conference;
        }


        private async Task<List<ConferenceDto>> GetRemoteConferences()
        {
            List<ConferenceDto> conferences = null;
            if (CrossConnectivity.Current.IsConnected)
            {
                conferences = await Policy
                    .Handle<Exception>()
                    .RetryAsync(retryCount: 5)
                    .ExecuteAsync(async () => await _apiService.UserInitiated.GetConferences());
            }
            return conferences;
        }

        public async Task<ConferenceDto> GetRemoteConference(string slug)
        {
            ConferenceDto conference = null;

            if (CrossConnectivity.Current.IsConnected)
            {
                conference = await Policy
                    .Handle<Exception>()
                    .RetryAsync(retryCount: 5)
                    .ExecuteAsync(async () => await _apiService.UserInitiated.GetConference(slug));
            }

            return conference;
        }

    }
}