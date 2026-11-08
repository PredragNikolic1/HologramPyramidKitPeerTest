
using System;
using System.Collections.Generic;
using System.Linq;


public static class BackgroundCalculator
{
    private static float MIN_BACKGROUND = 0.00001f;
    public static float Calculate(IReadOnlyList<float> values) {
        if (values == null || values.Count == 0)
            throw new ArgumentException("No RMS values collected, cannot calculate the background.", nameof(values));

        float[] sorted = values.OrderBy(v => v).ToArray();
        int middle = sorted.Length / 2;

        float median = sorted.Length % 2 == 1
            ? sorted[middle]                                   // odd count: the middle value
            : (sorted[middle - 1] + sorted[middle]) / 2f;
        
        return MathF.Max(median, MIN_BACKGROUND);
    } 
}
