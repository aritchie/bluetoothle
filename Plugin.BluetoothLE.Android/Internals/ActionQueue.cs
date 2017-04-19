using System;
using System.Threading;
using System.Threading.Tasks;
using Android.App;


namespace Plugin.BluetoothLE.Internals
{
    public class ActionQueue
    {
        readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);


        public async Task<T> Await<T>(Func<T> func, bool executeOnMainThread)
        {
            await this.semaphore.WaitAsync();
            try
            {
                T result = default(T);
                if (executeOnMainThread && AndroidConfig.WriteOnMainThread)
                {
                    var tcs = new TaskCompletionSource<T>();
                    Application.SynchronizationContext.Post(_ =>
                    {
                        try
                        {
                            var r = func();
                            tcs.TrySetResult(r);
                        }
                        catch (Exception ex)
                        {
                            tcs.TrySetException(ex);
                        }
                    }, null);

                    result = await tcs.Task;
                }
                else
                {
                    result = func();
                }
                return result;
            }
            finally
            {
                this.semaphore.Release();
            }
        }


        public async Task Await(Action action, bool executeOnMainThread)
        {
            await this.semaphore.WaitAsync();
            try
            {
                if (executeOnMainThread && AndroidConfig.WriteOnMainThread)
                {
                    var tcs = new TaskCompletionSource<object>();
                    Application.SynchronizationContext.Post(_ =>
                    {
                        try
                        {
                            action();
                            tcs.TrySetResult(null);
                        }
                        catch (Exception ex)
                        {
                            tcs.TrySetException(ex);
                        }
                    }, null);

                    await tcs.Task;
                }
                else
                {
                    action();
                }
            }
            finally
            {
                this.semaphore.Release();
            }
        }
    }
}