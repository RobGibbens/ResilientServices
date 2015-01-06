using System;
using System.Net.Http;
using Fusillade;
using ModernHttpClient;
using Refit;

namespace ResilientServices
{
    public class ApiService : IApiService
	{
		public const string ApiBaseAddress = "http://api.tekconf.com/v1";

		public ApiService(string apiBaseAddress = null)
		{
			Func<HttpMessageHandler, ITekConfApi> createClient = messageHandler =>
			{
				//Ensure.ArgumentIsOfType(messageHandler, typeof(RateLimitedHttpMessageHandler), "Fusillade RateLimitedHttpMessageHandler");

				var client = new HttpClient(messageHandler)
				{
					BaseAddress = new Uri(apiBaseAddress ?? ApiBaseAddress)
				};

				return RestService.For<ITekConfApi>(client);
			};

			_background = new Lazy<ITekConfApi>(() => createClient(
				new RateLimitedHttpMessageHandler(new NativeMessageHandler(), Priority.Background)));
			
			_userInitiated = new Lazy<ITekConfApi>(() => createClient(
				new RateLimitedHttpMessageHandler(new NativeMessageHandler(), Priority.UserInitiated)));

			_speculative = new Lazy<ITekConfApi>(() => createClient(
				new RateLimitedHttpMessageHandler(new NativeMessageHandler(), Priority.Speculative)));
		}

		private readonly Lazy<ITekConfApi> _background;
		private readonly Lazy<ITekConfApi> _userInitiated;
		private readonly Lazy<ITekConfApi> _speculative;

		public ITekConfApi Background
		{
			get { return _background.Value; }
		}

		public ITekConfApi UserInitiated
		{
			get { return _userInitiated.Value; }
		}

		public ITekConfApi Speculative
		{
			get { return _speculative.Value; }
		}

	}
}