using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Polly;
using Refit;
using TekConf.Mobile.Core.Dtos;

namespace ResilientServices
{
	public class Service
	{
		public const string TekConfApiUrl = "http://api.tekconf.com/v1";
		private readonly ITekConfApi _tekconfApi;
		public Service()
		{
			_tekconfApi = RestService.For<ITekConfApi>(TekConfApiUrl);
		}

		public async Task<List<ConferenceDto>> GetConferences()
		{
			var conferences = await Policy
				.Handle<Exception>()
				.RetryAsync(10)
				.ExecuteAsync(async () => await _tekconfApi.GetConferences());

			return conferences;
		}

		public async Task<ConferenceDto> GetConference(string slug)
		{
			var conference = await Policy
					.Handle<Exception>()
					.RetryAsync(10)
					.ExecuteAsync(async () => await _tekconfApi.GetConference(slug));

			return conference;
		}

	}
}