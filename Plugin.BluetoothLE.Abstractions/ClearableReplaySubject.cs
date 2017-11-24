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


        public ClearableReplaySubject(IObservable<TSource> src, IObservable<TClearTrigger> clearTrigger)
            => this.underlying = clearTrigger
                .Select(_ => Unit.Default)
                .StartWith(Unit.Default)
                .Select(_ =>
                {
                    var underlyingReplay = src.Replay();
                    replayConnectDisposable.Disposable = underlyingReplay.Connect();
                    return underlyingReplay;
                })
                .Replay(1);


        public IDisposable Subscribe(IObserver<TSource> observer) => this.underlying
            .Switch()
            .Subscribe(observer);


        public IDisposable Connect() => new CompositeDisposable(
            this.underlying.Connect(),
            this.replayConnectDisposable.Disposable
        );
    }
}
