using System;
using System.Threading;
using System.Threading.Tasks;

namespace IgorEnterprise.Misc
{
    public static class Timeout
    {
        public static async Task<bool> ForAsync(Action operationWithTimeout, TimeSpan maxTime)
        {
            var timeoutTask = Task.Delay(maxTime);
            var completionSource = new TaskCompletionSource<Thread>();

            // This will await while any of both given tasks end.
            await Task.WhenAny
            (
                timeoutTask,
                Task.Factory.StartNew
                (
                    () =>
                    {
                        // This will let main thread access this thread and force a Thread.Abort
                        // if the operation must be canceled due to a timeout
                        completionSource.SetResult(Thread.CurrentThread);
                        operationWithTimeout();
                    }
                )
            );

            // Since timeoutTask was completed before wrapped File.Copy task you can 
            // consider that the operation timed out
            if (timeoutTask.Status == TaskStatus.RanToCompletion)
            {
                // Timed out!
                Thread thread = await completionSource.Task;
                thread.Abort();
                return false;
            }
            return true;
        }
    }
}