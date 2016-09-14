using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;

namespace Samples.Services.Impl
{
    public class AppStateImpl : IAppState, IAppLifecycle
    {
        readonly Subject<object> resumeSubject = new Subject<object>();
        readonly Subject<object> bgSubject = new Subject<object>();

        public IObservable<object> WhenBackgrounding()
        {
            return this.bgSubject;
        }


        public IObservable<object> WhenResuming()
        {
            return this.resumeSubject;
        }


        public void OnForeground()
        {
            this.resumeSubject.OnNext(null);
        }


        public void OnBackground()
        {
            this.bgSubject.OnNext(null);
        }
    }
}
