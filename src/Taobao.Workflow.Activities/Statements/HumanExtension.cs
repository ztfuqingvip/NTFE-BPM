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
    /// 人工活动扩展程序
    /// <remarks>主要用于暂存人工活动运行时信息</remarks>
    /// </summary>
    public class HumanExtension
    {
        //暂存的人工节点实例信息
        private IList<HumanActivityInstance> _instances { get; set; }

        public HumanExtension()
        {
            this._instances = new List<HumanActivityInstance>();
        }
        /// <summary>
        /// 添加人工任务信息
        /// </summary>
        /// <param name="context"></param>
        /// <param name="referredBookmark"></param>
        /// <param name="activityName"></param>
        /// <param name="flowNodeIndex"></param>
        /// <param name="actioners"></param>
        public void AddHumanTask(NativeActivityContext context, Bookmark referredBookmark, string activityName, int flowNodeIndex, string[] actioners)
        {
            if (actioners == null || actioners.Length == 0)
                throw new InvalidOperationException("没有执行人");

            this._instances.Add(new HumanActivityInstance(context.WorkflowInstanceId
                , flowNodeIndex
                , context.ActivityInstanceId
                , activityName
                , referredBookmark.Name
                , actioners));
        }
        /// <summary>
        /// 获取暂存的人工节点实例列表
        /// </summary>
        /// <returns></returns>
        public IEnumerable<HumanActivityInstance> GetHumanActivityInstances()
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
}