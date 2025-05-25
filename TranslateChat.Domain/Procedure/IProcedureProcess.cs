namespace TranslateChat.Domain.Procedure;

public interface IProcedureProcess
{
}

/// <summary>
/// 存儲過程程序介面
/// </summary>
public interface IProcedureProcess<TFromCtx> : IProcedureProcess
	where TFromCtx : IProcedureContext
{
	/// <summary>
	/// 處理程序
	/// </summary>
	TFromCtx Process(TFromCtx ctx);
}

/// <summary>
/// 但參數的process
/// </summary>
public interface IProcedureProcess<TFromCtx, in TParam> : IProcedureProcess
	where TFromCtx : IProcedureContext
{
	/// <summary>
	/// 執行程序，並加入參數
	/// </summary>
	TFromCtx Process(TFromCtx ctx, TParam param);
}

/// <summary>
/// 存儲過程程序介面
/// </summary>
public interface IAsyncProcedureProcess<TFromCtx> : IProcedureProcess
	where TFromCtx : IProcedureContext
{
	/// <summary>
	/// 處理程序
	/// </summary>
	Task<TFromCtx> Process(TFromCtx ctx);
}

/// <summary>
/// 但參數的process
/// </summary>
public interface IAsyncProcedureProcess<TFromCtx, in TParam> : IProcedureProcess
	where TFromCtx : IProcedureContext
{
	/// <summary>
	/// 執行程序，並加入參數
	/// </summary>
	Task<TFromCtx> Process(TFromCtx ctx, TParam param);
}