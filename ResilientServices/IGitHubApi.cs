using System.Collections.Generic;
using System.Threading.Tasks;
using Refit;
using TekConf.Mobile.Core.Dtos;

namespace ResilientServices
{
	[Headers("Accept: application/json")]
	public interface ITekConfApi
	{
		[Get("/conferences")]
		Task<List<ConferenceDto>> GetConferences();

		[Get("/conferences/{slug}")]
		Task<ConferenceDto> GetConference(string slug);
	}
}