namespace ResilientServices
{
	public interface IApiService
	{
		ITekConfApi Speculative { get; }
		ITekConfApi UserInitiated { get; }
		ITekConfApi Background { get; }
	}
}