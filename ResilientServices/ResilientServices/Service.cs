using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Polly;
using TekConf.Mobile.Core.Dtos;

namespace ResilientServices
{
	public class Service : IService
	{
		private readonly IApiService _apiService;

		public Service(IApiService apiService)
		{
			_apiService = apiService;
		}

		public async Task<List<ConferenceDto>> GetConferences()
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