using System.Collections.Generic;
using System.Threading.Tasks;
using Fusillade;
using TekConf.Mobile.Core.Dtos;

namespace ResilientServices.Services
{
	public interface IConferencesService
	{
		Task<List<ConferenceDto>> GetConferences(Priority priority);
		Task<ConferenceDto> GetConference(Priority priority, string slug);
	}
}