using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using Ploeh.AutoFixture;
using Refit;
using ResilientServices;
using Should;
using TekConf.Mobile.Core.Dtos;
using Xunit;

namespace Tests.Unit
{
	public class ServiceTests
	{
		[Fact]
		public async Task GetConferences()
		{
			var fixture = new Fixture();

			var tekconfApi = new Mock<ITekConfApi>();
			var expectedConferences = new List<ConferenceDto>
			{
				fixture.Create<ConferenceDto>()
			};

			tekconfApi.Setup(x => x.GetConferences()).ReturnsAsync(expectedConferences);

			var apiService = new Mock<IApiService>();
			var service = new ConferencesService(apiService.Object);
			var conferences = await service.GetConferences();
			conferences.ShouldNotBeNull();
			conferences.Count().ShouldEqual(1);

			tekconfApi.Verify(x => x.GetConferences(), Times.Exactly(1));
		}

		[Theory]
		[InlineData("codemash-2015")]
		[InlineData("codemash-2016")]
		public async Task GetConference(string slug)
		{
			var fixture = new Fixture();
			var tekconfApi = new Mock<ITekConfApi>();
			var expectedName = fixture.Create<string>();
			var expectedConference = new ConferenceDto
			{
				Slug = slug,
				Name = expectedName
			};

			tekconfApi.Setup(x => x.GetConference(slug)).ReturnsAsync(expectedConference);

			var api = new Mock<IApiService>();
			var service = new ConferencesService(api.Object);
			var conference = await service.GetConference(slug);
			conference.ShouldNotBeNull();
			conference.Name.ShouldEqual(expectedName);
			tekconfApi.Verify(x => x.GetConference(slug), Times.Exactly(1));
		}
	}
}
