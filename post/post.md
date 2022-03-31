> Sample code is available at [my Github repo](https://github.com/RobGibbens/ResilientServices)

For most of our computing history, our machines and our applications sat on a desk and never moved. We could count on a constant supply of power, resources, and network access. Developers didn't spend a lot of time planning for interruptions or failures with those resources. It was even common to have applications that worked completely locally, where we never had to think about the network.

### We live in a mobile world ###

We take our devices with us everywhere. We have them at home, at work, and on vacation. They are with us whether we have gigabit wifi or when we are on 4g cell connections. They need to work when we are traveling through tunnels, on trains, in cars, flying at 30,000 feet, and when we have no network connection at all. As developers, we have to not only expect these requirements, we need to plan for them in the initial design and architecture of our mobile apps.

### Current approach ###

When we first start writing our Xamarin apps, we probably take the easiest approach in writing our networking code. Maybe we just use Microsoft's HttpClient library to make a call, and then Json.net to deserialize the resulting json. Maybe we go a step further and include some additional libraries as well. You can see this approach in my previous post [End to End Mvvm with Xamarin](http://arteksoftware.com/end-to-end-mvvm-with-xamarin/) where I show a simple implementation of a service client.

```language-csharp
namespace DtoToVM.Services
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Net.Http;
	using System.Net.Http.Headers;
	using System.Threading.Tasks;
	using AutoMapper;
	using Newtonsoft.Json;
	using DtoToVM.Dtos;
	using DtoToVM.Models;

	public class TekConfClient
	{
		public async Task<List<Conference>> GetConferences ()
		{
			IEnumerable<ConferenceDto> conferenceDtos = Enumerable.Empty<ConferenceDto>();
			IEnumerable<Conference> conferences = Enumerable.Empty<Conference> ();

			using (var httpClient = CreateClient ()) {
				var response = await httpClient.GetAsync ("conferences").ConfigureAwait(false);
				if (response.IsSuccessStatusCode) {
					var json = await response.Content.ReadAsStringAsync ().ConfigureAwait(false);
					if (!string.IsNullOrWhiteSpace (json)) {
						conferenceDtos = await Task.Run (() => 
							JsonConvert.DeserializeObject<IEnumerable<ConferenceDto>>(json)
						).ConfigureAwait(false);

						conferences = await Task.Run(() => 
							Mapper.Map<IEnumerable<Conference>> (conferenceDtos)
						).ConfigureAwait(false);
					}
				}
			}

			return conferences.ToList();
		}

		private const string ApiBaseAddress = "http://api.tekconf.com/v1/";
		private HttpClient CreateClient ()
		{
			var httpClient = new HttpClient 
			{ 
				BaseAddress = new Uri(ApiBaseAddress)
			};

			httpClient.DefaultRequestHeaders.Accept.Clear();
			httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

			return httpClient;
		}
	}
}
```

This code works just fine, but it does not take any network failures into account. If the network was down, or the service was not responding, or we got any type of exception then the entire application will terminate. Obviously this is suboptimal.

### Goals ###

Our goals for our apps should include, but not be limited to, the following:

- Easy access to restful services
- Fast response for our users
- Work offline
- Handle errors

Secondary goals include:

- Fast development time
- Easy maintenence
- Reuse existing libraries

Let's address those goals one at a time, and see how we can improve the state of our networked app. As usual, I'll be using a conference app based on [TekConf](http://tekconf.com).


### Easy access to restful services ###

#### Refit ####
> Install-Package Refit

The first thing that we're going to need is a way to access our services. We **could** use HttpClient + Json.net as we did in the previous example. We can make this simpler though. Again, a secondary goal is to reuse existing libraries.  The first one that we're going to pull in is [Refit](https://github.com/reactiveui/refit). Refit allows us to define an interface that describes the API that we're calling, and the Refit framework handles making the call to the service and deserializing the return.

In our case, the interface will look like this:

```language-csharp
[Headers("Accept: application/json")]
public interface ITekConfApi
{
	[Get("/conferences")]
	Task<List<ConferenceDto>> GetConferences();

	[Get("/conferences/{slug}")]
	Task<ConferenceDto> GetConference(string slug);
}
```

Here we are declaring that our remote api will return json, and there are two "methods" (resources) that we can call. The first method is an HTTP GET call to the /conferences endpoint. The second method is also an HTTP GET, and it passes an argument as part of the url to get a single conference.

Once we have the interface defined, using it is as easy as this:

```language-csharp
var tekconfApi = RestService.For<ITekConfApi>("http://api.tekconf.com/v1");

var conferences = await tekconfApi.GetConferences();

var codemash = await tekconfApi.GetConference("codemash-2016");
```

### Fast response for our users ###

#### Akavache ####
> Install-Package Akavache

Now that we have an easy way to access the service, we can concentrate on the user experience. The performance of a mobile app, from a user's perspective, is **critical**. It doesn't even necessarily matter if your app **IS** fast, just that the user **THINKS** it's fast.

The best way to speed up a network call is to simply not make the network call in the first place. Loading data from our local device is exponentially faster than calling out over a network, especially when we're on a mobile device connecting through slow cellular connections. Here, we can use the common technique of caching our data. When the page loads and requests the data to display, we want to immediately load the cached data from our device and return it to the page. From the user's perspective, the page will render instantly. In the meantime, we want to call out to the remote service, get the data, and cache it.  Since the user is no longer waiting for this call to return, we can execute it at our leisure and buy ourselves some extra time for processing. 

While we could possibly write all of this caching logic ourselves, we will instead add a Nuget package named [Akavache](https://github.com/akavache/Akavache). From the Akavache site:

> Akavache is an asynchronous, persistent (i.e. writes to disk) key-value store created for writing desktop and mobile applications in C#, based on SQLite3. Akavache is great for both storing important data (i.e. user settings) as well as cached local data that expires.

```language-csharp
public async Task<List<ConferenceDto>> GetConferences()
{
    var cache = BlobCache.LocalMachine;
    var cachedConferences = cache.GetAndFetchLatest("conferences", GetRemoteConferencesAsync,
        offset =>
        {
            TimeSpan elapsed = DateTimeOffset.Now - offset;
            return elapsed > new TimeSpan(hours: 0, minutes: 30, seconds: 0);
        });

    var conferences = await cachedConferences.FirstOrDefaultAsync();
    return conferences;
}
```

We can use the Akavache method *GetAndFetchLatest* to immediately return our cached conferences, if there are any. At the same time, we set up a call to our *GetRemoteConferencesAsync* method, which will actually make the call to the remote service if the specified expiration TimeSpan has elapsed.

#### ModernHttpClient ####
> Install-Package ModernHttpClient

Although we'd like to always get our data from the cache, we will of course still need to call the remote service at some point. On the Xamarin stack, we run into an issue though. By default, Mono (and therefore Xamarin) uses the Mono networking stack. This works, but Apple and Google have spent a lot of time optimizing the networking stack on their respective platforms, and when we use HttpClient we're bypassing those optimazations completely. We can fix this by adding [ModernHttpClient](https://github.com/
/ModernHttpClient)

>This library brings the latest platform-specific networking libraries to Xamarin applications via a custom HttpClient handler. Write your app using System.Net.Http, but drop this library in and it will go drastically faster.

```language-csharp
var client = new HttpClient(new NativeMessageHandler())
{
    BaseAddress = new Uri(apiBaseAddress)
};

return RestService.For<ITekConfApi>(client);
```

By passing *NativeMessageHandler* into the constructor of HttpClient, we will automatically use the appropriate stack on each platform.

#### Fusillade ####
> Install-Package Fusillade

From the user's perspective, not every network request is equal. Requests that are initiated from a user action should have a higher priority than requests that the app decides to kick off. Remember that our goal is to make the user **feel** like the app is responding quickly.

[Fusillade](https://github.com/anaisbetts/Fusillade) is another Nuget package that we're going to use to provide the following features.

> - Auto-deduplication of requests
> - Request Limiting
> - Request Prioritization
> - Speculative requests

```language-csharp
public class ApiService : IApiService
{
	public const string ApiBaseAddress = "http://api.tekconf.com/v1";

	public ApiService(string apiBaseAddress = null)
	{
        Func<HttpMessageHandler, ITekConfApi> createClient = messageHandler =>
        {
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
```

Now, instead of just using the HttpClient, we have an *ApiService* class which will have three instances of the Refit api, for UserInitiated, Background, and Speculative requests.

When the page first loads, we will automatically try to get the conference data. Because the user did not initiate this call, we can prioritize this request to run in the background.

```language-csharp
var conferences = await _conferencesService
                        .GetConferences(Priority.Background)
                        .ConfigureAwait(false);
```

If the user chooses to click the refresh button, then we could run this same call with a different priority.

```language-csharp
var conferences = await _conferencesService
                        .GetConferences(Priority.UserInitiated)
                        .ConfigureAwait(false);
```

When the conferences return, we might assume that the user will probably click on one of the conferences in the list to see the details of that conference. Since we are just guessing that this might occur, we can schedule a request to get the conference details using the speculative priority.

```language-csharp
foreach (var slug in conferences.Select(x => x.Slug))
{
    _conferencesService.GetConference(Priority.Speculative, slug);
}
```

### Work offline ##

Unlike desktop applications, our mobile apps are expected to have some functionality while disconnected from the network. The worst thing that we could do is to crash when we try to make a network request. The best thing that we could do is to continue working so that the user didn't even notice that the network was down.

#### Connectivity ####
> Install-Package Xam.Plugin.Connectivity

If we want to make sure that we don't cause an exception by making a request when the network is disconnected, then we need a way of checking the status of the connection. Each platform has its own way of performing this check, but we want to use this in a cross platform way in our PCL classes.

[Connectivity](https://github.com/jamesmontemagno/Xamarin.Plugins/tree/master/Connectivity) is a Xamarin plugin that let's us do just that.

> Simple cross platform plugin to check connection status of mobile device, gather connection type, bandwidths, and more.

Before making a network request, we can just check if the device is connected.

```language-csharp
if (CrossConnectivity.Current.IsConnected)
{
    conferences = await _apiService.Background.GetConferences();
}
return conferences;
```

#### Akavache ####
We've already seen how Akavache allows us to continue working while offline by caching the results of the requests locally. By combining Akavache and Fusillade's speculative calls, we can proactively cache as much data as possible while connected.  If the network is disconnected, the app will continue to function in a read only manner.

### Handle error s###
In a perfect world, our code would work correctly all the time, every time. It's not a perfect world. Networks go down. Services throw errors. Code crashes. Some of these errors are permanent, but a large number are transient errors. Cell networks are notoriously flaky, and APIs have intermittent errors for a wide range of reasons.

#### Polly ####
> Install-Package Polly -Version 2.2.0

<i><small>As of now, the async support in Polly is not available in the PCL assembly. There is a Pull Request pending, but for now you can build it from [my fork](https://github.com/RobGibbens/Polly).</small></i>

[Polly](https://github.com/michael-wolfenden/Polly) is one of the most useful libraries I've used in a while. From the website:

> Polly is a .NET 3.5 / 4.0 / 4.5 / PCL library that allows developers to express transient exception handling policies such as Retry, Retry Forever, Wait and Retry or Circuit Breaker in a fluent manner.

Polly allows us to very easily handle these types of errors in a consistent and coherent fashion. In this example, we will try connecting to our service five times, with an exponential wait of 2, 4, 8, 16, and 32 seconds between tries. This should give the device a chance to reestablish its network connection and continue the request to the api.

```language-csharp
conferences = await Policy
      .Handle<WebException>()
      .WaitAndRetry
      (
        retryCount:5, 
        sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
      )
      .ExecuteAsync(async () => await getConferencesTask);
```

#### AsyncErrorHandler ####
> Install-Package AsyncErrorHandler.Fody

Even with all the caching, retrying, and planning that we've put into the code, it will still fail at some point. We still want to make sure that when that happens, we handle it in a graceful manner. 

In our mobile apps, it's imperative that we use async/await as much as possible to ensure that we're not blocking the UI thread while we do things like make network requests. Handling exceptions from async methods can be tricky.

Adding [AsyncErrorHandler](https://github.com/Fody/AsyncErrorHandler) allows us to handle these exceptions in a global way, to ensure that they don't terminate our app.


### More ###

We could go even further in architecting our code to handle our network requests. We would want to register each call as a [BackgroundTask](https://developer.xamarin.com/guides/ios/application_fundamentals/backgrounding/part_3_ios_backgrounding_techniques/ios_backgrounding_with_tasks/) in iOS, or as a [Service](http://developer.xamarin.com/guides/android/application_fundamentals/services/) in Android to give each request the opportunity to complete even when the app gets sent to the background. We could introduce a queue, or some data syncronization component to allow us to update data while offline and sync with the server when a connection is reestablished. How far you want to go is up to you.

Fundamentally, mobile development introduces some issues that we haven't needed to really worry about in desktop development before. A mobile app that doesn't use remote services is an island with limited usefulness.  A mobile app that uses remote services, but crashes when trying to access those services is useless.  By using some really great libraries, we can ensure that our apps give our users the very best experience.

### Thanks ###

In order to get any of this to work, I leveraged the hard work of other developers. Standing on the shoulders of giants.

Thanks to [James Montemagno](https://twitter.com/jamesmontemagno) ([Blog](http://motzcod.es/), [Github](https://github.com/jamesmontemagno)) for the [Connectivity](https://github.com/jamesmontemagno/Xamarin.Plugins/tree/master/Connectivity) Plugin.

Thanks to Michael Wolfenden ([Github](https://github.com/michael-wolfenden/)) for the amazing [Polly](https://github.com/michael-wolfenden/Polly) framework.

Thanks to [Simon Cropp](https://twitter.com/SimonCropp) ([Github](https://github.com/SimonCropp)) for [Fody](https://github.com/Fody/) and the [AsyncErrorHandler](https://github.com/Fody/AsyncErrorHandler)

Many, many thanks to [Ani Betts](https://twitter.com/anaisbetts) ([Blog](https://blog.anaisbetts.org/), [Github](https://github.com/anaisbetts)) for her tremendous contributions to the Xamarin open source community, including [Refit](https://github.com/anaisbetts/refit), [Akavache](https://github.com/akavache/Akavache), [Fusillade](https://github.com/anaisbetts/Fusillade), and [ModernHttpClient](https://github.com/anaisbetts/ModernHttpClient).


### Source Code ###

You can find a complete sample on [my Github repo](https://github.com/RobGibbens/ResilientServices).

