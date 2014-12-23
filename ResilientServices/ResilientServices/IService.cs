using System.Collections.Generic;
using System.Threading.Tasks;
using TekConf.Mobile.Core.Dtos;

namespace ResilientServices
{
	public interface IService
	{
		Task<List<ConferenceDto>> GetConferences();
		Task<ConferenceDto> GetConference(string slug);
	}
}