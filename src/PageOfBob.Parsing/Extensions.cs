using System;
using System.Collections.Generic;

namespace PageOfBob.Parsing
{
    public static class Extensions
    {
        /// <summary>
        /// Will run a rule as long as there is input, yield results from that rule.
        /// </summary>
        public static IEnumerable<K> Tokenize<T, K>(this Rule<T, K> rule, ISource<T> source, bool throwOnFailure = true)
        {
            do
            {
                switch (source)
                {
                    case Empty<T> empty: yield break;
                    case Source<T> nonempty:
                        var result = rule(nonempty);
                        switch (result)
                        {
                            case Success<T, K> success:
                                yield return success.Value;
                                source = success.Next;
                                break;
                            case Failure<T, K> fail:
                                if (throwOnFailure)
                                    throw new FormatException("Invalid token");
                                else
                                    yield break;
                            default:
                                throw new NotImplementedException();                                
                        }
                        break;
                    default: throw new NotImplementedException();
                }
            } while (true);
        }
    }
}
