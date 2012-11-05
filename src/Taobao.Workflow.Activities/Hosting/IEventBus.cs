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
using Taobao.Workflow.Activities.Hosting;

namespace Taobao.Workflow.Activities.Hosting
{
    /// <summary>
    /// 提供事件总线
    /// </summary>
    public interface IEventBus
    {
        /// <summary>
        /// 触发任务到达事件
        /// </summary>
        /// <param name="args"></param>
        void RaiseWorkItemArrived(WorkItemArgs args);
        ///// <summary>
        ///// 触发人工节点任务升级事件
        ///// </summary>
        ///// <param name="args"></param>
        //void RaiseHumanActivityInstanceEscalated(ActivityInstanceArgs args);
        /// <summary>
        /// 触发人工节点实例开始事件
        /// </summary>
        /// <param name="args"></param>
        void RaiseHumanActivityInstanceStarted(ActivityInstanceArgs args);
        /// <summary>
        /// 触发人工节点实例完成事件 
        /// </summary>
        /// <param name="args"></param>
        void RaiseHumanActivityInstanceCompleted(ActivityInstanceArgs args);
        /// <summary>
        /// 触发流程结束事件
        /// </summary>
        /// <param name="args"></param>
        void RaiseProcessCompleted(ProcessArgs args);
    }
}
