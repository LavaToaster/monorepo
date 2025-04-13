using System;
using System.IO;
using System.Security.Cryptography;

namespace nuget2bazel.Utilities;

/// <summary>
/// Provides utility methods for cryptographic hash operations.
/// </summary>
public static class HashUtilities
{
    /// <summary>
    /// Calculates the SHA-512 hash of a stream in the format required by Bazel.
    /// </summary>
    /// <param name="stream">The stream to hash.</param>
    /// <param name="resetPosition">Whether to reset the stream position after hashing.</param>
    /// <returns>A SHA-512 hash string prefixed with "sha512-".</returns>
    public static string CalculateSha512Hash(Stream stream, bool resetPosition = true)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));
            
        var originalPosition = stream.Position;

        try
        {
            using var sha512 = SHA512.Create();
            var base64 = Convert.ToBase64String(sha512.ComputeHash(stream));
            
            return $"sha512-{base64}";
        }
        finally
        {
            // Reset the stream position if requested
            if (resetPosition)
            {
                stream.Position = originalPosition;
            }
        }
    }
}