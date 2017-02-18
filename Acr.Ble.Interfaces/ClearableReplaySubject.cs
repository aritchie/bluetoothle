// copied from: http://stackoverflow.com/questions/28945061/how-can-i-clear-the-buffer-on-a-replaysubject/
using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;


namespace Plugin.BluetoothLE
{
    public class ClearableReplaySubject<TSource, TClearTrigger> : IConnectableObservable<TSource>
    {
        readonly IConnectableObservable<IObservable<TSource>> underlying;
        readonly SerialDisposable replayConnectDisposable = new SerialDisposable();
        //readonly IObservable<TSource> source;
        //readonly IObservable<TClearTrigger> trigger;
        //readonly IConnectableObservable<TSource> underlying;
        //readonly IList<TSource> list = new List<TSource>();


        public ClearableReplaySubject(IObservable<TSource> src, IObservable<TClearTrigger> clearTrigger)
        {
            //this.source = src;
            //this.trigger = clearTrigger;
            //this.underlying = Observable.Create<TSource>(ob =>
            //{
            //    var sub1 = src.Subscribe(this.list.Add);
            //    var sub2 = clearTrigger.Subscribe(_ => this.list.Clear());

            //    return new CompositeDisposable(sub1, sub2);
            //})
            //.Publish();
            this.underlying = clearTrigger
                .Select(_ => Unit.Default)
                .StartWith(Unit.Default)
                .Select(_ =>
                {
                    var underlyingReplay = src.Replay();
                    replayConnectDisposable.Disposable = underlyingReplay.Connect();
                    return underlyingReplay;
                })
                .Replay(1);
        }


        public IDisposable Subscribe(IObserver<TSource> observer)
        {
            //return this.underlying.Subscribe(observer);
            return this.underlying.Switch().Subscribe(observer);
        }


        public IDisposable Connect()
        {
            //return this.underlying.Connect();
            return new CompositeDisposable(this.underlying.Connect(), this.replayConnectDisposable.Disposable);
        }
    }
}
