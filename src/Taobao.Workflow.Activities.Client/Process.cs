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
    /// 流程
    /// </summary>
    public class Process
    {
        /// <summary>
        /// 标识
        /// </summary>
        public Guid ID { get; set; }
        /// <summary>
        /// 标题
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// 发起时间
        /// </summary>
        public DateTime CreateTime { get; set; }
        /// <summary>
        /// 流程类型信息
        /// </summary>
        public ProcessType ProcessType { get; set; }
        /// <summary>
        /// 发起人
        /// </summary>
        public string Originator { get; set; }
        /// <summary>
        /// 流程状态
        /// </summary>
        public ProcessStatus Status { get; set; }
        /// <summary>
        /// 优先级
        /// </summary>
        public int Priority { get; set; }
        /// <summary>
        /// 数据字段集合
        /// </summary>
        public IDictionary<string, string> DataFields { get; set; }
    }

    /// <summary>
    /// 流程集合信息
    /// </summary>
    public class ProcessesInfo
    {
        /// <summary>
        /// 获取流程列表
        /// </summary>
        public Process[] Processes { get; set; }
        /// <summary>
        /// 获取总数
        /// </summary>
        public long Total { get; set; }
    }
}
