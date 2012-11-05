/*
    Copyright (C) 2012  Alibaba

	This program is free software: you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Taobao.Workflow.Activities.Client;

namespace Taobao.Workflow.Activities.Management
{
    /// <summary>
    /// 提供对NTFE-BPM的管理服务接口
    /// </summary>
    public interface ITFlowEngine//:Client.ITFlowEngine
    {
        #region Client api 由于NSF暂时无法暴露继承关系
        #region WorkItem
        /// <summary>
        /// 获取任务，不存在则返回Null
        /// </summary>
        /// <param name="id">任务标识</param>
        /// <param name="user">用户名</param>
        /// <returns></returns>
        WorkItem GetWorkItem(long id, string user);
        /// <summary>
        /// 打开/占用任务
        /// </summary>
        /// <param name="id">任务标识</param>
        /// <param name="user">用户名</param>
        /// <returns></returns>
        WorkItem OpenWorkItem(long id, string user);
        /// <summary>
        /// 获取任务列表
        /// </summary>
        /// <param name="user">用户名</param>
        /// <returns></returns>
        WorkItem[] GetWorkItems(string user);
        /// <summary>
        /// 获取指定流程的任务
        /// </summary>
        /// <param name="user"></param>
        /// <param name="processId"></param>
        /// <param name="activityName">不指定则设置空</param>
        /// <returns></returns>
        WorkItem[] GetWorkItemsByProcess(string user, Guid processId, string activityName);
        /// <summary>
        /// 执行任务
        /// </summary>
        /// <param name="id"></param>
        /// <param name="user"></param>
        /// <param name="action"></param>
        /// <param name="inputs">同时更新流程数据，不更新则设置null</param>
        void ExecuteWorkItem(long id, string user, string action, IDictionary<string, string> inputs);
        /// <summary>
        /// 转交任务
        /// </summary>
        /// <param name="id"></param>
        /// <param name="fromUser"></param>
        /// <param name="toUser"></param>
        void RedirectWorkItem(long id, string fromUser, string toUser);
        #endregion

        #region Process
        /// <summary>
        /// 发起流程
        /// </summary>
        /// <param name="processTypeName">流程类型名</param>
        /// <param name="title">流程标题</param>
        /// <param name="originator">发起人</param>
        /// <param name="priority">优先级</param>
        /// <param name="dataFields">流程数据</param>
        Process NewProcess(string processTypeName, string title, string originator, int priority, IDictionary<string, string> dataFields);
        /// <summary>
        /// 发起流程
        /// <remarks>允许使用外部赋值的processId</remarks>
        /// </summary>
        /// <param name="assignedProcessId">外部赋值的processId</param>
        /// <param name="processTypeName">流程类型名</param>
        /// <param name="title">流程标题</param>
        /// <param name="originator">发起人</param>
        /// <param name="priority">优先级</param>
        /// <param name="dataFields">流程数据</param>
        Process NewProcessWithAssignedId(Guid assignedProcessId, string processTypeName, string title, string originator, int priority, IDictionary<string, string> dataFields);
        /// <summary>
        /// 获取流程信息，不存在则返回Null
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Process GetProcess(Guid id);
        /// <summary>
        /// 更新流程数据
        /// </summary>
        /// <param name="processId"></param>
        /// <param name="overrides"></param>
        void UpdateDataFields(Guid processId, IDictionary<string, string> overrides);
        #endregion

        #region ActivityInstance
        /// <summary>
        /// 强制唤醒处于延迟状态的流程节点实例
        /// <remarks>通常</remarks>
        /// </summary>
        /// <param name="processId">流程实例标识</param>
        /// <param name="activityName">指定要唤醒的节点名称</param>
        /// <exception cref="InvalidOperationException"></exception>
        void WakeUpDelayedActivity(Guid processId, string activityName);
        /// <summary>
        /// 判断是否允许在流程当前节点之后按指定模式动态追加一个人工节点
        /// </summary>
        /// <param name="processId">流程实例标识</param>
        /// <param name="mode">节点追加模式</param>
        /// <param name="appendActivityName">期望追加的节点名称</param>
        /// <returns></returns>
        BooleanResult CanAppendHumanActivity(Guid processId, AppendHumanMode mode, string appendActivityName);
        /// <summary>
        /// 为流程实例动态追加人工节点（加签、要求更多）
        /// <remarks>当前节点应是人工节点</remarks>
        /// </summary>
        /// <param name="processId">流程实例标识</param>
        /// <param name="mode">节点追加模式</param>
        /// <param name="appendHumanSetting">人工节点设置</param>
        void AppendHumanActivity(Guid processId, AppendHumanMode mode, AppendHumanSetting appendHumanSetting);
        #endregion

        #region Agent
        /// <summary>
        /// 获取用户的代理信息
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        Agent[] GetAgents(string user);
        /// <summary>
        /// 撤销用户的所有代理设置
        /// </summary>
        /// <param name="user"></param>
        void RevokeAllAgents(string user);
        /// <summary>
        /// 创建代理信息
        /// <remarks>默认支持范围代理</remarks>
        /// </summary>
        /// <param name="user">代理人</param>
        /// <param name="actAsUser">被代理人</param>
        /// <param name="begin">开始时间</param>
        /// <param name="end">结束时间</param>
        /// <param name="processTypeNames">若要声明代理指定流程则设置此参数</param>
        void CreateAgent(string user, string actAsUser, DateTime begin, DateTime end, string[] processTypeNames);
        #endregion
        #endregion

        #region WorkItem
        /// <summary>
        /// 获取所有任务
        /// </summary>
        /// <param name="processTypeName"></param>
        /// <returns></returns>
        WorkItem[] GetAllWorkItemsByType(string processTypeName);
        /// <summary>
        /// 搜索任务
        /// </summary>
        /// <param name="query"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        WorkItemsInfo SearchWorkItems(WorkItemQuery query, int pageIndex, int pageSize);
        /// <summary>
        /// 将处于占用、open状态的任务进行释放
        /// </summary>
        /// <param name="id"></param> 
        void ReleaseWorkItem(long id);
        #endregion

        #region Process
        /// <summary>
        /// 按关键字查询有效的流程
        /// <remarks>非Completed和Deleted</remarks>
        /// </summary>
        /// <param name="key"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        ProcessesInfo GetProcessesByKeyword(string key, int pageIndex, int pageSize);
        /// <summary>
        /// 搜索流程
        /// </summary>
        /// <param name="query">查询对象</param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        ProcessesInfo SearchProcesses(ProcessQuery query, int pageIndex, int pageSize);
        /// <summary>
        /// 将流程实例切换到指定流程版本
        /// </summary>
        /// <param name="processId"></param>
        /// <param name="version"></param>
        void ChangeProcessType(Guid processId, string version);
        /// <summary>
        /// 停止流程
        /// </summary>
        /// <param name="processId"></param>
        void StopProcess(Guid processId);
        /// <summary>
        /// 重新启动流程
        /// </summary>
        /// <param name="processId"></param>
        void RestartProcess(Guid processId);
        /// <summary>
        /// 删除流程
        /// </summary>
        /// <param name="processId"></param>
        void DeleteProcess(Guid processId);
        /// <summary>
        /// 将流程重定向/跳转到指定节点
        /// </summary>
        /// <param name="processId"></param>
        /// <param name="activityName"></param>
        void RedirectProcess(Guid processId, string activityName);
        /// <summary>
        /// 重试错误流程
        /// </summary>
        /// <param name="faultProcessId"></param>
        void RetryFaultProcess(Guid faultProcessId);
        #endregion

        #region ProcessType
        /// <summary>
        /// 获取所有当前版本的流程类型
        /// </summary>
        /// <returns></returns>
        ProcessType[] GetProcessTypes();
        /// <summary>
        /// 获取流程类型历史版本信息
        /// </summary>
        /// <param name="name">流程类型名</param>
        /// <returns></returns>
        ProcessType[] GetHistoriesOfProcessType(string name);
        /// <summary>
        /// 获取流程类型的所有版本
        /// </summary>
        /// <param name="name">流程类型名</param>
        /// <returns></returns>
        ProcessType[] GetAllVersionsOfProcessType(string name);
        /// <summary>
        /// 获取工作流定义文本
        /// xaml+xml
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        string[] GetWorkflowDefinition(string name);
        /// <summary>
        /// 创建流程类型并设为当前版本
        /// </summary>
        /// <param name="name">流程类型名</param>
        /// <param name="workflowDefinition"></param>
        /// <param name="customSettingsDefinition"></param>
        /// <param name="description">流程描述</param>
        /// <param name="group">分组名，可以为空</param>
        void CreateProcessType(string name, string workflowDefinition, string customSettingsDefinition, string description, string group);
        /// <summary>
        /// 将指定流程版本设置当前版本
        /// </summary>
        /// <param name="name">流程类型名</param>
        /// <param name="version">版本</param>
        void SetCurrentProcessType(string name, string version);
        #endregion

        #region Function of Script 提供对应用可用脚本函数的管理功能
        /// <summary>
        /// 获取支持的脚本方法定义
        /// </summary>
        /// <returns></returns> 
        ScriptFunction[] GetScriptFunctions();
        #endregion

        /// <summary>
        /// 获取所有流程异常/错误记录
        /// </summary>
        /// <returns></returns>
        ErrorRecord[] GetErrors();
    }
}