using Application.Dtos.FcmDtos;

namespace Application.Interfaces.Services;

/// <summary>
/// Abstraction over FCM HTTP v1 / Admin SDK send.
/// Implementations must never log raw tokens or service-account material.
/// </summary>
public interface IFcmProvider
{
    string ProviderName { get; }

    Task<FcmSendResult> SendAsync(FcmSendRequest request, CancellationToken ct = default);
}
