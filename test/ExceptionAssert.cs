using System;
using System.Diagnostics;

namespace Rel.Test
{
    internal static class ExceptionAssert
    {
        [DebuggerStepThrough]
        public static void Throws<TExeception>(Action action)
            where TExeception : Exception
        {
            Throws<TExeception>(AlwaysTrue, action);
        }

        [DebuggerStepThrough]
        public static void Throws<TExeception>(Func<TExeception, bool> filter, Action action)
            where TExeception : Exception
        {
            try
            {
                action();
                throw new TestException(string.Format("Expected exception of type {0}", typeof(TExeception).FullName));
            }
            catch (TExeception ex)
            {
                if (!filter(ex))
                    throw new TestException("Proper exception type thrown.  Failed filter test.");
            }
        }

        [DebuggerStepThrough]
        private static bool AlwaysTrue(Exception arg) { return true; }
    }
}