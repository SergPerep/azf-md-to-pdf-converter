using Md2PDFConverter.Models;
using Microsoft.Azure.Functions.Worker;

namespace Md2PDFConverter.Entities;

public class RunCounter
{
    [Function(nameof(RunCounter))]
    public static Task Run([EntityTrigger] TaskEntityDispatcher dispatcher)
    {
        return dispatcher.DispatchAsync(operation =>
        {
            if (operation.State.GetState(typeof(RunCountState)) is null)
            {
                operation.State.SetState(new RunCountState());
            }

            switch (operation.Name)
            {
                case nameof(RunCountOperationName.IncCompleted):
                {
                    var state = operation.State.GetState<RunCountState>();
                    state!.Completed++;
                    state!.Total++;
                    operation.State.SetState(state);
                    return new(state);
                }
                case nameof(RunCountOperationName.IncFailed):
                {
                    var state = operation.State.GetState<RunCountState>();
                    state!.Failed++;
                    state!.Total++;
                    operation.State.SetState(state);
                    return new(state);
                }
                case nameof(RunCountOperationName.Get):
                    return new(operation.State.GetState<RunCountState>());
                case nameof(RunCountOperationName.Reset):
                    operation.State.SetState(new RunCountState());
                    break;
            }
            return default;
        });
    }
}