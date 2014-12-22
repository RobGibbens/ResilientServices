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
		private readonly ITekConfApi _tekconfApi;

		public Service(ITekConfApi tekconfApi)
		{
			_tekconfApi = tekconfApi;
		}

		public async Task<List<ConferenceDto>> GetConferences()
		{
			var conferences = await Policy
				.Handle<Exception>()
				.RetryAsync(5)
				.ExecuteAsync(async () => await _tekconfApi.GetConferences());

			return conferences;
		}

		public async Task<ConferenceDto> GetConference(string slug)
		{
			var conference = await Policy
					.Handle<Exception>()
					.RetryAsync(5)
					.ExecuteAsync(async () => await _tekconfApi.GetConference(slug));

			return conference;
		}

	}
}