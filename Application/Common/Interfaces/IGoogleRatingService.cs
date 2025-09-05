namespace Application.Common.Interfaces;

public interface IGoogleRatingService
{
    Task<double?> GetRatingAsync(string businessName, string address, CancellationToken cancellationToken = default);
}
