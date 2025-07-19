using System.Collections.Generic;

namespace Wrap.Remastered.Commands;

/// <summary>
/// 命令标签页补全接口
/// </summary>
public interface ICommandTabCompleter
{
    /// <summary>
    /// 获取命令名称
    /// </summary>
    /// <returns>命令名称</returns>
    string GetName();

    /// <summary>
    /// 获取补全选项
    /// </summary>
    /// <param name="args">命令参数</param>
    /// <returns>补全选项列表</returns>
    IList<string> OnComplete(string[] args);
} 