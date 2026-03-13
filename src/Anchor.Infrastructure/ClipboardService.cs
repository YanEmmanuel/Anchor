using Anchor.Application.Abstractions;
using TextCopy;

namespace Anchor.Infrastructure;

public sealed class ClipboardService : IClipboardService
{
    public async Task<bool> CopyAsync(string text, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await TextCopy.ClipboardService.SetTextAsync(text);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
