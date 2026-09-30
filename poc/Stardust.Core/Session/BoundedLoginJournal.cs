using System.Text;

namespace Stardust.Core.Session;

/// <summary>Allowlisted JSONL only. Stops writing after a size/event limit or uncertain I/O tail.</summary>
public sealed class BoundedLoginJournal : IDisposable
{
    private readonly Stream stream;
    private readonly bool leaveOpen;
    private readonly int maximumBytes;
    private readonly int maximumEvents;
    private readonly object sync = new();
    private bool disposed;

    public BoundedLoginJournal(Stream stream, bool leaveOpen = false,
        int maximumBytes = 262_144, int maximumEvents = 4096)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanWrite) throw new ArgumentException("Journal stream must be writable.", nameof(stream));
        if (stream.CanSeek && (stream.Position != 0 || stream.Length != 0))
            throw new ArgumentException("Journal stream must be empty.", nameof(stream));
        if (maximumBytes is < 1 or > 16_777_216) throw new ArgumentOutOfRangeException(nameof(maximumBytes));
        if (maximumEvents is < 1 or > 1_000_000) throw new ArgumentOutOfRangeException(nameof(maximumEvents));
        this.stream = stream;
        this.leaveOpen = leaveOpen;
        this.maximumBytes = maximumBytes;
        this.maximumEvents = maximumEvents;
    }

    public int WrittenBytes { get; private set; }
    public int WrittenEvents { get; private set; }
    public int DroppedEvents { get; private set; }
    public int IoFailures { get; private set; }
    public bool HasUncertainTail { get; private set; }

    /// <summary>Explicit destination only; refuses to replace an existing file.</summary>
    public static BoundedLoginJournal CreateNew(string path, int maximumBytes = 262_144, int maximumEvents = 4096)
    {
        if (maximumBytes is < 1 or > 16_777_216) throw new ArgumentOutOfRangeException(nameof(maximumBytes));
        if (maximumEvents is < 1 or > 1_000_000) throw new ArgumentOutOfRangeException(nameof(maximumEvents));
        FileStream file = new(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
        try { return new(file, maximumBytes: maximumBytes, maximumEvents: maximumEvents); }
        catch { file.Dispose(); throw; }
    }

    public void Write(LoginDiagnosticEvent value)
    {
        byte[] record = Encoding.UTF8.GetBytes(LoginDiagnostics.Encode(value) + "\n");
        lock (sync)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (HasUncertainTail || WrittenEvents >= maximumEvents || record.Length > maximumBytes - WrittenBytes)
            {
                if (DroppedEvents < int.MaxValue) DroppedEvents++;
                return;
            }
            try
            {
                stream.Write(record);
                WrittenBytes += record.Length;
                WrittenEvents++;
            }
            catch (Exception)
            {
                HasUncertainTail = true;
                if (IoFailures < int.MaxValue) IoFailures++;
            }
        }
    }

    public void Flush()
    {
        lock (sync)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            try { stream.Flush(); }
            catch (Exception)
            {
                HasUncertainTail = true;
                if (IoFailures < int.MaxValue) IoFailures++;
            }
        }
    }

    public void Dispose()
    {
        lock (sync)
        {
            if (disposed) return;
            disposed = true;
            try { stream.Flush(); }
            catch (Exception) { HasUncertainTail = true; if (IoFailures < int.MaxValue) IoFailures++; }
            if (!leaveOpen)
            {
                try { stream.Dispose(); }
                catch (Exception) { if (IoFailures < int.MaxValue) IoFailures++; }
            }
        }
    }
}
