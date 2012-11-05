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
using Taobao.Activities;

namespace Taobao.Workflow.Activities.Statements
{
    /// <summary>
    /// 子流程活动扩展程序
    /// <remarks>用于暂存子流程活动的运行时信息</remarks>
    /// </summary>
    public class SubProcessExtension
    {
        //暂存子流程节点实例信息
        private IList<SubProcessActivityInstance> _instances { get; set; }

        public SubProcessExtension()
        {
            this._instances = new List<SubProcessActivityInstance>();
        }
        /// <summary>
        /// 添加子流程启动信息
        /// </summary>
        /// <param name="context"></param>
        /// <param name="referredBookmark"></param>
        /// <param name="activityName"></param>
        /// <param name="flowNodeIndex"></param>
        public void AddSubProcess(NativeActivityContext context, Bookmark referredBookmark, string activityName, int flowNodeIndex)
        {
            this._instances.Add(new SubProcessActivityInstance(context.WorkflowInstanceId
                , flowNodeIndex
                , context.ActivityInstanceId
                , activityName
                , referredBookmark.Name));
        }
        /// <summary>
        /// 获取暂存的子流程节点实例列表
        /// </summary>
        /// <returns></returns>
        public IEnumerable<SubProcessActivityInstance> GetSubProcessActivityInstance()
        {
            return this._instances.AsEnumerable();
        }
        /// <summary>
        /// 清除暂存信息
        /// </summary>
        public void Clear()
        {
            this._instances.Clear();
        }
    }

    /// <summary>
    /// 用于记录流程运行中产生的子流程实例的信息
    /// <remarks>同时用于描述子流程节点实例</remarks>
    /// </summary>
    public class SubProcessInstance
    {
        /// <summary>
        /// 获取流程实例标识
        /// </summary>
        public virtual Guid ProcessId { get; private set; }

        public string BookmarkName { get; private set; }
        public string ActivityName { get; private set; }

        protected SubProcessInstance()
        {
        }

        public SubProcessInstance(Guid processId, string activityName, string bookmarkName)
        {
            this.ProcessId = processId;
            this.ActivityName = activityName;
            this.BookmarkName = bookmarkName;
        }
    }
}
