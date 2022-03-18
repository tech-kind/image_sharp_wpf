namespace ImageLib
{
    public static partial class ImageOperator
    {
        private static ParallelOptions _parallelOptions = new ()
        {
            MaxDegreeOfParallelism = 4,
        };
    }
}