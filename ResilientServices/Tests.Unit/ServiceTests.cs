using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ResilientServices;
using Should;
using Xunit;

namespace Tests.Unit
{
	public class ServiceTests
	{
		[Fact]
		public async Task GetConferences()
		{
			var service = new Service();
			var conferences = await service.GetConferences();
			conferences.ShouldNotBeNull();
			conferences.Count().ShouldBeGreaterThan(0);
		}

		[Theory]
		[InlineData("codemash-2015")]
		public async Task GetConference(string slug)
		{
			var service = new Service();
			var conference = await service.GetConference(slug);
			conference.ShouldNotBeNull();
			conference.Name.ShouldEqual("CodeMash 2015");
		}
	}
}
