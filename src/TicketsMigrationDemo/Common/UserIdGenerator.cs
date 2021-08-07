using System;
using System.Collections.Generic;

namespace Contracts
{
    public static class UserIdGenerator
    {
        public static IEnumerable<Guid> GenerateIds(int seed = 42)
        {
            var random = new Random(seed);
            var buffer = new byte[16];
            
            while (true)
            {
                random.NextBytes(buffer);
                yield return new Guid(buffer);
            }
            // ReSharper disable once IteratorNeverReturns
        }
    }
}