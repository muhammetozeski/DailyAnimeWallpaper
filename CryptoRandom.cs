using System.Buffers.Binary;
using System.Security.Cryptography;

namespace DailyAnimeWallpaper;

/// <summary>Provides cryptographically secure random numbers and UInt128 weighted item selection.</summary>
internal static class CryptoRandom
{
    /// <summary>Fills sixteen random bytes using the OS CSPRNG and returns them as a <see cref="UInt128"/>.</summary>
    /// <returns>A uniformly distributed 128-bit unsigned integer.</returns>
    public static UInt128 NextUInt128()
    {
        Span<byte> buffer = stackalloc byte[16];
        RandomNumberGenerator.Fill(buffer);
        return BinaryPrimitives.ReadUInt128LittleEndian(buffer);
    }

    /// <summary>Selects a random <see cref="UInt128"/> below the specified upper bound.</summary>
    /// <param name="exclusiveUpperBound">The exclusive upper limit.</param>
    /// <returns>Zero when the bound is one or zero; otherwise a random value in <c>[0, exclusiveUpperBound)</c>.</returns>
    public static UInt128 NextUInt128(UInt128 exclusiveUpperBound)
    {
        if (exclusiveUpperBound <= 1)
            return 0;

        return NextUInt128() % exclusiveUpperBound;
    }

    /// <summary>Selects a cryptographically random zero-based index below <paramref name="count"/>.</summary>
    /// <param name="count">The number of elements.</param>
    /// <returns>Zero when <paramref name="count"/> is one or less; otherwise an index in <c>[0, count)</c>.</returns>
    public static int NextIndex(int count)
    {
        if (count <= 1)
            return 0;

        return (int)(NextUInt128() % (uint)count);
    }

    /// <summary>Sums positive integer weights into a <see cref="UInt128"/> total and picks the item matching a random offset.</summary>
    /// <typeparam name="T">The candidate item type.</typeparam>
    /// <param name="candidates">The candidate list to choose from.</param>
    /// <param name="weightSelector">Reads a weight in <c>[0, int.MaxValue]</c> for each candidate.</param>
    /// <returns>The chosen candidate.</returns>
    public static T SelectWeighted<T>(IReadOnlyList<T> candidates, Func<T, int> weightSelector)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(weightSelector);

        UInt128 totalWeight = CalculateTotalWeight(candidates, weightSelector);
        if (totalWeight == 0)
            throw new InvalidOperationException("Cannot select from candidates whose total weight is zero.");

        UInt128 target = NextUInt128(totalWeight);
        UInt128 cumulative = 0;
        T? lastPositive = default;

        for (int i = 0; i < candidates.Count; i++)
        {
            T item = candidates[i];
            int weight = weightSelector(item);
            if (weight <= 0)
                continue;

            lastPositive = item;
            cumulative += (uint)weight;
            if (target < cumulative)
                return item;
        }

        return lastPositive!;
    }

    /// <summary>Calculates the <see cref="UInt128"/> sum of all positive candidate weights.</summary>
    /// <typeparam name="T">The candidate item type.</typeparam>
    /// <param name="candidates">The candidate list.</param>
    /// <param name="weightSelector">Reads each candidate's integer weight.</param>
    /// <returns>The total positive weight.</returns>
    public static UInt128 CalculateTotalWeight<T>(IReadOnlyList<T> candidates, Func<T, int> weightSelector)
    {
        UInt128 totalWeight = 0;
        for (int i = 0; i < candidates.Count; i++)
        {
            int weight = weightSelector(candidates[i]);
            if (weight > 0)
                totalWeight += (uint)weight;
        }

        return totalWeight;
    }
}
