namespace ResilientServices.Services
{
	public interface IApiService
	{
		ITekConfApi Speculative { get; }
		ITekConfApi UserInitiated { get; }
		ITekConfApi Background { get; }
	}
}