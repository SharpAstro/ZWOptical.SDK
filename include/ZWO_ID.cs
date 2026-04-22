using System.Runtime.InteropServices;
using System.Text;

namespace ZWOptical.SDK;

/// <summary>
/// Mirrors the native <c>ASI_ID</c>/<c>EAF_ID</c>/<c>EFW_ID</c> structs: 8 raw bytes.
/// The header documents them as <c>unsigned char id[8]</c> with no encoding or
/// termination contract - the bytes are whatever the camera firmware last stored.
/// For virgin / non-programmed bodies (e.g. entry-level ASI120MM-S without a
/// factory-written serial) the bytes can be anything, including stack garbage.
///
/// Callers that want to use the id as a stable string identity should use
/// <see cref="TryGetPrintableText"/>, not <see cref="ToString"/>. Blindly
/// ASCII-decoding the bytes and treating the result as a serial produces
/// URIs / logs / device lookups that can't round-trip (see issue noted with
/// ASI120MM-S returning <c>\x1E,&#x2423;&#x2423;&#x2423;&#x2423;&#x2423;&#x2423;</c>).
/// </summary>
public readonly struct ZWO_ID
{
    [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 8)]
    private readonly byte[] _id;

    /// <summary>
    /// Hex dump of the 8 raw bytes. Always printable and round-trippable - use this
    /// for diagnostics or for a deterministic identity when the bytes are not valid
    /// printable ASCII. Never returns null.
    /// </summary>
    public string ToHexString() => _id is { Length: > 0 } ? System.Convert.ToHexString(_id) : "";

    /// <summary>
    /// ASCII decoding with trailing nulls trimmed. Kept for debugging / backward
    /// compat; may contain non-printable bytes and is NOT a safe identity.
    /// </summary>
    public override string ToString() => Encoding.ASCII.GetString(_id).TrimEnd((char)0);

    /// <summary>
    /// Returns the ASCII-decoded id only when every byte is printable ASCII
    /// (0x20..0x7E) after trimming trailing nulls, AND at least one byte is
    /// non-whitespace. Otherwise returns null. Use this when the id is going
    /// into a URI, log line, or any context where a clean printable identifier
    /// is required.
    /// </summary>
    public bool TryGetPrintableText(out string text)
    {
        if (_id is not { Length: > 0 })
        {
            text = "";
            return false;
        }

        // Find the end of the id (strip trailing nulls, which native firmware
        // typically uses to right-pad an id shorter than 8 chars).
        var end = _id.Length;
        while (end > 0 && _id[end - 1] == 0) end--;
        if (end == 0)
        {
            text = "";
            return false;
        }

        var hasNonSpace = false;
        for (var i = 0; i < end; i++)
        {
            var b = _id[i];
            if (b < 0x20 || b > 0x7E)
            {
                text = "";
                return false;
            }
            if (b != 0x20) hasNonSpace = true;
        }

        if (!hasNonSpace)
        {
            text = "";
            return false;
        }

        text = Encoding.ASCII.GetString(_id, 0, end);
        return true;
    }
}
