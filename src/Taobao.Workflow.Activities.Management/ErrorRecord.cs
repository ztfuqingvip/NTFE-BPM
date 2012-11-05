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
    /// 错误记录
    /// </summary>
    public class ErrorRecord
    {
        /// <summary>
        /// 错误编号
        /// </summary>
        public long ID { get; set; }
        /// <summary>
        /// 出错的流程实例
        /// </summary>
        public Process Process { get; set; }
        /// <summary>
        /// 异常内容/原因
        /// </summary>
        public string Reason { get; set; }
        /// <summary>
        /// 错误时间
        /// </summary>
        public DateTime CreateTime { get; set; }
    }
}
