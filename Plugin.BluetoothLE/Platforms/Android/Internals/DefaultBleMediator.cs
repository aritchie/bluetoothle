using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.OS;
using Javax.Security.Auth;
using Plugin.BluetoothLE.Infrastructure;


namespace Plugin.BluetoothLE.Internals
{
    public class DefaultBleMediator : IBleMediator
    {


        /// <summary>
        ///
        /// </summary>
        public static bool ForceSequentialInvocations { get; set; } = true;

       


        static TimeSpan? opPause;
        /// <summary>
        /// Time span to pause android operations
        /// DO NOT CHANGE this if you don't know what this is!
        /// </summary>
        public static TimeSpan? OperationPause
        {
            get
            {
                if (opPause != null)
                    return opPause;

                if (Build.VERSION.SdkInt < BuildVersionCodes.N)
                    return TimeSpan.FromMilliseconds(100);

                return null;
            }
            set => opPause = value;
        }


        readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);


        public void Dispose()
        {
            try
            {
                this.semaphore.Dispose();
            }
            finally
            {
                // swallow release all
            }
        }


        void Release()
        {
            try
            {
                this.semaphore.Release();
            }
            catch
            {

            }
        }


        public IObservable<T> Invoke<T>(IObservable<T> observable)
        {
            if (!ForceSequentialInvocations)
                return observable;

            return Observable.Create<T>(async ob =>
            {
                var cts = new CancellationTokenSource();
                IDisposable sub = null;
                Log.Debug("Device", "Lock - at the gate");

                try
                {
                    await this.semaphore.WaitAsync(cts.Token);
                }
                finally { }

                if (cts.IsCancellationRequested)
                {
                    Log.Debug("Device", "Lock - past the gate, but was cancelled");
                }
                else
                {
                    Log.Debug("Device", "Lock - past the gate");

                    if (OperationPause != null)
                        await Task.Delay(OperationPause.Value, cts.Token).ConfigureAwait(false);

                    sub = observable.Subscribe(
                        ob.OnNext,
                        ex =>
                        {
                            Log.Debug("Device", "Task errored - releasing lock");
                            this.Release();
                            ob.OnError(ex);
                        },
                        () =>
                        {
                            Log.Debug("Device", "Task completed - releasing lock");
                            this.Release();
                            ob.OnCompleted();
                        }
                    );
                }

                return () =>
                {
                    sub?.Dispose();
                    cts.Cancel();
                };
            });
        }
    }
}
