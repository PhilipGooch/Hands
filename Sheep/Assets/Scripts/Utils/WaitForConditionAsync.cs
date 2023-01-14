using System;
using UnityEngine;
using System.Threading.Tasks;

public struct WaitForConditionAsync
{
    Func<bool> condition;

    WaitForConditionAsync(Func<bool> condition)
    {
        this.condition = condition;
    }

    public static async Task Create(Func<bool> condition)
    {
        var instance = new WaitForConditionAsync(condition);
        await instance.Execute();
    }

    async Task Execute()
    {
        if (condition != null)
        {
            while (!condition())
            {
                await Task.Yield();
            }
        }

    }
}
