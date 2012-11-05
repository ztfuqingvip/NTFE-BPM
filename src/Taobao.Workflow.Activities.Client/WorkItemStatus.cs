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
    /// 任务状态
    /// </summary>
    public enum WorkItemStatus
    {
        //客户端不允许查询失效的任务

        /// <summary>
        /// 新建
        /// </summary>
        New = 0,
        /// <summary>
        /// 打开/占用
        /// </summary>
        Open = 1,
        ///// <summary>
        ///// 已执行
        ///// </summary>
        //Executed = 2,
        ///// <summary>
        ///// 已取消
        ///// </summary>
        //Canceled = 3
    }
}
