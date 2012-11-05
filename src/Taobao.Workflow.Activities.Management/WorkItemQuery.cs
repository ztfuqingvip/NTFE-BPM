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
    /// 任务查询对象
    /// </summary>
    public class WorkItemQuery
    {
        /// <summary>
        /// 流程类型名称
        /// </summary>
        public string ProcessTypeName { get; set; }
        /// <summary>
        /// 流程标题关键字
        /// </summary>
        public string ProcessTitle { get; set; }
        /// <summary>
        /// 流程节点名称
        /// </summary>
        public string ActivityName { get; set; }
        /// <summary>
        /// 执行人
        /// </summary>
        public string Actioner { get; set; }

        /// <summary>
        /// 任务创建时间范围 起始
        /// </summary>
        public DateTime? CreateFrom { get; set; }
        /// <summary>
        /// 任务创建时间范围 结束
        /// </summary>
        public DateTime? CreateTo { get; set; }

        /// <summary>
        /// 任务状态
        /// </summary>
        public WorkItemStatus[] Status { get; set; }
    }
}
