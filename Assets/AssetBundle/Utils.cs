using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public static class Extensions
{
    public static async Task<T> ToAsync<T>(this T asyncOperation) where T : AsyncOperation
    {
        TaskCompletionSource<T> tcs = new TaskCompletionSource<T>();

        asyncOperation.completed += OnCompleted;

        return await tcs.Task;

        void OnCompleted(AsyncOperation operation)
        {
            operation.completed -= OnCompleted;
            tcs.TrySetResult(operation as T);
        }
    }
}
