 using Rel.Merge.Strategies;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Rel.Merge
{
    /// <summary>
    ///   Repents a merge operation.
    /// </summary>
    public class MergeOperation : IMergeProvider
    {
        /// <summary>
        ///   Performs a merge of the specified kind.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="kind">The kind.</param>
        /// <param name="before">The before.</param>
        /// <param name="current">The current.</param>
        /// <param name="after">The after.</param>
        /// <returns></returns>
        public IMergeResolution<TEntity> Merge<TEntity>(MergeKind kind, TEntity before, TEntity current, TEntity after)
        {
            MergeOperation<TEntity> op;

            op = new MergeOperation<TEntity>(before, current, after);

            var result = op.Merge();
            bool resolved = result.IsResolved();
            return result;
        }
    }

    /// <summary>
    ///   Encapsulates the merge API behind a concrete type which
    ///   caches merge resolution strategies by the entity type.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    internal class MergeOperation<TEntity>
    {
        private static readonly CloneMethod[] s_cloneProps;

        /// <summary>
        ///   Tests that the BFIM of a conflict is current with the
        ///   Current state.
        /// </summary>
        private static readonly CurrentStateCheck s_isCurrent;

        /// <summary>
        ///   The dynamic merge implementation used.
        /// </summary>
        private static readonly Func<MergeOperation<TEntity>, IMergeResolution<TEntity>> s_mergeImpl;

        /// <summary>
        ///   Invokes the merge on all properties which are not
        ///   control properties.
        /// </summary>
        private static readonly PropertyMergeMethod[] s_propertyMerges;

        private static readonly MergeableAttribute[] s_typeAttr;
        private TEntity _afim;
        private TEntity _bfim;
        private TEntity _current;

        /// <summary>
        ///   Initializes the <see cref="MergeOperation{TEntity}"/> class.
        /// </summary>
        static MergeOperation()
        {
            var type = typeof(TEntity);
            var props = type.GetProperties();

            if (InitCc(type, props, out s_mergeImpl, ref s_isCurrent))
            {
                InitMerge(type, props, ref s_propertyMerges, ref s_typeAttr, ref s_cloneProps);
            }
        }

        /// <summary>
        ///   Initializes a new instance of the
        ///   <see cref="MergeOperation{TEntity}"/> class.
        /// </summary>
        /// <param name="before">The before image.</param>
        /// <param name="current">The current image.</param>
        /// <param name="after">The after image.</param>
        public MergeOperation(TEntity before, TEntity current, TEntity after)
        {
            _bfim = before;
            _current = current;
            _afim = after;
        }

        /// <summary>
        ///   Clones data from src to dest.
        /// </summary>
        /// <param name="src">The source.</param>
        /// <param name="dest">The destination.</param>
        private delegate void CloneMethod(TEntity src, TEntity dest);

        /// <summary>
        ///   Signature of concurrency checking implementations.
        /// </summary>
        /// <param name="bfim">The before image.</param>
        /// <param name="current">The current image.</param>
        /// <returns></returns>
        private delegate bool CurrentStateCheck(TEntity bfim, TEntity current);

        /// <summary>
        ///   Signature of property level merging.
        /// </summary>
        /// <param name="before">The before image.</param>
        /// <param name="current">The current image.</param>
        /// <param name="after">The after image.</param>
        /// <returns></returns>
        private delegate PendingMergeResolution PropertyMergeMethod(TEntity before, TEntity current, TEntity after);

        /// <summary>
        ///   Signature for class level merging.
        /// </summary>
        /// <param name="kind">The kind of merge resolved by <see cref="M:ControlledMerge"/>.</param>
        /// <param name="mop">The merge operation.</param>
        /// <returns></returns>
        private delegate IMergeResolution<TEntity> TypeMergeMethod(MergeKind kind, MergeOperation<TEntity> mop);

        /// <summary>
        ///   Gets the final after image resultant from this merge operation.
        /// </summary>
        /// <value>The final after image value..</value>
        public TEntity AFIM { get { return _afim; } }

        /// <summary>
        ///   Determines if two OCCUTS values match.
        /// </summary>
        /// <param name="cc1">The CC1.</param>
        /// <param name="cc2">The CC2.</param>
        /// <returns>
        ///   <see langword="true"/> if the time stamps match;
        ///   otherwise, <see langword="false"/>.
        /// </returns>
        internal static bool TimestampIsCurrentChecker(byte[] cc1, byte[] cc2)
        {
            if (cc1 == null)
            {
                return cc2 == null;
            }
            else if (cc2 == null)
            {
                return cc1 == null;
            }
            else if (object.ReferenceEquals(cc1, cc2))
            {
                return true;
            }
            else if (cc1.Length != cc2.Length)
            {
                return false;
            }
            else if (cc1.Length == 8)
            {
                // unrolled loop evaluation for performance as this
                // method should see a lot of mileage
                return
                    cc1[0] == cc2[0] &&
                    cc1[1] == cc2[1] &&
                    cc1[2] == cc2[2] &&
                    cc1[3] == cc2[3] &&
                    cc1[4] == cc2[4] &&
                    cc1[5] == cc2[5] &&
                    cc1[6] == cc2[6] &&
                    cc1[7] == cc2[7];
            }
            else
            {
                return cc1.SequenceEqual(cc2);
            }
        }

        /// <summary>
        ///   Performs the merge operation.
        /// </summary>
        internal IMergeResolution<TEntity> Merge()
        {
            return s_mergeImpl(this);
        }

        /// <summary>
        ///   Effectively last write wins. "Merge" where TEntity does
        ///   not participate in optimistic concurrency control.
        /// </summary>
        /// <returns><see langword="true"/> always.</returns>
        /// <remarks>
        ///   An entity which does not participate in concurrency
        ///   control is implicitly last write wins which mirrors
        ///   regular RDBMS behavior.
        /// </remarks>
        private static IMergeResolution<TEntity> ChaoticMerge(MergeOperation<TEntity> op)
        {
            return Resolve(MergeActionResult.Resolved, op.AFIM);
        }

        /// <summary>
        ///   Clones the non-control properties.
        /// </summary>
        /// <param name="src">The source.</param>
        /// <param name="dest">The destination.</param>
        private static void CloneNonControl(TEntity src, TEntity dest)
        {
            for (int i = s_cloneProps.Length; i-- > 0; )
            {
                s_cloneProps[i](src, dest);
            }
        }

        /// <summary>
        ///   The concurrency controlled merge implementation.
        /// </summary>
        /// <param name="op">The merge operation invoking merge.</param>
        /// <returns>
        ///   <see langword="true"/> if merge was successful;
        ///   otherwise, <see langword="false"/>.
        /// </returns>
        private static IMergeResolution<TEntity> ControlledMerge(MergeOperation<TEntity> op)
        {
            var bfim = op._bfim;

            // should we already be aware of it? no? this is new,
            // implies no conflict is possible (in this version at least)
            if (bfim == null)
                return Resolve(MergeActionResult.Create, op.AFIM);

            var cfim = op._current;

            // did someone else already delete this while bfim was
            // offline? yes? handle as special case since property
            // merge strategies have no basis for comparison
            if (cfim == null)
                return MergeByType(MergeKind.HiddenDelete, op);

            // is this normal optimistic write behavior? yes? then why
            // waste time fancy dancing?
            if (s_isCurrent(bfim, cfim))
                return Resolve(MergeActionResult.Resolved, op._afim);

            var afim = op._afim;

            // are we trying to delete someone else's work? yes?
            // handle as special case since the afim cannot be used
            // for comparison and the other writer presumably worked
            // from bfim to get to current.
            if (afim == null)
                return MergeByType(MergeKind.DirtyDelete, op);

            // finally, we have work todo. This is a legitimate
            // conflicting update
            return MergeByType(MergeKind.ConflictingUpdate, op);
        }

        /// <summary>
        ///   Creates a pre-compiled wrapper to clone data between two entities.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>
        ///   A precompiled method which will clone values from one
        ///   entity to another for the property given.
        /// </returns>
        private static CloneMethod CreateCloneWrapper(PropertyInfo property)
        {
            var type = typeof(TEntity);
            var src = Expression.Parameter(typeof(TEntity));
            var dest = Expression.Parameter(typeof(TEntity));

            var expr = Expression
                .Lambda<CloneMethod>(
                    Expression.Call(
                        dest,
                        property.GetSetMethod(),
                        Expression.Call(
                            src,
                            property.GetGetMethod())
                    ),
                src, dest);

            return expr.Compile();
        }

        /// <summary>
        ///   Creates a precompiled wrapper to evaluate whether two
        ///   entities share the same timestamp value for optimistic
        ///   concurrency control.
        /// </summary>
        /// <param name="ccProperty">The cc property.</param>
        /// <returns></returns>
        /// <exception cref="MergeException">
        ///   Timestamp field must be readable. or Timestamp
        ///   attributed fields must be of type byte[].
        /// </exception>
        private static CurrentStateCheck CreateOccUtsCurrencyChecker(PropertyInfo ccProperty)
        {
            // current version only support Timestamp concurrency
            // evaluation. A future revision may support the
            // System.ComponentModel.DataAnnotations.ConcurrencyCheckAttribute
            // as well.

            if (!ccProperty.CanRead)
            {
                throw new MergeException("Timestamp field must be readable.");
            }

            if (ccProperty.PropertyType != typeof(byte[]))
            {
                throw new MergeException("Timestamp attributed fields must be of type byte[].");
            }

            var fGet = PropertyWrapper.CreatePropertyGetter<TEntity, byte[]>(ccProperty);
            return (TEntity a, TEntity b) => TimestampIsCurrentChecker(fGet(a), fGet(b));
        }

        /// <summary>
        ///   Initializes the concurrency control domain for the given type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="properties">The properties.</param>
        /// <param name="merge">The merge.</param>
        /// <param name="IsCurrent">The is current.</param>
        /// <returns>
        ///   <see langword="true"/> if the domain is controlled;
        ///   otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="System.InvalidOperationException">
        ///   Only one timestamp property is permitted per type.
        /// </exception>
        private static bool InitCc(Type type, PropertyInfo[] properties, out Func<MergeOperation<TEntity>, IMergeResolution<TEntity>> merge, ref CurrentStateCheck IsCurrent)
        {
            // current version only supports Timestamp concurrency
            // evaluation. A future revision may support the
            // System.ComponentModel.DataAnnotations.ConcurrencyCheckAttribute
            // as well.
            PropertyInfo ccProperty;

            try
            {
                // in a future revision, extend this with inspection
                // for
                // System.ComponentModel.DataAnnotations.ConcurrencyCheckAttribute
                // and return a method which looks like (a,b)=>AllCcProps.All(_=>_(a,b));
                ccProperty = properties
                    .Where(_ => _.GetCustomAttributes(true).OfType<System.ComponentModel.DataAnnotations.TimestampAttribute>().Any())
                    .SingleOrDefault();
            }
            catch (InvalidOperationException)
            {
                throw new InvalidOperationException("Only one timestamp property is permitted per type.");
            }

            // in the absence CC, there exists no possibility of
            // conflict / detection.
            if (ccProperty == null)
            {
                merge = ChaoticMerge;
                return false;
            }

            IsCurrent = CreateOccUtsCurrencyChecker(ccProperty);
            merge = ControlledMerge;

            return true;
        }

        /// <summary>
        ///   Initializes the merge domain of this class.
        /// </summary>
        /// <param name="type">The type to merge on.</param>
        /// <param name="properties">
        ///   The properties of the given type.
        /// </param>
        /// <param name="propertyMerges">
        ///   A collection of property wrappers which will perform the
        ///   merge of data from one entity to the next.
        /// </param>
        /// <param name="typeMergeAttr">
        ///   The mergeable attributes applied to the type given.
        /// </param>
        /// <param name="cloneProps">
        ///   A collection of property wrappers to move data between
        ///   entities for all but control fields.
        /// </param>
        private static void InitMerge(Type type, PropertyInfo[] properties,
            ref PropertyMergeMethod[] propertyMerges,
            ref MergeableAttribute[] typeMergeAttr,
            ref CloneMethod[] cloneProps)
        {
            typeMergeAttr = type.GetCustomAttributes<MergeableAttribute>(false).ToArray();

            var controlProps = properties.Where(IsControlProperty).ToArray();
            var mergedProps = properties.Where(IsMergedProperty).ToArray();
            var unmergedProps = properties
                .Except(controlProps)
                .Except(mergedProps).ToArray();

            LastWriteWinsAttribute unmergedPropertyDefault = null;
            if (typeMergeAttr.Length == 0)
            {
                // type defaults to reject
                typeMergeAttr = new[] { new LastWriteWinsAttribute(false) };

                unmergedPropertyDefault = new LastWriteWinsAttribute(mergedProps.Length > 0);
            }
            else
            {
                unmergedPropertyDefault = new LastWriteWinsAttribute(false);
            }

            if (mergedProps.Length == 0)
            {
                propertyMerges = new PropertyMergeMethod[] { NeverMerge };
            }
            else
            {
                var p = mergedProps
                    .Select(_ => WrapProperty(_, _.GetCustomAttributes<MergeableAttribute>(false).ToArray()))
                    .Union(unmergedProps.Select(_ => WrapProperty(_, unmergedPropertyDefault)))
                    .ToArray();

                propertyMerges = p.ToArray();
            }

            cloneProps = properties.Except(controlProps)
                .Select(CreateCloneWrapper)
                .ToArray();
        }

        /// <summary>
        ///   Determines whether the property given is a control
        ///   property. That is, a key field or a concurrency control field.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>
        ///   <see langword="true"/> if the given property is a
        ///   controlled field.
        /// </returns>
        private static bool IsControlProperty(PropertyInfo property)
        {
            var controlMembers = property
                .GetCustomAttributes(typeof(KeyAttribute), true)
                .Union(
                property.GetCustomAttributes<ConcurrencyCheckAttribute>(true))
                .Union(
                property.GetCustomAttributes<TimestampAttribute>(true));

            return controlMembers.Any();
        }

        /// <summary>
        ///   Determines whether the given propert is a merged
        ///   property. That is, will participate in verifying the
        ///   validity of data resulting from dirty writes.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>
        ///   <see langword="true"/> if the property participates in
        ///   merge; otherwise, <see langword="false"/>.
        /// </returns>
        private static bool IsMergedProperty(PropertyInfo property)
        {
            if (!property.GetCustomAttributes<MergeableAttribute>(false).Any())
                return false;
            if (IsControlProperty(property))
            {
                System.Diagnostics.Trace.TraceWarning("Merge attributes on property {0} of type {1} will be ignored due to control attributes applied.",
                    property.Name, property.DeclaringType.FullName);
                return false;
            }
            return true;
        }

        /// <summary>
        ///   Merges all mergeable properties.
        /// </summary>
        /// <param name="kind">
        ///   The kind of merge operation; only here for signature compliance..
        /// </param>
        /// <param name="op">The merge operation.</param>
        /// <returns>
        ///   A merge resolution which reports as resolved if all
        ///   merged properties could be merged; otherwise, unresolvable.
        /// </returns>
        private static IMergeResolution<TEntity> MergeAllProperties(MergeKind kind, MergeOperation<TEntity> op)
        {
            // the result are cached and processed after so that the
            // parent type merge can process clean data should the
            // property merge fail.
            var results = new PendingMergeResolution[s_propertyMerges.Length];
            for (int i = results.Length; i-- > 0; )
            {
                var result = s_propertyMerges[i](op._bfim, op._current, op._afim);
                if (!result.CanResolve)
                    return Resolve(MergeActionResult.Unresolved, op._current);
                results[i] = result;
            }

            for (int i = results.Length; i-- > 0; )
            {
                results[i].Commit();
            }
            return Resolve(MergeActionResult.Update, op._current);
        }

        /// <summary>
        ///   Entry into merge on the given entity.
        /// </summary>
        /// <param name="mergeKind">Kind of the merge.</param>
        /// <param name="op">The merge operation.</param>
        /// <returns>
        ///   A merge resolution which reports as resolved if all
        ///   merged properties could be merged; otherwise, unresolvable.
        /// </returns>
        private static IMergeResolution<TEntity> MergeByType(MergeKind mergeKind, MergeOperation<TEntity> op)
        {
            if (mergeKind == MergeKind.ConflictingUpdate)
            {
                // finally, we've work to do! This is a dirty update
                // request and does warrant merging. start with the
                // most fine grain merge possible
                var test = MergeAllProperties(MergeKind.ConflictingUpdate, op);

                if (test.IsResolved())
                {
                    return test;
                }
            }

            var resolution = MergeType(mergeKind, op);
            var isResolved = resolution.IsResolved();
            return resolution;
        }

        /// <summary>
        ///   Performs the actual type level merge.
        /// </summary>
        /// <param name="kind">The kind.</param>
        /// <param name="mop">The mop.</param>
        /// <returns>
        ///   A merge resolution which reports as resolved if all
        ///   merged properties could be merged; otherwise, unresolvable.
        /// </returns>
        private static IMergeResolution<TEntity> MergeType(MergeKind kind, MergeOperation<TEntity> mop)
        {
            TEntity result = default(TEntity);
            var action = new MergeAction<TEntity>(kind, mop._bfim, mop._current, mop._afim);
            for (int i = s_typeAttr.Length; i-- > 0; )
            {
                s_typeAttr[i].Merge(action);
                if (action.Resolved)
                {
                    if (mop._current != null && !object.ReferenceEquals(action.ResolvedValue, mop._current))
                    {
                        CloneNonControl(mop._afim, mop._current);
                        result = mop._current;
                    }
                    else
                    {
                        result = mop._afim;
                    }
                    break;
                }
            }

            return Resolve(action.Result, result);
        }

        /// <summary>
        ///   A property merge implementation which always rejects.
        /// </summary>
        /// <param name="bfim">The bfim.</param>
        /// <param name="current">The current.</param>
        /// <param name="afim">The afim.</param>
        /// <returns><see langword="false"/></returns>
        private static PendingMergeResolution NeverMerge(TEntity bfim, TEntity current, TEntity afim)
        {
            return new PendingMergeResolution(MergeActionResult.Unresolved, Noop);
        }

        /// <summary>
        ///   Convenience no operation method.
        /// </summary>
        private static void Noop()
        {
        }

        /// <summary>
        ///   Produces an IMergeResolution{} using the resolution and
        ///   value given.
        /// </summary>
        /// <param name="result">The result.</param>
        /// <param name="tEntity">The t entity.</param>
        /// <returns>A completed merge resoution.</returns>
        private static IMergeResolution<TEntity> Resolve(MergeActionResult result, TEntity tEntity)
        {
            return new BasicMergeResolution<TEntity>(result, tEntity);
        }

        /// <summary>
        ///   Wraps the specified property with a merge acceptor
        /// </summary>
        /// <param name="prop">The property.</param>
        /// <param name="attrs">The merge attributes.</param>
        /// <returns>Accessor methods to the property merge.</returns>
        /// <exception cref="System.ArgumentException">
        ///   attrs must contain at least one mergeable attribute to wrap.
        /// </exception>
        private static PropertyMergeMethod WrapProperty(PropertyInfo prop, params MergeableAttribute[] attrs)
        {
            if (attrs == null || attrs.Length == 0)
                throw new ArgumentException("attrs must contain at least one mergeable attribute to wrap.");

            var w = typeof(MergePropertyWrapper<,>).MakeGenericType(prop.DeclaringType, prop.PropertyType);
            var wrapper = Activator.CreateInstance(w, new object[] { prop, attrs }, null);
            var kind = Expression.Constant(MergeKind.ConflictingUpdate); //Expression.Parameter(typeof(MergeKind));
            var b = Expression.Parameter(prop.DeclaringType);
            var c = Expression.Parameter(prop.DeclaringType);
            var a = Expression.Parameter(prop.DeclaringType);

            var exec = Expression.Lambda<PropertyMergeMethod>(
                Expression.Call(
                Expression.Constant(wrapper),
                w.GetMethod("Merge"),
                kind, b, c, a),
                /*kind,*/ b, c, a);

            return exec.Compile();
        }
    }
}