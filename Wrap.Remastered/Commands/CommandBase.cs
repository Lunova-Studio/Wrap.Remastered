namespace Wrap.Remastered.Commands;

/// <summary>
/// 命令基类
/// </summary>
public abstract class CommandBase : ICommandExecuter
{
    /// <summary>
    /// 获取命令名称
    /// </summary>
    /// <returns>命令名称</returns>
    public abstract string GetName();

    /// <summary>
    /// 获取命令描述
    /// </summary>
    /// <returns>命令描述</returns>
    public abstract string GetDescription();

    /// <summary>
    /// 获取命令用法
    /// </summary>
    /// <returns>命令用法</returns>
    public abstract string GetUsage();

    /// <summary>
    /// 执行命令
    /// </summary>
    /// <param name="args">命令参数</param>
    public abstract Task OnExecuteAsync(string[] args);
} 