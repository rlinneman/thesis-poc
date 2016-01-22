using System;

namespace Rel.Merge.Strategies
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class HiddenDelete : MergeableAttribute
    {
        protected internal override void Merge<TValue>(MergeAction<TValue> request)
        {
            if (request.Kind == MergeKind.HiddenDelete)
            {
                if (object.Equals(default(TValue), request.AFIM))
                {
                    //System.Diagnostics.Trace.TraceInformation("Hidden NOOP Delete");
                    // noop delete
                    request.Resolve(MergeActionResult.Resolved, default(TValue));
                }
                else
                {
                    //System.Diagnostics.Trace.TraceInformation("Hidden Delete --> Create");
                    request.Resolve(MergeActionResult.Create, request.AFIM);
                }
            }
        }
    }
}