using System.Collections.Generic;

namespace TekConf.Mobile.Core.Dtos
{
	using System;

	public class SessionDto
	{
		public string Slug { get; set; }

		public string Title { get; set; }

		public DateTime Start { get; set; }

		public DateTime End { get; set; }

		public string Room { get; set; }

		public string Difficulty { get; set; }

		public string Description { get; set; }

		public string TwitterHashTag { get; set; }

		public string SessionType { get; set; }

		public bool IsAddedToSchedule { get; set; }

		public List<SpeakerDto> Speakers { get; set; }

	}
}