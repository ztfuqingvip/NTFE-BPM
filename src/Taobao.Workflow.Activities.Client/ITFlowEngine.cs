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

namespace Taobao.Workflow.Activities.Client
{
    //目前通过NSF暴露引擎服务接口，主要由流程服务调用，参数名大小写敏感且不可轻易修改

    /// <summary>
    /// NTFE-BPM常规服务接口
    /// </summary>
    public interface ITFlowEngine
    {
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
        /// <summary>
        /// 为发起人判断是否允许撤销流程
        /// </summary>
        /// <param name="processId"></param>
        /// <param name="originator">发起人</param>
        /// <returns></returns>
        bool CanRevokeProcess(Guid processId, string originator);
        /// <summary>
        /// 撤销流程
        /// </summary>
        /// <param name="processId"></param>
        /// <param name="originator">发起人</param>
        void RevokeProcess(Guid processId, string originator);
        /// <summary>
        /// 为上一节点执行人检查是否允许执行流程回滚操作
        /// </summary>
        /// <param name="processId"></param>
        /// <param name="previousActioner">上一节点执行人用户名</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        bool CanRollbackProcess(Guid processId, string previousActioner);
        /// <summary>
        /// 为上一节点执行人执行流程回滚操作
        /// </summary>
        /// <param name="processId"></param>
        /// <param name="previousActioner">上一节点执行人用户名</param>
        /// <exception cref="InvalidOperationException"></exception>
        void RollbackProcess(Guid processId, string previousActioner);
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
        /// 为流程实例动态追加人工节点
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
        /// 撤销代理设置
        /// </summary>
        /// <param name="actAsUser">被代理人</param>
        /// <param name="user">代理人</param>
        void RevokeAgent(string actAsUser, string user);
        /// <summary>
        /// 创建代理信息
        /// <remarks>默认支持范围代理</remarks>
        /// </summary>
        /// <param name="user">代理人</param>
        /// <param name="actAsUser">被代理人</param>
        /// <param name="begin">开始时间</param>
        /// <param name="end">结束时间</param>
        /// <param name="processTypeNames">若要声明代理指定流程则设置此参数，若指定的类型没有定义则忽略该类型</param>
        void CreateAgent(string user, string actAsUser, DateTime begin, DateTime end, string[] processTypeNames);
        #endregion
    }
}