using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Rel.Merge
{
    /// <summary>
    /// A base class for generating compiled get/set accessors for
    /// runtime resolved properties.
    /// </summary>
    public abstract class PropertyWrapper
    {
        /// <summary>
        /// Creates the specified information.
        /// </summary>
        /// <typeparam name="TClass">The type of the class.</typeparam>
        /// <typeparam name="TProp">The type of the property.</typeparam>
        /// <param name="info">The information.</param>
        /// <returns></returns>
        public static PropertyWrapper<TClass, TProp> Create<TClass, TProp>(PropertyInfo info)
        {
            return new PropertyWrapper<TClass, TProp>(info);
        }

        /// <summary>
        ///   Creates a property getter.
        /// </summary>
        /// <typeparam name="TProp">The type of the value.</typeparam>
        /// <param name="property">The property to wrap.</param>
        /// <returns>
        ///   A function which wraps the get method of an arbitrary
        ///   property with a native compiled function.
        /// </returns>
        /// <remarks>
        ///   This is for performance gains in runtime resolved property
        ///   accessors. This method circumvents the System.Reflection
        ///   namespace performance penalties and overhead of explicit
        ///   dynamic proxy class and assembly generation with the
        ///   System.Reflection.Emit namespace. Benchmarks for property
        ///   retrieval via native, compiled lambda expression, and via
        ///   reflection invocation measured in mm:ss.fffffff and given below.
        ///
        ///   Native: e.Prop
        ///   Lambda: e =&gt; e.prop
        ///   Reflection: propGetter.Invoke(e, null)
        ///
        ///  Iterations |     Native    |     Lambda    |  Reflection
        /// ------------+---------------+---------------+--------------
        ///       1000  | 00:00.0005833 | 00:00.0000617 | 00:00.0003298
        ///      10000  | 00:00.0004251 | 00:00.0006168 | 00:00.0031853
        ///     100000  | 00:00.0083098 | 00:00.0056037 | 00:00.0442952
        ///    1000000  | 00:00.0355637 | 00:00.0536704 | 00:00.3333400
        ///   10000000  | 00:00.3717554 | 00:00.5645168 | 00:03.0993979
        ///  100000000  | 00:03.6002561 | 00:05.3618157 | 00:31.0219926
        /// 1000000000  | 00:35.2922689 | 00:53.7391055 | 05:10.4827815
        /// </remarks>
        public static Func<TClass, TProp> CreatePropertyGetter<TClass, TProp>(PropertyInfo property)
        {
            var p = Expression.Parameter(typeof(TClass));

            var expr = Expression.Lambda<Func<TClass, TProp>>(
                Expression.Call(
                p,
                property.GetGetMethod()),
                p);

            return expr.Compile();
        }

        /// <summary>
        ///   Creates a property setter.
        /// </summary>
        /// <typeparam name="TProp">The type of the value.</typeparam>
        /// <param name="ccProperty">The cc property.</param>
        /// <returns></returns>
        public static Action<TClass, TProp> CreatePropertySetter<TClass, TProp>(PropertyInfo property)
        {
            var pe = Expression.Parameter(typeof(TClass));
            var pv = Expression.Parameter(typeof(TProp));

            var expr = Expression.Lambda<Action<TClass, TProp>>(
                Expression.Call(
                pe,
                property.GetSetMethod(),
                new[] { pv }),
                pe, pv);

            return expr.Compile();
        }
    }

    /// <summary>
    ///   A typed implementation of the property wrapper to avoid
    ///   repeated un-/boxing operations for value types.
    /// </summary>
    /// <typeparam name="TClass">The type of the class.</typeparam>
    /// <typeparam name="TProp">The type of the property.</typeparam>
    public class PropertyWrapper<TClass, TProp>
        : PropertyWrapper
    {
        private readonly Func<TClass, TProp> _getter;
        private readonly string _name;
        private readonly Action<TClass, TProp> _setter;

        /// <summary>
        ///   Initializes a new instance of the <see
        ///   cref="PropertyWrapper{TClass, TProp}"/> class.
        /// </summary>
        /// <param name="info">The information.</param>
        public PropertyWrapper(PropertyInfo info)
        {
            _name = info.Name;
            if (info.CanRead)
            {
                _getter = CreatePropertyGetter<TClass, TProp>(info);
            }
            if (info.CanWrite)
                _setter = CreatePropertySetter<TClass, TProp>(info);
        }

        /// <summary>
        ///   Gets the value stored at the wrapped property from the
        ///   specified object.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns>The property value of obj.</returns>
        public TProp Get(TClass obj)
        {
            return _getter(obj);
        }

        /// <summary>
        ///   Sets the given value to the wrapped property on the
        ///   specified object.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="value">The value.</param>
        /// <returns>
        ///   <paramref name="obj"/> for convenience in chaining if so desired.
        /// </returns>
        /// <example>
        ///   <c>Assert.AreEqual(100, propertyWrapper.Set(ent, 100).Property);</c>
        /// </example>
        public TClass Set(TClass obj, TProp value)
        {
            _setter(obj, value);
            return obj;
        }
    }
}