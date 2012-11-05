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
    /// <summary>
    /// 工作项/任务
    /// </summary>
    public class WorkItem
    {
        /// <summary>
        /// 标识
        /// </summary>
        public long ID { get; set; }
        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }
        /// <summary>
        /// 任务到达时间
        /// </summary>
        public DateTime ArrivedTime { get; set; }
        /// <summary>
        /// 任务的原始执行人
        /// </summary>
        public string OriginalActioner { get; set; }
        /// <summary>
        /// 任务的当前执行人
        /// </summary>
        public string Actioner { get; set; }
        /// <summary>
        /// 任务状态
        /// </summary>
        public WorkItemStatus Status { get; set; }
        /// <summary>
        /// 任务执行地址
        /// </summary>
        public string Url { get; set; }
        /// <summary>
        /// 任务操作
        /// </summary>
        public string[] Actions { get; set; }
        /// <summary>
        /// 任务所在的活动名称
        /// </summary>
        public string ActivityName { get; set; }

        //关联对象属性拆分为单个属性，简化对象

        /// <summary>
        /// 任务所在的流程标识
        /// </summary>
        public Guid ProcessId { get; set; }
        /// <summary>
        /// 所在流程的标题
        /// </summary>
        public string ProcessTitle { get; set; }
        /// <summary>
        /// 所在流程的发起时间
        /// </summary>
        public DateTime ProcessCreateTime { get; set; }
        /// <summary>
        /// 所在流程的类型
        /// </summary>
        public string ProcessTypeName { get; set; }
    }

    /// <summary>
    /// 任务集合信息
    /// </summary>
    public class WorkItemsInfo
    {
        /// <summary>
        /// 获取任务列表
        /// </summary>
        public WorkItem[] WorkItems { get; set; }
        /// <summary>
        /// 获取总数
        /// </summary>
        public long Total { get; set; }
    }
}