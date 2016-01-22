using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Text;

namespace Rel.Merge.Test
{
    [TestClass]
    public class PropertyPerfTest
    {
        [TestMethod]
        public void ReadPops()
        {
            var d = new DerivedClass { Value = 1 };
            var a = new SimpleClass { Value = 1 };
            int t;
            Func<SimpleClass, int> lam = _ => _.Value;
            var refl = typeof(SimpleClass).GetProperty("Value").GetGetMethod();
            long k, j;

            var sb = new StringBuilder();
            for (int i = 75; i-- > 0; )
            {
                t = a.Value;
                t = d.Value;

                // native
                for (k = 0, j = Stopwatch.GetTimestamp() + Stopwatch.Frequency; Stopwatch.GetTimestamp() < j; k++)
                {
                    k += a.Value;
                }
                sb.Append(k);

                // compiled proxy with override
                for (k = 0, j = Stopwatch.GetTimestamp() + Stopwatch.Frequency; Stopwatch.GetTimestamp() < j; k++)
                {
                    k += d.Value;
                }
                sb.Append("\t").Append(k);

                // compiled lambda
                for (k = 0, j = Stopwatch.GetTimestamp() + Stopwatch.Frequency; Stopwatch.GetTimestamp() < j; k++)
                {
                    k += lam(a);
                }
                sb.Append("\t").Append(k);

                // reflection
                for (k = 0, j = Stopwatch.GetTimestamp() + Stopwatch.Frequency; Stopwatch.GetTimestamp() < j; k++)
                {
                    k += (int)refl.Invoke(a, null);
                }
                sb.Append("\t").Append(k).AppendLine();

            }
            Trace.TraceInformation(sb.ToString());
        }

        [TestMethod]
        public void ReadAndWritePops()
        {
            var d = new DerivedClass { Value = 1 };
            var a = new SimpleClass { Value = 1 };
            int t;
            Func<SimpleClass, int> read = _ => _.Value;
            Action<SimpleClass, int> write = (x, y) => x.Value = y;
            var get = typeof(SimpleClass).GetProperty("Value").GetGetMethod();
            var set = typeof(SimpleClass).GetProperty("Value").GetSetMethod();
            long k, j;

            var sb = new StringBuilder();
            for (int i = 15; i-- > 0; )
            {
                t = a.Value;
                t = d.Value;

                // native
                for (k = 0, j = Stopwatch.GetTimestamp() + Stopwatch.Frequency; Stopwatch.GetTimestamp() < j; k++)
                {
                    t = a.Value;
                    k += 1;
                    a.Value = t;
                }
                sb.Append(k);

                // compiled proxy with override
                for (k = 0, j = Stopwatch.GetTimestamp() + Stopwatch.Frequency; Stopwatch.GetTimestamp() < j; k++)
                {

                    t = d.Value;
                    k += 1;
                    d.Value = t;
                }
                sb.Append("\t").Append(k);

                // compiled lambda
                for (k = 0, j = Stopwatch.GetTimestamp() + Stopwatch.Frequency; Stopwatch.GetTimestamp() < j; k++)
                {

                    t = read(a);
                    k += 1;
                    write(a, t);
                }
                sb.Append("\t").Append(k);

                // reflection
                for (k = 0, j = Stopwatch.GetTimestamp() + Stopwatch.Frequency; Stopwatch.GetTimestamp() < j; k++)
                {
                    t = (int)get.Invoke(a, null);
                    k += 1;
                    set.Invoke(a, new object[1] { t });
                }
                sb.Append("\t").Append(k).AppendLine();

            }
            Trace.TraceInformation(sb.ToString());
        }
    }

    internal class DerivedClass : SimpleClass
    {
        public override int Value
        {
            get
            {
                return base.Value;
            }
            set
            {
                base.Value = value;
            }
        }
    }

    internal class SimpleClass
    {
        public virtual int Value { get; set; }
    }
}