using System.Buffers;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace BigBrain.Finance.OutcomeAudit;

// Authenticity relative to an explicitly supplied key only. Neither the key's trust
// nor the claimed clock/instant is independently attested by this offline component.
internal sealed class SignedCommitmentReceiptVerifier : ICommitmentReceiptVerifier
{
    private static ReadOnlySpan<byte> Profile => "BB073/receipt/v1/ECDSA-P256-SHA256/P1363\0"u8;
    private static readonly SearchValues<char> HexDigits = SearchValues.Create("0123456789abcdef");
    internal const int MaxReceiptBytes = 2048;
    private readonly byte[] publicKey;
    private readonly string issuer;
    private readonly string keyId;
    private readonly string clockSource;

    // SEC1 uncompressed P-256 public point: 04 || X[32] || Y[32]. No private-key input.
    internal SignedCommitmentReceiptVerifier(ReadOnlySpan<byte> publicKey, string issuer,
        string keyId, string clockSource)
    {
        if (publicKey.Length != 65 || publicKey[0] != 4 ||
            !Identity(issuer) || !Identity(keyId) || !Identity(clockSource))
            throw new ArgumentException("Invalid explicit receipt verification configuration.");
        this.publicKey = publicKey.ToArray();
        this.issuer = issuer;
        this.keyId = keyId;
        this.clockSource = clockSource;
        using var key = CreatePublicKey(); // Reject invalid curve points at configuration time.
    }

    public bool Verify(CommitmentClaims claims, ReadOnlySpan<byte> receiptBytes, DateTimeOffset asOf)
    {
        if (claims is null || receiptBytes.Length > MaxReceiptBytes || receiptBytes.Length <= 64 ||
            claims.Issuer != issuer || claims.ClockSource != clockSource ||
            !Utc(asOf) || asOf < claims.WitnessedAt || asOf > claims.ValidUntil)
            return false;
        try
        {
            var payload = CanonicalPayload(claims, keyId);
            // Reconstruct rather than parse: no alternative lengths, ordering, encoding,
            // trailing bytes or unsigned envelope fields can be accepted.
            if (receiptBytes.Length != payload.Length + 64 ||
                !receiptBytes[..payload.Length].SequenceEqual(payload)) return false;
            using var key = CreatePublicKey();
            return key.VerifyData(payload, receiptBytes[payload.Length..], HashAlgorithmName.SHA256,
                DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        }
        catch (ArgumentException) { return false; }
        catch (CryptographicException) { return false; }
    }

    // Fixed profile || key ID || receipt ID || issuer || clock || both SHA256 digests
    // || witnessed UTC ticks || valid-until UTC ticks. Identity lengths are uint16 BE;
    // digests are 32 raw bytes; times are int64 BE (.NET ticks since 0001-01-01 UTC).
    internal static byte[] CanonicalPayload(CommitmentClaims claims, string keyId)
    {
        if (claims is null || !Identity(keyId) || !Identity(claims.ReceiptId) ||
            !Identity(claims.Issuer) || !Identity(claims.ClockSource) ||
            !Hash(claims.ManifestHash) || !Hash(claims.ManifestBytesHash) ||
            !Utc(claims.WitnessedAt) || !Utc(claims.ValidUntil) || claims.ValidUntil < claims.WitnessedAt)
            throw new ArgumentException("Invalid canonical receipt claims.");
        var identities = new[] { keyId, claims.ReceiptId, claims.Issuer, claims.ClockSource };
        var payload = new byte[Profile.Length + identities.Sum(value => 2 + value.Length) + 64 + 16];
        Profile.CopyTo(payload);
        var offset = Profile.Length;
        foreach (var value in identities)
        {
            BinaryPrimitives.WriteUInt16BigEndian(payload.AsSpan(offset), (ushort)value.Length);
            offset += 2;
            offset += Encoding.ASCII.GetBytes(value, payload.AsSpan(offset));
        }
        Convert.FromHexString(claims.ManifestHash[7..]).CopyTo(payload, offset);
        Convert.FromHexString(claims.ManifestBytesHash[7..]).CopyTo(payload, offset + 32);
        BinaryPrimitives.WriteInt64BigEndian(payload.AsSpan(offset + 64), claims.WitnessedAt.Ticks);
        BinaryPrimitives.WriteInt64BigEndian(payload.AsSpan(offset + 72), claims.ValidUntil.Ticks);
        return payload;
    }

    private ECDsa CreatePublicKey() => ECDsa.Create(new ECParameters
    {
        Curve = ECCurve.NamedCurves.nistP256,
        Q = new ECPoint { X = publicKey[1..33], Y = publicKey[33..65] }
    });

    // Visible ASCII only, exact ordinal identity; no whitespace, Unicode normalization
    // or replacement encoding. Limits are checked before any payload allocation.
    private static bool Identity(string value) => value is { Length: > 0 and <= 256 } &&
        value.All(c => c is >= '!' and <= '~');
    private static bool Hash(string value) => value is { Length: 71 } &&
        value.StartsWith("sha256:", StringComparison.Ordinal) &&
        value.AsSpan(7).IndexOfAnyExcept(HexDigits) < 0;
    private static bool Utc(DateTimeOffset value) => value != default && value.Offset == TimeSpan.Zero;
}
