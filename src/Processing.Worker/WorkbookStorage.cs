namespace Processing.Worker;

internal sealed class WorkbookStorage(HttpClient client)
{
    public async Task<Stream> OpenAsync(Guid identifier, CancellationToken cancellationToken)
    {
        var path = $"/uploads/{identifier:N}.xlsx";
        using var response = await client.GetAsync(path, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        const int maxBytes = 5 * 1024 * 1024;
        if (response.Content.Headers.ContentLength > maxBytes)
            throw new InvalidDataException("Stored workbook exceeds 5 MiB.");
        var stream = new MemoryStream();
        await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
        var buffer = new byte[81920];
        int read;
        while ((read = await source.ReadAsync(buffer, cancellationToken)) > 0)
        {
            if (stream.Length + read > maxBytes)
                throw new InvalidDataException("Stored workbook exceeds 5 MiB.");
            await stream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }
        stream.Position = 0;
        return stream;
    }
}
