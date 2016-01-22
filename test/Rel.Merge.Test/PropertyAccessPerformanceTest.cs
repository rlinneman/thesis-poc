using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.Text;

namespace Rel.Merge.Test
{
    [TestClass]
    public class PropertyAccessPerformanceTest
    {
        [TestMethod]
        public void PropertyReadTimes()
        {
            var derived = new DerivedEntity();
            var entity = new Entity();
            Func<Entity, long> lambda = _ => _.Value;
            var refl = typeof(Entity).GetProperty("Value").GetGetMethod();
            long mark,dot, count, holder;
            var sb = new StringBuilder()
                .Append("Native,Derived,Lambda,Reflection");

            entity.Value = derived.Value = 42;

            for (int i = 1; i-- > 0; )
            {

                // Native
                for (count = 0,
                    mark = Stopwatch.GetTimestamp(),
                    dot = mark + Stopwatch.Frequency;
                    Stopwatch.GetTimestamp() < dot;
                    count++)
                {
                    holder = entity.Value;
                }
                sb.AppendLine().Append(count.ToString());

                // Derived
                for (count = 0,
                    mark = Stopwatch.GetTimestamp(),
                    dot = mark + Stopwatch.Frequency;
                    Stopwatch.GetTimestamp() < dot;
                    count++)
                {
                    holder = derived.Value;
                }
                sb.Append(',').Append(count.ToString());


                // Lambda
                for (count = 0,
                    mark = Stopwatch.GetTimestamp(),
                    dot = mark + Stopwatch.Frequency;
                    Stopwatch.GetTimestamp() < dot;
                    count++)
                {
                    holder = lambda(entity);
                }
                sb.Append(',').Append(count.ToString());


                // Reflection
                for (count = 0,
                    mark = Stopwatch.GetTimestamp(),
                    dot = mark + Stopwatch.Frequency;
                    Stopwatch.GetTimestamp() < dot;
                    count++)
                {
                    holder = (int)refl.Invoke(entity, null);
                }
                sb.Append(',').Append(count.ToString());
            }

            System.Diagnostics.Debug.Print(sb.ToString());
        }
    }

    internal class Entity
    {
        public virtual int Value { get; set; }
    }

    internal class DerivedEntity : Entity
    {
        public override int Value
        {
            get { return base.Value; }
            set { base.Value = value; }
        }
    }
}
