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
    /// 流程类型
    /// </summary>
    public class ProcessType
    {
        /// <summary>
        /// 流程名
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }
        /// <summary>
        /// 是否是当前版本
        /// </summary>
        public bool IsCurrent { get; set; }
        /// <summary>
        /// 版本号
        /// </summary>
        public string Version { get; set; }
        /// <summary>
        /// 分组信息
        /// </summary>
        public string Group { get; set; }

        /// <summary>
        /// 节点定义列表
        /// </summary>
        public string[] ActivityNames { get; set; }
        /// <summary>
        /// 流程变量定义列表
        /// </summary>
        public string[] DataFields { get; set; }
    }
}