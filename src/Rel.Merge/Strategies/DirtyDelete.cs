using System;

namespace Rel.Merge.Strategies
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class DirtyDelete : MergeableAttribute
    {
        protected internal override void Merge<TValue>(MergeAction<TValue> request)
        {
            if (request.Kind == MergeKind.DirtyDelete)
            {
                if (object.Equals(default(TValue), request.CFIM))
                {
                    //System.Diagnostics.Trace.TraceInformation("Dirty NOOP Delete");
                    request.Resolve(MergeActionResult.Resolved, default(TValue));
                }
                else
                {
                    //System.Diagnostics.Trace.TraceInformation("Dirty Delete -- > Approved");
                    request.Resolve(MergeActionResult.Delete, request.CFIM);
                }
            }
        }
    }
}