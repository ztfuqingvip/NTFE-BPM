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
using Taobao.Workflow.Activities.Statements;

namespace Taobao.Workflow.Activities
{
    /// <summary>
    /// 流程任务仓储
    /// <remarks>由于任务相关的事务性依赖DB，故部分方法将会绕过service而提供给其他聚合</remarks>
    /// </summary>
    public interface IWorkItemRepository : CodeSharp.Core.RepositoryFramework.IRepository<long, WorkItem>
    {
        #region 常规查询 只能获取有效任务
        /// <summary>
        /// 查找
        /// </summary>
        /// <param name="criteria"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <param name="total"></param>
        /// <returns></returns>
        IEnumerable<WorkItem> FindAllBy(object criteria, int? pageIndex, int? pageSize, out long total);
        /// <summary>
        /// 查找用户任务
        /// </summary>
        /// <param name="user"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        IEnumerable<WorkItem> FindAllBy(User user, params WorkItemStatus[] status);
        /// <summary>
        /// 查找用户任务
        /// </summary>
        /// <param name="user"></param>
        /// <param name="processTypeNames"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        IEnumerable<WorkItem> FindAllBy(User user, string[] processTypeNames, params WorkItemStatus[] status);
        /// <summary>
        /// 查找用户任务
        /// </summary>
        /// <param name="user"></param>
        /// <param name="process"></param>
        /// <param name="activityName"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        IEnumerable<WorkItem> FindAllBy(User user, Process process, string activityName);
        /// <summary>
        /// 查找指定流程任务
        /// </summary>
        /// <param name="process"></param>
        /// <returns></returns>
        IEnumerable<WorkItem> FindAllByProcess(Process process);
        /// <summary>
        /// 查找指定流程和节点的任务
        /// </summary>
        /// <param name="process"></param>
        /// <param name="activityName"></param>
        /// <returns></returns>
        IEnumerable<WorkItem> FindAllByProcessAndActivity(Process process, string activityName);
        /// <summary>
        /// 查找指定流程类型任务
        /// </summary>
        /// <param name="typeName"></param>
        /// <returns></returns>
        IEnumerable<WorkItem> FindAllByProcessType(string typeName);
        #endregion

        /// <summary>
        /// 查找指定流程在指定的人工节点实例的产生任务
        /// </summary>
        /// <param name="process"></param>
        /// <param name="humanActivityInstance"></param>
        /// <returns></returns>
        IEnumerable<WorkItem> FindWorkItemsByActivityInstance(Process process, HumanActivityInstance humanActivityInstance);
        /// <summary>
        /// 查找指定流程在指定的人工节点实例的产生任务
        /// </summary>
        /// <param name="workItem"></param>
        /// <returns></returns>
        IEnumerable<WorkItem> FindWorkItemsByActivityInstance(WorkItem workItem);

        #region 过时，早期设计
        ///// <summary>
        ///// 尝试打开任务
        ///// </summary>
        ///// <param name="workItem"></param>
        ///// <param name="openedWorkItem"></param>
        ///// <returns></returns>
        //bool TryOpenWorkItem(WorkItem workItem, out WorkItem openedWorkItem);
        ///// <summary>
        ///// 执行任务
        ///// </summary>
        ///// <param name="workItem"></param>
        ///// <param name="action">动作</param>
        ///// <param name="isHumanDone">人工节点是否完成</param>
        ///// <returns>返回是否执行成功</returns>
        //bool ExecuteWorkItem(WorkItem workItem, string action, bool isHumanDone); 
        //bool SetAsOpen(WorkItem workItem);
        //bool SetAsOpenAndSetOthersAsNoSlot(WorkItem workItem);
        //int CountOpenedWorkItemsByActivityInstance(Process process, HumanActivityInstance humanActivityInstance);

        //bool TryOpenWorkItem(WorkItem workItem);
        //void ExecuteWorkItem(WorkItem w, User user, string action, IDictionary<string, string> inputs);
        //bool SetAsExecuted(WorkItem workItem, string action, bool isHumanDone);
        #endregion

        /// <summary>
        /// 取消所有任务
        /// </summary>
        /// <param name="process"></param>
        void CancelAll(Process process);
        /// <summary>
        /// 取消所有任务
        /// </summary>
        /// <param name="process"></param>
        /// <param name="humanActivityInstanceId"></param>
        void CancelAll(Process process, long humanActivityInstanceId);
    }
}