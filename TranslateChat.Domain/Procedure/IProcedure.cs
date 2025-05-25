namespace TranslateChat.Domain.Procedure
{
    /// <summary>
    /// 處理過程介面
    /// </summary>
    public interface IProcedure<TCtx> where TCtx : IProcedureContext
    {
        // execute async
        public Task<IProcedure<TCtx>> ExecuteAsync(
            IAsyncProcedureProcess<TCtx> process);

        public Task<IProcedure<TCtx>> ExecuteAsync<TParam>(
            IAsyncProcedureProcess<TCtx, TParam> process, TParam param);

        public Task<TCtx> GetResult();
    }


    public class BaseProcedure<TCtx> : IProcedure<TCtx>
        where TCtx : IProcedureContext
    {
        private Task<TCtx> Ctx { get; set; }

        // execute async
        public async Task<IProcedure<TCtx>> ExecuteAsync(
            IAsyncProcedureProcess<TCtx> process)
        {
            var ctx = await Ctx;
            if (!ctx.IsSuccess)
            {
                return this;
            }

            await process.Process(ctx);
            return this;
        }

        public async Task<IProcedure<TCtx>> ExecuteAsync<TParam>(
            IAsyncProcedureProcess<TCtx, TParam> process, TParam param)
        {
            var ctx = await Ctx;
            if (!ctx.IsSuccess)
            {
                return this;
            }

            await process.Process(ctx, param);
            return this;
        }

        public async Task<TCtx> GetResult()
        {
            return await Ctx;
        }

        public static async Task<IProcedure<TCtx>> From(TCtx ctx)
        {
            return new BaseProcedure<TCtx>()
            {
                Ctx = Task.FromResult<TCtx>(ctx)
            };
        }
    }


    public static class ProcedureHelper
    {
        public static async Task<IProcedure<TCtx>> ExecuteAsync<TCtx>(
            this Task<IProcedure<TCtx>> task,
            IAsyncProcedureProcess<TCtx> process)
            where TCtx : IProcedureContext
        {
            var baseProcedure = await task;
            return await baseProcedure.ExecuteAsync(process);
        }

        public static async Task<IProcedure<TCtx>> ExecuteAsync<TCtx, TParam>(
            this Task<IProcedure<TCtx>> task,
            IAsyncProcedureProcess<TCtx, TParam> process, TParam param)
            where TCtx : IProcedureContext
        {
            var baseProcedure = await task;
            return await baseProcedure.ExecuteAsync(process, param);
        }

        public static async Task<TCtx> GetResultAsync<TCtx>(
            this Task<IProcedure<TCtx>> task)
            where TCtx : IProcedureContext
        {
            var baseProcedure = await task;
            return await baseProcedure.GetResult();
        }
    }
}