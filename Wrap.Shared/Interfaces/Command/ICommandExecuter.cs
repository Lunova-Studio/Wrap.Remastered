namespace Wrap.Shared.Interfaces.Command;

/// <summary>
/// 命令执行器接口
/// </summary>
public interface ICommandExecuter {
    /// <summary>
    /// 获取命令名称
    /// </summary>
    /// <returns>命令名称</returns>
    string GetName();

    /// <summary>
    /// 获取命令描述
    /// </summary>
    /// <returns>命令描述</returns>
    string GetDescription();

    /// <summary>
    /// 获取命令用法
    /// </summary>
    /// <returns>命令用法</returns>
    string GetUsage();

    /// <summary>
    /// 执行命令
    /// </summary>
    /// <param name="args">命令参数</param>
    Task OnExecuteAsync(string[] args);
}