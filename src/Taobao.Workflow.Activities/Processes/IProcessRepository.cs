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

namespace Taobao.Workflow.Activities
{
    /// <summary>
    /// 流程仓储
    /// </summary>
    public interface IProcessRepository : CodeSharp.Core.RepositoryFramework.IRepository<Guid, Process>
    {
        /// <summary>
        /// 增加节点实例
        /// </summary>
        /// <param name="instance"></param>
        void AddActivityInstance(ActivityInstanceBase instance);
        /// <summary>
        /// 更新节点实例
        /// </summary>
        /// <param name="instance"></param>
        void UpdateActivityInstance(ActivityInstanceBase instance);

        //获取工作流实例树文本
        string FindWorkflowInstanceData(Guid id);
        void UpdateWorkflowInstanceData(Guid id, string data);

        ActivityInstanceBase FindActivityInstance(long id);
        ActivityInstanceBase FindActivityInstance(Process process, long workflowActivityInstanceId);

        //查找指定流程的所有节点实例，由于节点可能被多次流转，会产生多个节点实例记录
        IEnumerable<ActivityInstanceBase> FindAllActivityInstances(Process process);
        SubProcessActivityInstance FindSubProcessActivityInstance(Process process, Process sub);

        IEnumerable<Process> FindProcesses(string key, int? pageIndex, int? pageSize, ProcessStatus[] status, out long total);
        IEnumerable<Process> FindProcesses(object criteria, int? pageIndex, int? pageSize, out long total);
        IEnumerable<Process> FindSubProcesses(Process parent);
    }
}